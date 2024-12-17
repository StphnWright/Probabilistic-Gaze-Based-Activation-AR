using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class SceneManager : MonoBehaviour
{
    [Header("Scene Configuration")] 
    public SceneConfig[] sceneConfigs;
    public float interval = 20f; // Interval in seconds to change the scene
    public float selectingTime = 5f; // Interval in seconds to change the scene
    public int distractorsAmount = 10;
    public float minSceneDepth = 0.4f;
    public float maxSceneDepth = 10f;
    public float minDistractorWidth = 0.5f;
    public float maxDistractorWidth = 4f;

    [Header("Cone Settings")] 
    public float coneAngle = 50f;

    [SerializeField] private Transform gazeFixation;

    public GameObject targetPrefab;
    public GameObject distractorPrefab;
    public Transform targetContainer;

    public LayerMask occlusionMask;


    public MetricLogger metricLogger;

    public Transform Target { get; private set; }
    public float TimeElapsed { get; private set; } // Time elapsed since the last scene change

    public SceneConfig CurrentConfig { get; private set; }

    private void Start()
    {
        
        RunScene("A");
    }

    private void Update()
    {
        TimeElapsed += Time.deltaTime;

        if (TimeElapsed >= interval)
        {
            RunScene(CurrentConfig.sceneName);
        }
        
        metricLogger.RecordGazeTracking();
        
    }

    /// <summary>
    /// Runs a scene based on the specified scene name.
    /// </summary>
    /// <param name="sceneName">Name of the scene to run.</param>
    public void RunScene(string sceneName)
    {
        metricLogger.StopLogging();
        ClearTargetContainer();

        if (!SelectScene(sceneName)) return;

        TimeElapsed = 0f;
        GenerateScene();
    }

    /// <summary>
    /// Clears all child objects in the target container.
    /// </summary>
    private void ClearTargetContainer()
    {
        if (targetContainer == null)
        {
            Debug.LogWarning("Target container is not assigned.");
            return;
        }

        foreach (Transform child in targetContainer)
        {
            Destroy(child.gameObject);
        }
    }

    /// <summary>
    /// Selects a scene configuration based on the given scene name.
    /// </summary>
    /// <param name="sceneName">Name of the scene to select.</param>
    /// <returns>True if the scene configuration is found; otherwise, false.</returns>
    private bool SelectScene(string sceneName)
    {
        foreach (var config in sceneConfigs)
        {
            if (config.sceneName != sceneName) continue;

            CurrentConfig = config;
            Debug.Log($"Selected Scene: {CurrentConfig.sceneName}");

            return true;
        }

        Debug.LogError($"Scene '{sceneName}' not found! Check your configuration.");

        return false;
    }

    /// <summary>
    /// Generates the scene by spawning a target and distractors based on the current configuration.
    /// </summary>
    private void GenerateScene()
    {
        List<Transform> distructors = new List<Transform>();
        if (CurrentConfig == null)
        {
            Debug.LogError("No scene configuration selected!");
            return;
        }

        // Spawn the main target
        float targetWidth = Random.Range(CurrentConfig.targetWidthRange.x, CurrentConfig.targetWidthRange.y);
        float targetDepth = Random.Range(CurrentConfig.targetDepthRange.x, CurrentConfig.targetDepthRange.y);
        float targetRadius = CalculateRadius(targetDepth, targetWidth);
        
        Vector3 targetPosition = GetRandomPositionInCone(targetDepth);
        GameObject mainTarget = SpawnObject(targetPrefab, targetPosition, targetRadius);
        Target = mainTarget.transform;

        // Spawn distractors
        for (int i = 0; i < CurrentConfig.distractorCount; i++)
        {
            var distructor = TrySpawnNearDistractor(mainTarget, targetDepth);
            if (distructor != null)
            {
                distructors.Add(distructor.transform);
            }
        }

        for (int i = 0; i < distractorsAmount; i++)
        {
            var distructor = TrySpawnDistractor();
            if (distructor != null)
            {
                distructors.Add(distructor.transform);
            }
        }

        metricLogger.StartLogging(CurrentConfig.sceneName, mainTarget.transform, distructors);
    }

    private GameObject TrySpawnDistractor()
    {
        int maxAttempts = 20;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            float width = Random.Range(minDistractorWidth, maxDistractorWidth);
            float depth = Random.Range(minSceneDepth, maxSceneDepth);
            float radius = CalculateRadius(depth, width);
            Vector3 position = GetRandomPositionInCone(depth);

            if (!IsOccluded(position) || CurrentConfig.sceneName.Equals("A"))
            {
                var spawnObject = SpawnObject(distractorPrefab, position, radius);
                return spawnObject;
            }
        }

        Debug.LogWarning("Failed to place a distractor without occlusion after multiple attempts.");
        return null;
    }

    /// <summary>
    /// Attempts to spawn a distractor at a valid position near the main target.
    /// </summary>
    /// <param name="mainTarget">The main target object.</param>
    /// <param name="targetDepth">The depth of the main target.</param>
    private GameObject TrySpawnNearDistractor(GameObject mainTarget, float targetDepth)
    {
        int maxAttempts = 20;
        
        float depthDifference = Random.Range(CurrentConfig.minDepthDifference, CurrentConfig.maxDepthDifference);
        float distractorWidth = Random.Range(CurrentConfig.targetWidthRange.x, CurrentConfig.targetWidthRange.y);
        float distractorDepth = Mathf.Clamp(
            targetDepth + Random.Range(-depthDifference, depthDifference),
            0.4f,
            30f
        );
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            Vector3 position = GetRandomNearbyPosition(mainTarget.transform, 0.5f, 2f, distractorWidth, distractorDepth,
                out float distractorRadius);
           
            if (!IsOccluded(position) || CurrentConfig.sceneName.Equals("A"))
            {
                var spawnObject = SpawnObject(distractorPrefab, position, distractorRadius);
                return spawnObject;
            }
        }

        Debug.LogWarning("Failed to place a distractor without occlusion after multiple attempts.");
        return null;
    }

    /// <summary>
    /// Checks if a given position is occluded by other objects.
    /// </summary>
    /// <param name="position">The position to check for occlusion.</param>
    /// <returns>True if the position is occluded; otherwise, false.</returns>
    private bool IsOccluded(Vector3 position)
    {
        Vector3 direction = position - targetContainer.position;
        return Physics.Raycast(targetContainer.position, direction.normalized, 30, occlusionMask);
    }

    /// <summary>
    /// Gets a random nearby position around the main target within specified angular constraints.
    /// </summary>
    /// <param name="mainTarget">The main target transform.</param>
    /// <param name="minAngleDegrees">Minimum angle from the main target (in degrees).</param>
    /// <param name="maxAngleDegrees">Maximum angle from the main target (in degrees).</param>
    /// <param name="distractorWidth">Width of the distractor object.</param>
    /// <param name="distractorDepth">Depth of the distractor object.</param>
    /// <param name="radius">Output radius of the distractor object.</param>
    /// <returns>A random position nearby the main target.</returns>
    private Vector3 GetRandomNearbyPosition(Transform mainTarget, float minAngleDegrees, float maxAngleDegrees,
        float distractorWidth, float distractorDepth, out float radius)
    {
        float minRadius = CalculateRadius(mainTarget.localPosition.z, minAngleDegrees);
        float maxRadius = CalculateRadius(mainTarget.localPosition.z, maxAngleDegrees);
        float randomDistance = Random.Range(minRadius, maxRadius);

        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        radius = CalculateRadius(distractorDepth, distractorWidth);

        Vector3 offset = new Vector3(
            randomDirection.x * (randomDistance + mainTarget.localScale.x / 2 + radius),
            randomDirection.y * (randomDistance + mainTarget.localScale.x / 2 + radius),
            0);

        var position = mainTarget.localPosition + offset;
        position.z = distractorDepth;
        return position;
    }

    /// <summary>
    /// Spawns an object at the specified position with a given radius.
    /// </summary>
    /// <param name="prefab">Prefab of the object to spawn.</param>
    /// <param name="position">Position where the object will be spawned.</param>
    /// <param name="radius">Radius of the object.</param>
    /// <returns>The spawned GameObject instance.</returns>
    private GameObject SpawnObject(GameObject prefab, Vector3 position, float radius)
    {
        GameObject spawnObject = Instantiate(prefab, position, Quaternion.identity, targetContainer);
        spawnObject.transform.localScale = Vector3.one * (radius * 2);
        spawnObject.transform.localPosition = position;
        return spawnObject;
    }

    /// <summary>
    /// Calculates the radius of a circle at a given distance and angle.
    /// </summary>
    /// <param name="distance">Distance from the origin.</param>
    /// <param name="angleInDegrees">Angle in degrees.</param>
    /// <returns>The radius of the circle.</returns>
    private float CalculateRadius(float distance, float angleInDegrees)
    {
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        return distance * Mathf.Tan(angleInRadians / 2);
    }

    /// <summary>
    /// Gets a random position within a cone originating from the target container.
    /// </summary>
    /// <param name="depth">Depth of the position within the cone.</param>
    /// <returns>A random position within the cone.</returns>
    private Vector3 GetRandomPositionInCone(float depth)
    {
        float randomAngleX = Random.Range(-coneAngle / 2f, coneAngle / 2f);
        float randomAngleY = Random.Range(-coneAngle / 2f, coneAngle / 2f);

        Quaternion rotation = Quaternion.Euler(randomAngleX, randomAngleY, 0);
        Vector3 coneDirection = rotation * Vector3.forward;

        return coneDirection * depth;
    }
}