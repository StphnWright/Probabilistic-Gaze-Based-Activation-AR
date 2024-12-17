using UnityEngine;
using UnityEngine.UI;

public class VisualDebug : MonoBehaviour
{
    [SerializeField] private GameObject mlFixationPoseObject;
    [SerializeField] private GameObject fixationPoseObject;
    [SerializeField] private Toggle mlFixationPoseToggle;
    [SerializeField] private Toggle fixationPoseToggle;
    // Start is called before the first frame update
    private void Start()
    {
        InitializeToggle(mlFixationPoseToggle, mlFixationPoseObject);
        InitializeToggle(fixationPoseToggle, fixationPoseObject);
    }

    private void InitializeToggle(Toggle toggle, GameObject targetObject)
    {
        targetObject.SetActive(toggle.isOn);
        toggle.onValueChanged.AddListener(targetObject.SetActive);
    }
}
