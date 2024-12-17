using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AdvancedMetricLogger
{
    private StreamWriter writer;
    private string filePath;

    private List<Vector3> gazePositions = new List<Vector3>();
    private List<float> vergenceAngles = new List<float>();
    private Vector3 previousGazePosition;
    private int saccadeCount = 0;
    private float fixationStartTime = -1f;

    private SceneManager _sceneManager;
    private bool _metricsWasWritten;

    private const float fixationWindow = 2.0f;
    private const float saccadeThreshold = 0.3f;

    public AdvancedMetricLogger(string fileName, SceneManager sceneManager)
    {
        _sceneManager = sceneManager;
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        
    }

    public void StartLogging()
    {
        saccadeCount = 0;
        fixationStartTime = -1;
        gazePositions.Clear();
        vergenceAngles.Clear();
        previousGazePosition = Vector3.zero;
        _metricsWasWritten = false;
        
        writer = new StreamWriter(filePath, true);
        
        writer.WriteLine("{");
        writer.WriteLine($"  \"StartTime\": \"{System.DateTime.Now}\",");
        writer.WriteLine($"  \"SceneName\": \"{_sceneManager.CurrentConfig.sceneName}\",");
        writer.WriteLine("  \"Metrics\": [");
    }

    public void LogMetrics(
        float timeElapsed,
        Transform target,
        Transform gazePosition,
        Transform leftEye,
        Transform rightEye,
        float leftPupilDiameter,
        float rightPupilDiameter)
    {
        if (writer == null) return;

        if (_metricsWasWritten)
        {
            writer.WriteLine(",");
        }
        writer.WriteLine("    {");
        writer.WriteLine($"      \"TimeFromStart\": {timeElapsed.ToString("F3")},");
        
        float depthError = MetricLoggerHelper.CalculateDepthError(_sceneManager.Target, gazePosition);
        writer.WriteLine($"      \"DepthError\": {depthError.ToString("F3", System.Globalization.CultureInfo.InvariantCulture)},");

        UpdateFixationStability(gazePosition.position);
        float fixationStability = CalculateFixationStability();
        writer.WriteLine($"      \"FixationStability\": {fixationStability.ToString("F3")},");

        UpdateVergenceStability(leftEye, rightEye);
        float vergenceStability = CalculateVergenceStability();
        writer.WriteLine($"      \"VergenceAngleStability\": {vergenceStability.ToString("F3")},");
        
        UpdateFixationTime(timeElapsed, gazePosition.position, target);
        writer.WriteLine($"      \"TimeToStableFixation\": {fixationStartTime.ToString("F3")},");
        
        writer.WriteLine($"      \"PupilDiameterLeft\": {leftPupilDiameter.ToString("F3")},");
        writer.WriteLine($"      \"PupilDiameterRight\": {rightPupilDiameter.ToString("F3")},");
        
        UpdateSaccadeCount(gazePosition.position);
        writer.WriteLine($"      \"SaccadeCount\": {saccadeCount}");
        
        writer.WriteLine("    }");
        writer.Flush();
        
        _metricsWasWritten = true;
    }

    public void StopLogging()
    {
        if (writer == null) return;

        writer.WriteLine("  ]");
        writer.WriteLine("},");
        writer.Close();
        writer = null;
    }

    private void UpdateFixationStability(Vector3 gazePosition)
    {
        gazePositions.Add(gazePosition);
        if (gazePositions.Count > fixationWindow * 90) 
        {
            gazePositions.RemoveAt(0);
        }
    }

    private float CalculateFixationStability()
    {
        if (gazePositions.Count == 0) return 0;

        Vector3 mean = Vector3.zero;
        foreach (var pos in gazePositions) mean += pos;
        mean /= gazePositions.Count;

        float variance = 0f;
        foreach (var pos in gazePositions) variance += (pos - mean).sqrMagnitude;

        return Mathf.Sqrt(variance / gazePositions.Count);
    }

    private void UpdateVergenceStability(Transform leftEye, Transform rightEye)
    {
            Vector3 eyeDirectionLeft = leftEye.position - rightEye.position;

            float vergenceAngle = Vector3.Angle(leftEye.forward, eyeDirectionLeft);

            vergenceAngle = Mathf.Clamp(vergenceAngle, 0f, 90f);  

            vergenceAngles.Add(vergenceAngle);
    }

    private float CalculateVergenceStability()
    {
        if (vergenceAngles.Count == 0) return 0;

        float mean = 0f;
        foreach (var angle in vergenceAngles) mean += angle;
        mean /= vergenceAngles.Count;

        float variance = 0f;
        foreach (var angle in vergenceAngles) variance += Mathf.Pow(angle - mean, 2);

        return variance / vergenceAngles.Count;
    }

    private void UpdateFixationTime(float timeElapsed, Vector3 gazePosition, Transform target)
    {
        if (fixationStartTime < 0) fixationStartTime = timeElapsed;

        if (Vector3.Distance(gazePosition, target.position) < 0.08f) 
        {
            fixationStartTime = timeElapsed - fixationStartTime;
        }
    }

    private void UpdateSaccadeCount(Vector3 currentGazePosition)
    {
        if (previousGazePosition != Vector3.zero && Vector3.Distance(currentGazePosition, previousGazePosition) > saccadeThreshold)
        {
            saccadeCount++;
        }

        previousGazePosition = currentGazePosition;
    }
}