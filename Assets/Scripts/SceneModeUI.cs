using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SceneModeUI : MonoBehaviour
{
    [SerializeField] private List<Toggle> buttons; // A list to hold buttons
    [SerializeField] private List<string> sceneNames; // A list to hold corresponding scene names
    [SerializeField] private SceneManager sceneManager;
    [SerializeField] private Toggle isRecording;

    private void Start()
    {
        if (buttons.Count != sceneNames.Count)
        {
            Debug.LogError("Buttons and Scene Names lists must have the same length!");
            return;
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i; // Capture the current index to avoid closure issues
            buttons[i].onValueChanged.AddListener(isOn =>
            {
                if (isOn) sceneManager.RunScene(sceneNames[index]);
            } );
        }
    }
}