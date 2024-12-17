using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class MetricLoggerHelper
{
    public static float CalculateDepthError(Transform target, Transform gazeFixation)
    {
        return Mathf.Abs(target.localPosition.z - gazeFixation.localPosition.z);
    }
}
public class MetricLogger : MonoBehaviour
{
    [SerializeField] GazeInput gazeInput;
    [SerializeField] Transform rightEye;
    [SerializeField] Transform leftEye;
    [SerializeField] Transform gaze;
    [SerializeField] SceneManager sceneManager;
    [SerializeField] Transform mlGazeFixation;
    [SerializeField] Transform gazeFixation;
    
    private List<Vector3> gazePositionBuffer = new List<Vector3>();
    private const float fixationDuration = 2.0f; // Duration in seconds
    private float gazeBufferStartTime;
    
    private StreamWriter writer;

    public bool IsLogging { get; private set; }
    bool gazeTrackingDataWritten = false;
    private bool isFirstEntry = true;
    private AdvancedMetricLogger _advancedMetricLogger;

    private string filePath;

    private void Start()
    {
        _advancedMetricLogger = new AdvancedMetricLogger("advanced_metrics.txt", sceneManager);
    }

    public void StartLogging(string sceneName, Transform target, List<Transform> distractors)
    {
        _advancedMetricLogger.StartLogging();
        filePath = Path.Combine(Application.persistentDataPath, "study_2_debug_file.txt");

        writer = new StreamWriter(filePath, true);

        if (!isFirstEntry)
        {
            writer.WriteLine(","); // Add a comma before the new entry
        }

        // Start of the JSON object for this entry
        writer.WriteLine("{");
        writer.WriteLine($"  \"StartTime\": \"{System.DateTime.Now}\",");
        writer.WriteLine($"  \"SceneName\": \"{sceneName}\",");

        // Target details
        writer.WriteLine("  \"Target\": {");
        writer.WriteLine(
            $"    \"Position\": {{ \"x\": {target.localPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {target.localPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {target.localPosition.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");

        writer.WriteLine($"    \"Diameter\": {target.localScale.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
        writer.WriteLine("  },");

        // Distractors details
        writer.WriteLine("  \"Distractors\": [");
        for (int i = 0; i < distractors.Count; i++)
        {
            var distractor = distractors[i];
            writer.WriteLine("    {");
            writer.WriteLine(
                $"      \"Position\": {{ \"x\": {distractor.localPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {distractor.localPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {distractor.localPosition.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");

            writer.WriteLine($"      \"Diameter\": {distractor.localScale.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}");
            writer.WriteLine("    }" + (i < distractors.Count - 1 ? "," : ""));
        }

        writer.WriteLine("  ],");

        // Gaze tracking details
        writer.WriteLine("  \"GazeTracking\": [");
        IsLogging = true;
    }

    public void RecordGazeTracking()
    {
        if (writer != null && IsLogging)
        {
            _advancedMetricLogger.LogMetrics(sceneManager.TimeElapsed, sceneManager.Target, gazeFixation, leftEye, rightEye, gazeInput.LeftPupilDiameter, gazeInput.RightPupilDiameter);
            UpdateGazeBuffer();
            
            if (gazeTrackingDataWritten)
            {
                writer.WriteLine(",");
            }

            writer.WriteLine("    {");
            writer.WriteLine($"      \"TimeFromStartTrial:\": \"{sceneManager.TimeElapsed}\",");
            
            float depthError = MetricLoggerHelper.CalculateDepthError(sceneManager.Target, gazeFixation);
            writer.WriteLine($"      \"DepthError\": {depthError.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");
            
            float fixationStability = CalculateFixationStability();
            writer.WriteLine($"      \"FixationStability\": {fixationStability.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");

            writer.WriteLine($"      \"PupilDiameterLeft\": {gazeInput.LeftPupilDiameter.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");
            writer.WriteLine($"      \"PupilDiameterRight\": {gazeInput.RightPupilDiameter.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");
            
            writer.WriteLine(
                $"      \"LeftEyePosition\": {{ \"x\": {leftEye.localPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {leftEye.localPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {leftEye.localPosition.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");

            Vector3 leftEyeAngles = leftEye.localRotation.eulerAngles;
            writer.WriteLine(
                $"      \"LeftEyeRotation\": {{ \"x\": {leftEyeAngles.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {leftEyeAngles.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {leftEyeAngles.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");
            // writer.WriteLine(
            //     $"      \"LeftEyeDirection\": {{ \"x\": {gazeInput.LeftEyeDirection.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {gazeInput.LeftEyeDirection.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {gazeInput.LeftEyeDirection.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},"); 
            //
            writer.WriteLine(
                $"      \"RightEyePosition\": {{ \"x\": {rightEye.localPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {rightEye.localPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {rightEye.localPosition.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");


            Vector3 rightEyeAngles = rightEye.localRotation.eulerAngles;
            writer.WriteLine(
                $"      \"RightEyeRotation\": {{ \"x\": {rightEyeAngles.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {rightEyeAngles.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {rightEyeAngles.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");
            // writer.WriteLine(
            //     $"      \"RightEyeDirection\": {{ \"x\": {gazeInput.RightEyeDirection.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {gazeInput.RightEyeDirection.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {gazeInput.RightEyeDirection.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");
            //
            writer.WriteLine(
                $"      \"GazePosition\": {{ \"x\": {gaze.localPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {gaze.localPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {gaze.localPosition.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");
            
            Vector3 gazeAngles = gaze.localRotation.eulerAngles;
            writer.WriteLine(
                $"      \"GazeRotation\": {{ \"x\": {gazeAngles.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {gazeAngles.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {gazeAngles.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");
            // writer.WriteLine(
            //     $"      \"GazeDirection\": {{ \"x\": {gazeInput.GazeDirection.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {gazeInput.GazeDirection.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {gazeInput.GazeRotation.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");
            
            writer.WriteLine(
                $"      \"GazeFixationPosition\": {{ \"x\": {gazeFixation.localPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {gazeFixation.localPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {gazeFixation.localPosition.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");

            writer.WriteLine($"      \"GazeFixationDiameter\": {gazeFixation.localScale.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");
            writer.WriteLine($"      \"IsGazeCollideWithTarget\": {SphereTools.AreSphereColliding(gazeFixation,sceneManager.Target).ToString().ToLower()},"); 
            writer.WriteLine(
                $"      \"MLGazeFixationPosition\": {{ \"x\": {mlGazeFixation.localPosition.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"y\": {mlGazeFixation.localPosition.y.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)}, \"z\": {mlGazeFixation.localPosition.z.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)} }},");

            writer.WriteLine($"      \"MLGazeFixationDiameter\": {mlGazeFixation.localScale.x.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");
            writer.WriteLine($"      \"IsMLGazeCollideWithTarget\": {SphereTools.AreSphereColliding(mlGazeFixation,sceneManager.Target).ToString().ToLower()}");
            writer.WriteLine("    }");

            gazeTrackingDataWritten = true;

            // Periodically flush the writer to disk to ensure data is written
            writer.Flush();
        }
    }

    public void StopLogging()
    {
        if (writer != null && IsLogging)
        {
            _advancedMetricLogger.StopLogging();
            // Write the closing brackets for the GazeTracking array and the main JSON object
            writer.WriteLine("  ]");
            writer.WriteLine("},");

            writer.Close();
            writer = null;
        }

        IsLogging = false;
        gazeTrackingDataWritten = false;
    }
    
    private void UpdateGazeBuffer()
    {
        if (IsLogging)
        {
            float currentTime = sceneManager.TimeElapsed;

            // Add the current gaze position to the buffer
            gazePositionBuffer.Add(gaze.localPosition);

            // Remove outdated entries (older than fixationDuration)
            gazePositionBuffer = gazePositionBuffer
                .Where((_, index) => currentTime - gazeBufferStartTime < fixationDuration)
                .ToList();

            // Update the start time for the buffer
            gazeBufferStartTime = currentTime;
        }
    }

    private float CalculateFixationStability()
    {
        if (gazePositionBuffer.Count > 1)
        {
            // Calculate the mean position
            Vector3 mean = new Vector3(
                gazePositionBuffer.Average(p => p.x),
                gazePositionBuffer.Average(p => p.y),
                gazePositionBuffer.Average(p => p.z)
            );

            // Calculate variance
            float variance = gazePositionBuffer.Average(p => (p - mean).sqrMagnitude);
            return Mathf.Sqrt(variance); // Standard deviation
        }

        return 0f; // Return 0 if not enough data
    }

    private void OnApplicationQuit()
    {
        // Ensure that any remaining data is saved when the application is about to close
        StopLogging();
    }

    private void OnDestroy()
    {
        // Ensure that logging is stopped when the object is destroyed
        StopLogging();
    }
}
