using System.Collections.Generic;
using MagicLeap.Android;
using MagicLeap.OpenXR.Features.EyeTracker;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

public class GazeInput : MonoBehaviour
{
    // Reference to the object representing the fixation point in the scene
    [SerializeField] private Transform fixationPoseObject;
    [SerializeField] private Transform leftEye;
    [SerializeField] private Transform rightEye;
    [SerializeField] private Transform gaze;
    
    // Layer of interactable objects for gaze interaction
    [SerializeField] private LayerMask interactableLayer; 
    
    // Maximum distance for the gaze raycast to detect interactable objects
    [SerializeField] private float maxRayDistance = 10f;

    // Eye position, direction, and rotation properties for both eyes
    public Vector3 RightEyePosition { get; private set; }
    public Vector3 RightEyeDirection { get; private set; }
    public Quaternion RightEyeRotation { get; private set; }
    public Vector3 LeftEyePosition { get; private set; }
    public Vector3 LeftEyeDirection { get; private set; }
    public Quaternion LeftEyeRotation { get; private set; }
    
    // Gaze direction, rotation, and position properties
    public Vector3 GazeDirection { get; private set; }
    public Quaternion GazeRotation { get; private set; }
    public Vector3 GazePosition { get; private set; }
    
    public bool IsInitialized  { get; private set; }

    // Internal variables for tracking eye input device and permissions
    private InputDevice _eyeTrackingDevice;
    private bool _permissionGranted;
    
    // Callbacks for handling permission requests and responses
    private readonly MLPermissions.Callbacks _permissionCallbacks = new MLPermissions.Callbacks();
    
    // MagicLeap feature to access eye tracking data
    private MagicLeapEyeTrackerFeature _eyeTrackerFeature;

    private void Start()
    {
        // Register callbacks for handling permission responses
        RegisterPermissionCallbacks();
        
        // Request necessary permissions for eye tracking
        RequestEyeTrackingPermissions();
    }

    private void Update()
    {
        // Skip if permission not granted or device not ready
        if (!_permissionGranted || !IsInitialized)
            return;

        // Delay first data fetch to allow initialization
        if (Time.timeSinceLevelLoad < 3.0f)  // Delay for 3 second
            return;
    
        // Update or acquire the eye tracking input device
        UpdateEyeTrackingDevice();
        
        // Retrieve and process gaze tracking data
        ProcessGazeData();
        
        // Perform raycast from gaze direction to interact with objects
        PerformRaycastInteraction();
    }

    private void OnDestroy()
    {
        // Unregister callbacks to avoid memory leaks when the object is destroyed
        UnregisterPermissionCallbacks();
        
        // Destroy the eye tracker feature if it was enabled
        DestroyEyeTracker();
    }

    // Registers permission callbacks to handle the responses for permission requests
    private void RegisterPermissionCallbacks()
    {
        _permissionCallbacks.OnPermissionGranted += OnPermissionGranted;
        _permissionCallbacks.OnPermissionDenied += OnPermissionDenied;
        _permissionCallbacks.OnPermissionDeniedAndDontAskAgain += OnPermissionDenied;
    }

    // Unregisters permission callbacks on destroy to prevent issues
    private void UnregisterPermissionCallbacks()
    {
        _permissionCallbacks.OnPermissionGranted -= OnPermissionGranted;
        _permissionCallbacks.OnPermissionDenied -= OnPermissionDenied;
        _permissionCallbacks.OnPermissionDeniedAndDontAskAgain -= OnPermissionDenied;
    }

    // Requests permissions for eye tracking and pupil size data
    private void RequestEyeTrackingPermissions()
    {
        Permissions.RequestPermissions(new[] { Permissions.EyeTracking, Permissions.PupilSize }, OnPermissionGranted, OnPermissionDenied);
    }

    // Initializes the eye tracker feature if it’s enabled in OpenXR settings
    private void InitializeEyeTrackerFeature()
    {
        // Avoid re-initialization if already initialized
        if (_eyeTrackerFeature != null && _eyeTrackerFeature.enabled) return;

        _eyeTrackerFeature = OpenXRSettings.Instance.GetFeature<MagicLeapEyeTrackerFeature>();
        
        // If the feature is enabled, create the eye tracker instance
        if (_eyeTrackerFeature?.enabled == true)
        {
            _eyeTrackerFeature.CreateEyeTracker();
            IsInitialized = true;
            Debug.Log("Eye Tracker initialized.");
        }
    }

    // Checks for and updates the eye tracking input device if not already available
    private void UpdateEyeTrackingDevice()
    {
        if (_eyeTrackingDevice.isValid) return;

        // Search for devices with eye tracking characteristics
        List<InputDevice> inputDeviceList = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, inputDeviceList);

        if (inputDeviceList.Count > 0)
        {
            _eyeTrackingDevice = inputDeviceList[0];
            Debug.Log("Eye tracking device acquired.");
        }
        else
        {
            Debug.LogWarning("Unable to acquire eye tracking device. Have permissions been granted?");
        }
    }

    // Processes gaze data by updating eye position, rotation, and direction
    private void ProcessGazeData()
    {
        // Use MagicLeap eye tracker feature to get eye pose data if available
        if (_eyeTrackerFeature != null && _eyeTrackerFeature.enabled)
        {
            UpdateEyePoseData();
        }

        // Retrieve gaze tracking data from the eye tracking input device
        if (_eyeTrackingDevice.isValid)
        {
            bool isTracked = TryGetEyeTrackingData(out Vector3 position, out Quaternion rotation);
            if (isTracked)
            {
                GazePosition = position;
                GazeRotation = rotation;
                GazeDirection = rotation * Vector3.forward;
                
                gaze.position = GazePosition;
                gaze.rotation = GazeRotation;
            }
        }
    }

    // Updates the left and right eye poses using the MagicLeap eye tracker feature
    private void UpdateEyePoseData()
    {
        if (_eyeTrackerFeature == null || !_eyeTrackerFeature.enabled || !IsInitialized)
            return;
        
        var posesData = _eyeTrackerFeature.GetPosesData();
        LeftEyePosition = posesData.LeftPose.Pose.position;
        LeftEyeRotation = posesData.LeftPose.Pose.rotation;
        LeftEyeDirection = LeftEyeRotation * Vector3.forward;
        leftEye.position = LeftEyePosition;
        leftEye.rotation = LeftEyeRotation;

        RightEyePosition = posesData.RightPose.Pose.position;
        RightEyeRotation = posesData.RightPose.Pose.rotation;
        RightEyeDirection = RightEyeRotation * Vector3.forward;
        rightEye.position = RightEyePosition;
        rightEye.rotation = RightEyeRotation;
        
        var pupilData =  _eyeTrackerFeature.GetPupilData();
        LeftPupilDiameter = pupilData[0].PupilDiameter;
        RightPupilDiameter = pupilData[1].PupilDiameter;
        
        fixationPoseObject.position = _eyeTrackerFeature.GetEyeTrackerData().PosesData.FixationPose.Pose.position;
    }

    public float RightPupilDiameter { get; set; }

    public float LeftPupilDiameter { get; set; }

    // Tries to get eye tracking data, returns true if tracked, and outputs position and rotation
    private bool TryGetEyeTrackingData(out Vector3 position, out Quaternion rotation)
    {
        bool hasData = _eyeTrackingDevice.TryGetFeatureValue(CommonUsages.isTracked, out bool isTracked);
        hasData &= _eyeTrackingDevice.TryGetFeatureValue(EyeTrackingUsages.gazePosition, out position);
        hasData &= _eyeTrackingDevice.TryGetFeatureValue(EyeTrackingUsages.gazeRotation, out rotation);
        return isTracked && hasData;
    }

    // Raycasts from the gaze direction to interact with objects on the specified layer
    private void PerformRaycastInteraction()
    {
        Ray gazeRay = new Ray(GazePosition, GazeDirection);
        if (Physics.Raycast(gazeRay, out RaycastHit hit, maxRayDistance, interactableLayer))
        {
            if (hit.collider.TryGetComponent(out InteractableSphere interactable))
            {
                interactable.OnGazeHit();
                Debug.Log("Gaze hit an interactable object");
            }
        }
    }

    // Destroys the eye tracker feature if it was created, used on object destruction
    private void DestroyEyeTracker()
    {
        if (_eyeTrackerFeature != null && _eyeTrackerFeature.enabled)
        {
            _eyeTrackerFeature.DestroyEyeTracker();
            IsInitialized = false;
            Debug.Log("Eye Tracker destroyed.");
        }
    }

    // Callback for when a permission is granted
    private void OnPermissionGranted(string permission)
    {
        _permissionGranted = true;
        InitializeEyeTrackerFeature();
    }

    // Callback for when a permission is denied, logs an error
    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"Permission denied: {permission}");
    }
}
