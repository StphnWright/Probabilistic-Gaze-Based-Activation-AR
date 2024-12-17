using System;
using MagicLeap.Android;
using MagicLeap.OpenXR.Features.EyeTracker;
using UnityEngine;
using UnityEngine.XR.OpenXR;

public class EyeTrackerLogger : MonoBehaviour
{
    private MagicLeapEyeTrackerFeature eyeTrackerFeature;
    private bool eyeTrackPermissionGranted = false;
    private bool pupilSizePermissionGranted = false;

    void Start()
    {
        // Request necessary permissions
        Permissions.RequestPermissions(new string[]
        {
            Permissions.EyeTracking,
            Permissions.PupilSize
        }, OnPermissionGranted, OnPermissionDenied);
    }

    private void OnPermissionGranted(string permission)
    {
        if (permission == Permissions.EyeTracking)
        {
            eyeTrackPermissionGranted = true;
            Debug.Log("Eye Tracking permission granted.");
        }

        if (permission == Permissions.PupilSize)
        {
            pupilSizePermissionGranted = true;
            Debug.Log("Pupil Size permission granted.");
        }

        // If both permissions are granted, initialize the eye tracker
        if (eyeTrackPermissionGranted && pupilSizePermissionGranted)
        {
            InitializeEyeTracker();
        }
    }

    private void OnPermissionDenied(string permission)
    {
        Debug.LogError($"{permission} denied. Eye tracking data will not be available.");
    }

    private void InitializeEyeTracker()
    {
        // Initialize the Eye Tracker feature
        eyeTrackerFeature = OpenXRSettings.Instance.GetFeature<MagicLeapEyeTrackerFeature>();
        if (eyeTrackerFeature != null && eyeTrackerFeature.enabled)
        {
            eyeTrackerFeature.CreateEyeTracker();
            Debug.Log("Eye Tracker initialized.");
        }
        else
        {
            Debug.LogError("Failed to initialize the Eye Tracker. Ensure the feature is enabled in OpenXR settings.");
        }
    }

    void Update()
    {
        // Only proceed if both permissions have been granted
        if (!eyeTrackPermissionGranted || !pupilSizePermissionGranted)
            return;

        if (eyeTrackerFeature != null && eyeTrackerFeature.enabled)
        {
            EyeTrackerData eyeTrackerData;
            // Retrieve all eye-tracking data
            try
            {
                eyeTrackerData = eyeTrackerFeature.GetEyeTrackerData();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }

            // Log Geometric Data
            foreach (var geometricData in eyeTrackerData.GeometricData)
            {
                Debug.Log($"Geometric Data - Eye: {geometricData.Eye}, Openness: {geometricData.EyeOpenness}, Position: {geometricData.EyeInSkullPosition}");
            }

            // Log Pupil Data
            foreach (var pupilData in eyeTrackerData.PupilData)
            {
                Debug.Log($"Pupil Data - Eye: {pupilData.Eye}, Diameter: {pupilData.PupilDiameter} meters");
            }

            // Log Gaze Behavior Data
            GazeBehavior gazeBehavior = eyeTrackerData.GazeBehaviorData;
            Debug.Log($"Gaze Behavior - Type: {gazeBehavior.GazeBehaviorType}, Duration: {gazeBehavior.Duration}, Amplitude: {gazeBehavior.MetaData.Amplitude}, Direction: {gazeBehavior.MetaData.Direction}, Velocity: {gazeBehavior.MetaData.Velocity}");

            // Log Static Data
            StaticData staticData = eyeTrackerData.StaticData;
            Debug.Log($"Static Data - Max Width: {staticData.EyeWidthMax}, Max Height: {staticData.EyeHeightMax}");
        }
    }

    void OnDestroy()
    {
        // Clean up the Eye Tracker
        if (eyeTrackerFeature != null && eyeTrackerFeature.enabled)
        {
            eyeTrackerFeature.DestroyEyeTracker();
            Debug.Log("Eye Tracker destroyed.");
        }
    }
}