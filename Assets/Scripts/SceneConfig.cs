using UnityEngine;

[CreateAssetMenu(fileName = "SceneConfig", menuName = "SceneGeneration/SceneConfig")]
public class SceneConfig : ScriptableObject
{
    public string sceneName;             // Name of the scene/mode
    public Vector2 targetWidthRange;     // Min and max target width in degrees
    public Vector2 targetDepthRange;     // Min and max target depth in meters
    public int distractorCount;          // Number of distractors
    public float minDepthDifference;        // Depth difference for distractors
    public float maxDepthDifference;        // Depth difference for distractors
    public string focusDescription;      // Description of the focus of the mode
}