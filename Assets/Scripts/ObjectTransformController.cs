using System;
using UnityEngine;
using UnityEngine.UI;

public class ObjectTransformController : MonoBehaviour
{
    [SerializeField] private GameObject targetObject; // Assign the object you want to control
    [SerializeField] private Slider scaleSlider;
    [SerializeField] private Slider positionXSlider;
    [SerializeField] private Slider positionYSlider;
    [SerializeField] private Slider positionZSlider;

    public float XSliderValue => positionXSlider.value;
    public float YSliderValue => positionYSlider.value;
    public float ZSliderValue => positionZSlider.value;
    public float RSliderValue => scaleSlider.value;

    private Vector3 initialPosition;
    private Vector3 initialScale;

    void Start()
    {
        // Store the initial position and scale of the target object
        initialPosition = targetObject.transform.position;
        initialScale = targetObject.transform.localScale;

        // Initialize sliders
        SetupScaleSlider();
        SetupPositionSlider(positionXSlider);
        SetupPositionSlider(positionYSlider);
        SetupPositionSlider(positionZSlider);
    }

    void SetupScaleSlider()
    {
        scaleSlider.minValue = 0.1f;
        scaleSlider.maxValue = 1f;
        scaleSlider.value = initialScale.x; // Assume uniform scaling
        scaleSlider.onValueChanged.AddListener(OnScaleChanged);
    }

    void SetupPositionSlider(Slider slider)
    {
        slider.minValue = -1f;
        slider.maxValue = 1f;
        slider.value = 0f;
        slider.onValueChanged.AddListener(OnPositionChanged);
    }

    void OnScaleChanged(float value)
    {
        targetObject.transform.localScale = new Vector3(value, value, value);
    }

    void OnPositionChanged(float value)
    {
        targetObject.transform.position = initialPosition + new Vector3(
            positionXSlider.value,
            positionYSlider.value,
            positionZSlider.value
        );
    }
}