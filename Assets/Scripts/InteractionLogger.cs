using System.Collections;
using UnityEngine;

using System.Collections.Generic;
using UnityEngine;

public class InteractionLogger : MonoBehaviour
{
    [SerializeField] private Transform gazeFixation;
    [SerializeField] private SceneManager sceneManager;
    // Start is called before the first frame update
    private float _durationToSelect = 5;
    private float _fixationTime = 0;
    private bool _isFocused = false;

    // Update is called once per frame
    void Update()
    {
        if (SphereTools.AreSphereColliding(gazeFixation, sceneManager.Target))
        {
            _isFocused = true;
            _fixationTime += Time.deltaTime;
            if (_fixationTime >= _durationToSelect)
            {
                sceneManager.RunScene(sceneManager.CurrentConfig.name);
                _fixationTime = 0;
            }
        }
        else
        {
            _isFocused = false;
            _fixationTime = 0;
        }
        
    }
}
