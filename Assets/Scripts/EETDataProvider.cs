using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class EETDataProvider : MonoBehaviour
{
    [SerializeField] private GameObject LeftGazeObject;
    [SerializeField] private GameObject RightGazeObject;
    [SerializeField] private GameObject CombinedGazeObject;
    [SerializeField] private GameObject CameraRelativeCombinedGazeObject;
    [SerializeField] private GazeInput extendedEyeGazeDataProvider;
    [SerializeField] private ObjectTransformController transformController;
    private SphereCollider CombinedGazeObjectCollider;
    [SerializeField] private Toggle toggle;

    [SerializeField] private MetricLogger metricLogger;
    
    private DateTime timestamp;
    // private GazeInput.GazeReading gazeReading;

    // Gaze Data
    public Vector3 leftPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 rightPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 combinedPosition = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 leftDirection = new Vector3(1.0f, 0.0f, 0.0f);
    public Vector3 rightDirection = new Vector3(1.0f, 0.0f, 0.0f);
    public Vector3 combinedDirection = new Vector3(1.0f, 0.0f, 0.0f);
    public float eyeTrackerVergDist = 0.0f;

    // Vergence Computations Data
    public Vector3 closestPoint = new Vector3(0.0f, 0.0f, 0.0f);
    public Vector3 intersectionPoint = new Vector3(0.0f, 0.0f, 0.0f);
    public float currentClosestPointVergeDist = 0.0f;
    public float currentIntersectionPointVergeDist = 0.0f;

    // Collision Data
    public List<Collider> intersectedVirtualColliders;
    public SphereCollider[] allSphereColliders;

    // Debug Data
    public string path; // For debugging, writes computations in a file located at <path>
    private int loop_count = 0; // For debugging, print every 50 loops
    private int debug_frequency = 1; // Out results every <debug_frequency> loops
    private int debug_batch_size = 1; // Out <debug_batch_size> loops every <debug_frequency>
    private bool oldIsRecording = false;

    private void writeToDebugFile(string sentence, bool option = true) // Out results in a text file
    {
        if (option)
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                if (toggle.isOn && loop_count % debug_frequency < debug_batch_size)
                {
                    writer.WriteLine(sentence);
                }
            }
        }
        else
        {
            using (StreamWriter writer = new StreamWriter(path, true))
            {
                if (toggle.isOn && !oldIsRecording)
                {
                    writer.WriteLine(sentence);
                }
            }
        }
    }

    private void OnEnable()
    {
        allSphereColliders = GameObject.FindObjectsOfType<SphereCollider>();

        // Debug
        // buttonScript = ButtonObject.GetComponent<buttonExperimentSettings>();
        path = Path.Combine(UnityEngine.Application.persistentDataPath, "debug_file.txt");
        UnityEngine.Debug.Log("path = " + path);
    }

    // Takes as an argument two 3D lines each defined a point "position<i>" and a vector "direction<i>"
    private Vector3 FindIntersection(Vector3 position1, Vector3 position2, Vector3 direction1, Vector3 direction2)
    {
        // Makes sure the line directions are normalized
        Vector3 n_direction1 = direction1.normalized;
        Vector3 n_direction2 = direction2.normalized;

        // Formulates intersection problem as a linear system AX=b
        float[][] matrix_values = new float[][]
        {
            new float[] { 1, 0, 0, -n_direction1.x, 0 },
            new float[] { 0, 1, 0, -n_direction1.y, 0 },
            new float[] { 0, 0, 1, -n_direction1.z, 0 },
            new float[] { 1, 0, 0, 0, -n_direction2.x },
            new float[] { 0, 1, 0, 0, -n_direction2.y },
            new float[] { 0, 0, 1, 0, -n_direction2.z }
        };
        Matrix A = new Matrix(matrix_values);
        Matrix A_T = A.T;

        matrix_values = new float[][]
        {
            new float[] { position1.x },
            new float[] { position1.y },
            new float[] { position1.z },
            new float[] { position2.x },
            new float[] { position2.y },
            new float[] { position2.z }
        };
        Matrix b = new Matrix(matrix_values);


        // Solution is X = (A.T*A)^-1 * A.T * b
        // X = (x,y,z,t1,t2), with t<i> being the parametric variable of line <i>
        Matrix X = ((A_T * A).Inverse()) * A_T * b;

        // Debug
        string s_intersectionFullResult = string.Format("({0:F5}, {1:F5}, {2:F5}, {3:F5}, {4:F5})", X[0, 0], X[1, 0],
            X[2, 0], X[3, 0], X[4, 0]);
        writeToDebugFile("\t\t\t\tintersectionFullResult = " + s_intersectionFullResult);

        Vector3 result = new Vector3(X[0, 0], X[1, 0], X[2, 0]);
        return result;
    }

    // Takes as an argument two 3D lines each defined a point "position<i>" and a vector "direction<i>"
    // Computes the pair of points that are closest from each other on both lines and returns the middle of this pair
    private Vector3 FindClosestPoint(Vector3 position1, Vector3 position2, Vector3 direction1, Vector3 direction2)
    {
        // Makes sure the line directions are normalized
        Vector3 n_direction1 = direction1.normalized;
        Vector3 n_direction2 = direction2.normalized;

        // Compute the closed form solution of the two closest points
        float det = Vector3.Dot(n_direction1, n_direction2) * Vector3.Dot(n_direction1, n_direction2) -
                    Vector3.Dot(n_direction1, n_direction1) * Vector3.Dot(n_direction2, n_direction2);
        if (Mathf.Abs(det) < Math.Pow(10, -10)) // det = 0 means directions are colinear
        {
            // Debug
            writeToDebugFile("\t\t\t\tclosestPointFullResult = " + string.Format("({0}, {1}, {2}, {3}, {4})", float.NaN,
                float.NaN, float.NaN, float.NaN, float.NaN));
            return new Vector3(float.NaN, float.NaN, float.NaN);
        }

        Vector3 e = position1 - position2;
        float t = (1.0f / det) * (Vector3.Dot(n_direction2, n_direction2) * Vector3.Dot(n_direction1, e) -
                                  Vector3.Dot(n_direction2, e) * Vector3.Dot(n_direction2, n_direction1));
        float s = (1.0f / det) * (Vector3.Dot(n_direction2, n_direction1) * Vector3.Dot(n_direction1, e) -
                                  Vector3.Dot(n_direction2, e) * Vector3.Dot(n_direction1, n_direction1));
        Vector3 closest_point1 = position1 + t * n_direction1;
        Vector3 closest_point2 = position2 + s * n_direction2;

        // Take the middle of the two closest points
        Vector3 result = 0.5f * (closest_point1 + closest_point2);

        // Debug
        string s_closestPointFullResult =
            string.Format("({0:F5}, {1:F5}, {2:F5}, {3:F5}, {4:F5})", result.x, result.y, result.z, t, s);
        writeToDebugFile("\t\t\t\tclosestPointFullResult = " + s_closestPointFullResult);

        return result;
    }

    // Puts the colliders of the scene that intersect with CombinedGazeObjectCollider in a list called intersectedVirtualColliders
    // Note: CombinedGazeObjectCollider is the collider of our ege-gaze sphere
    void findIntersectingColliders()
    {
        // Get all the colliders in the scene that intersect with the sphere
        Collider[] colliders = Physics.OverlapSphere(CombinedGazeObject.transform.position,
            CombinedGazeObject.transform.localScale.x);

        // Filter out CombinedGazeObjectCollider
        CombinedGazeObjectCollider = CombinedGazeObject.GetComponent<SphereCollider>();
        Collider[] otherColliders = System.Array.FindAll(colliders, c => c != CombinedGazeObjectCollider);

        // Check for intersection with each collider in the list
        intersectedVirtualColliders = new List<Collider>();
        foreach (Collider collider in otherColliders)
        {
            if (CombinedGazeObjectCollider.bounds.Intersects(collider.bounds))
            {
                intersectedVirtualColliders.Add(collider);
            }
        }
    }


    void Update()
    {
        if(!extendedEyeGazeDataProvider.IsInitialized) return;
        // Debug
        writeToDebugFile("\n\nNEW APP LAUNCH\n\n", false);

        // Debug
        writeToDebugFile("\n\t\tUPDATE LOOP " + loop_count + "\n");

        timestamp = DateTime.Now;

        // positioning for left gaze object
        // gazeReading =
        //     extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Left, timestamp);
        if (extendedEyeGazeDataProvider != null)
        {
            // position gaze object 1.5 meters out from the gaze origin along the gaze direction
            leftPosition = extendedEyeGazeDataProvider.LeftEyePosition;
            leftDirection = extendedEyeGazeDataProvider.LeftEyeDirection;
            LeftGazeObject.transform.position = leftPosition + 1.5f * leftDirection;

            //LeftGazeObject.SetActive(true);
            // LeftGazeObject.SetActive(false);

            // Debug
            string s_leftPosition =
                string.Format("({0:F5}, {1:F5}, {2:F5})", leftPosition.x, leftPosition.y, leftPosition.z);
            string s_leftDirection = string.Format("({0:F5}, {1:F5}, {2:F5})", leftDirection.x, leftDirection.y,
                leftDirection.z);
            string s_LeftGazeObjectPosition = string.Format("({0:F5}, {1:F5}, {2:F5})",
                LeftGazeObject.transform.position.x, LeftGazeObject.transform.position.y,
                LeftGazeObject.transform.position.z);
            writeToDebugFile("\t\t\t\tLeft Eye != null - leftPosition = " + s_leftPosition + " - leftDirection = " +
                             s_leftDirection + " - LeftGazeObject.position = " + s_LeftGazeObjectPosition);
        }
        else
        {
            //LeftGazeObject.SetActive(false);

            // Debug
            string s_leftPosition =
                string.Format("({0:F5}, {1:F5}, {2:F5})", leftPosition.x, leftPosition.y, leftPosition.z);
            string s_leftDirection = string.Format("({0:F5}, {1:F5}, {2:F5})", leftDirection.x, leftDirection.y,
                leftDirection.z);
            string s_LeftGazeObjectPosition = string.Format("({0:F5}, {1:F5}, {2:F5})",
                LeftGazeObject.transform.position.x, LeftGazeObject.transform.position.y,
                LeftGazeObject.transform.position.z);
            writeToDebugFile("\t\t\t\tLeft Eye == null - leftPosition = " + s_leftPosition + " - leftDirection = " +
                             s_leftDirection + " - LeftGazeObject.position = " + s_LeftGazeObjectPosition);
        }


        // positioning for right gaze object
        // gazeReading =
        //     extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Right, timestamp);
        if (extendedEyeGazeDataProvider != null)
        {
            // position gaze object 1.5 meters out from the gaze origin along the gaze direction
            rightPosition = extendedEyeGazeDataProvider.RightEyePosition;
            rightDirection = extendedEyeGazeDataProvider.RightEyeDirection;
            RightGazeObject.transform.position = rightPosition + 1.5f * rightDirection;

            //RightGazeObject.SetActive(true);
            // RightGazeObject.SetActive(false);

            // Debug
            string s_rightPosition = string.Format("({0:F5}, {1:F5}, {2:F5})", rightPosition.x, rightPosition.y,
                rightPosition.z);
            string s_rightDirection = string.Format("({0:F5}, {1:F5}, {2:F5})", rightDirection.x, rightDirection.y,
                rightDirection.z);
            string s_RightGazeObjectPosition = string.Format("({0:F5}, {1:F5}, {2:F5})",
                RightGazeObject.transform.position.x, RightGazeObject.transform.position.y,
                RightGazeObject.transform.position.z);
            writeToDebugFile("\t\t\t\tRight Eye != null - rightPosition = " + s_rightPosition + " - rightDirection = " +
                             s_rightDirection + " - RightGazeObject.position = " + s_RightGazeObjectPosition);
        }
        else
        {
            //RightGazeObject.SetActive(false);

            // Debug
            string s_rightPosition = string.Format("({0:F5}, {1:F5}, {2:F5})", rightPosition.x, rightPosition.y,
                rightPosition.z);
            string s_rightDirection = string.Format("({0:F5}, {1:F5}, {2:F5})", rightDirection.x, rightDirection.y,
                rightDirection.z);
            string s_RightGazeObjectPosition = string.Format("({0:F5}, {1:F5}, {2:F5})",
                RightGazeObject.transform.position.x, RightGazeObject.transform.position.y,
                RightGazeObject.transform.position.z);
            writeToDebugFile("\t\t\t\tRight Eye == null - rightPosition = " + s_rightPosition + " - rightDirection = " +
                             s_rightDirection + " - RightGazeObject.position = " + s_RightGazeObjectPosition);
        }


        // positioning for combined gaze object
        // gazeReading =
        //     extendedEyeGazeDataProvider.GetWorldSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined,
        //         timestamp);
        if (extendedEyeGazeDataProvider != null)
        {
            combinedPosition = extendedEyeGazeDataProvider.GazePosition;
            combinedDirection = extendedEyeGazeDataProvider.GazeDirection;

            // Intersection Point
            intersectionPoint = FindIntersection(leftPosition, rightPosition, leftDirection, rightDirection);
            
            // Only update the vergence position under certain conditions
            if ((!float.IsNaN(intersectionPoint.x)) && (!float.IsNaN(intersectionPoint.y)) &&
                (!float.IsNaN(intersectionPoint.z)))
            {
                currentIntersectionPointVergeDist = (combinedPosition - intersectionPoint).magnitude;

                // Debug
                string s_intersectionPoint = string.Format("({0:F5},{1:F5},{2:F5})", intersectionPoint.x,
                    intersectionPoint.y, intersectionPoint.z);
                string s_currentIntersectionPointVergeDist =
                    string.Format("({0:F5})", currentIntersectionPointVergeDist);
                writeToDebugFile("\t\t\t\tintersectionPoint != nan - intersectionPoint = " + s_intersectionPoint +
                                 " - currentIntersectionPointVergeDist = " + s_currentIntersectionPointVergeDist);
            }
            else
            {
                // Debug
                string s_intersectionPoint = string.Format("({0:F5},{1:F5},{2:F5})", intersectionPoint.x,
                    intersectionPoint.y, intersectionPoint.z);
                string s_currentIntersectionPointVergeDist =
                    string.Format("({0:F5})", currentIntersectionPointVergeDist);
                writeToDebugFile("\t\t\t\tintersectionPoint == nan - intersectionPoint = " + s_intersectionPoint +
                                 " - currentIntersectionPointVergeDist = " + s_currentIntersectionPointVergeDist);
            }

            // Closest Point
            closestPoint = FindClosestPoint(leftPosition, rightPosition, leftDirection, rightDirection);
            // Only update the vergence position under certain conditions
            if ((!float.IsNaN(closestPoint.x)) && (!float.IsNaN(closestPoint.y)) && (!float.IsNaN(closestPoint.z)))
            {
                currentClosestPointVergeDist = (combinedPosition - closestPoint).magnitude;

                // Debug
                string s_closestPoint = string.Format("({0:F5},{1:F5},{2:F5})", closestPoint.x, closestPoint.y,
                    closestPoint.z);
                string s_currentClosestPointVergeDist = string.Format("({0:F5})", currentClosestPointVergeDist);
                writeToDebugFile("\t\t\t\tclosestPoint != nan - closestPoint = " + s_closestPoint +
                                 " - currentClosestPointVergeDist = " + s_currentClosestPointVergeDist);
            }
            else
            {
                // Debug
                string s_closestPoint = string.Format("({0:F5},{1:F5},{2:F5})", closestPoint.x, closestPoint.y,
                    closestPoint.z);
                string s_currentClosestPointVergeDist = string.Format("({0:F5})", currentClosestPointVergeDist);
                writeToDebugFile("\t\t\t\tclosestPoint == nan - closestPoint = " + s_closestPoint +
                                 " - currentClosestPointVergeDist = " + s_currentClosestPointVergeDist);
            }

            // currentClosestPointVergeDist is the current vergence distance in meters
            // position gaze object <currentClosestPointVergeDist> meters out from the gaze origin along the gaze direction
            CombinedGazeObject.transform.position = combinedPosition + currentClosestPointVergeDist * combinedDirection;

            //CombinedGazeObject.SetActive(true);
            // CombinedGazeObject.SetActive(false);

            // Debug
            string s_combinedPosition = string.Format("({0:F5}, {1:F5}, {2:F5})", combinedPosition.x,
                combinedPosition.y, combinedPosition.z);
            string s_combinedDirection = string.Format("({0:F5}, {1:F5}, {2:F5})", combinedDirection.x,
                combinedDirection.y, combinedDirection.z);
            string s_CombinedGazeObjectPosition = string.Format("({0:F5}, {1:F5}, {2:F5})",
                CombinedGazeObject.transform.position.x, CombinedGazeObject.transform.position.y,
                CombinedGazeObject.transform.position.z);
            writeToDebugFile("\t\t\t\tCombined Eye != null - combinedPosition = " + s_combinedPosition +
                             " - combinedDirection = " + s_combinedDirection + " - CombinedGazeObject.position = " +
                             s_CombinedGazeObjectPosition);
        }
        else
        {
            //CombinedGazeObject.SetActive(false);

            // Debug
            string s_combinedPosition = string.Format("({0:F5}, {1:F5}, {2:F5})", combinedPosition.x,
                combinedPosition.y, combinedPosition.z);
            string s_combinedDirection = string.Format("({0:F5}, {1:F5}, {2:F5})", combinedDirection.x,
                combinedDirection.y, combinedDirection.z);
            string s_CombinedGazeObjectPosition = string.Format("({0:F5}, {1:F5}, {2:F5})",
                CombinedGazeObject.transform.position.x, CombinedGazeObject.transform.position.y,
                CombinedGazeObject.transform.position.z);
            writeToDebugFile("\t\t\t\tCombined Eye == null - combinedPosition = " + s_combinedPosition +
                             " - combinedDirection = " + s_combinedDirection + " - CombinedGazeObject.position = " +
                             s_CombinedGazeObjectPosition);
        }

        // positioning for camera relative gaze cube
        // gazeReading =
        //     extendedEyeGazeDataProvider.GetCameraSpaceGazeReading(ExtendedEyeGazeDataProvider.GazeType.Combined,
        //         timestamp);
        if (extendedEyeGazeDataProvider != null)
        {
            // position gaze object 1.5 meters out from the gaze origin along the gaze direction
            CameraRelativeCombinedGazeObject.transform.localPosition =
                extendedEyeGazeDataProvider.GazePosition + 1.5f * extendedEyeGazeDataProvider.GazeDirection;
            //CameraRelativeCombinedGazeObject.SetActive(true);
            // CameraRelativeCombinedGazeObject.SetActive(false);
        }
        else
        {
            //CameraRelativeCombinedGazeObject.SetActive(false);
        }

        // Check intersection of the combined gaze object with scene elements
        findIntersectingColliders();
        /*UnityEngine.Debug.Log("Number of intersections = " + intersectedVirtualColliders.Count);
        for (int i = 0; i < intersectedVirtualColliders.Count; i++)
        {
            UnityEngine.Debug.Log("intersection " + i + " = " + intersectedVirtualColliders[i].name);
        }*/

        foreach (SphereCollider sphereCollider in allSphereColliders)
        {
            // Debug
            float sphereDistToEye = (sphereCollider.gameObject.transform.position - combinedPosition).magnitude;
            string s_spherePosition = string.Format("({0:F5}, {1:F5}, {2:F5})",
                sphereCollider.gameObject.transform.position.x, sphereCollider.gameObject.transform.position.y,
                sphereCollider.gameObject.transform.position.z);
            string s_sphereDistToEye = string.Format("({0:F5})", sphereDistToEye);
            string s_sphereRadius = string.Format("({0:F5})", sphereCollider.gameObject.transform.localScale.x);
            writeToDebugFile("\t\t\t\tSphere name = " + sphereCollider.gameObject.name + " - spherePosition = " +
                             s_spherePosition + " - sphereDistToEye = " + s_sphereDistToEye + " - sphereRadius = " +
                             s_sphereRadius + " - sphereColor = " +
                             sphereCollider.GetComponent<Renderer>().material.color);

            // Debug
            // sphereExperimentSettings sphereScript = sphereCollider.GetComponentInParent<sphereExperimentSettings>();
            if (transformController != null)
            {
                string s_sliderParams = string.Format("({0:F5}, {1:F5}, {2:F5}, {3:F5})", transformController.XSliderValue,
                    transformController.YSliderValue, transformController.ZSliderValue, transformController.RSliderValue);
                writeToDebugFile("\t\t\t\tSlider values (x,y,z,r) = " + s_sliderParams);
            }
        }

        // Debug
        loop_count++;
        oldIsRecording = toggle.isOn;
    }
}