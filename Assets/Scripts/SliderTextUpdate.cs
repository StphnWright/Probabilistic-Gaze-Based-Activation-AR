using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderTextUpdate : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text valueText;
    // Start is called before the first frame update
    void Start()
    {
      valueText.text = slider.value.ToString();
      slider.onValueChanged.AddListener(UpdateValueText);
    }

    private void UpdateValueText(float value)
    {
        valueText.text = value.ToString("F2");
    }
}
