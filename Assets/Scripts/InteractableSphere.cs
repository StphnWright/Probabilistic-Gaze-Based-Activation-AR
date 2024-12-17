using System;
using UnityEngine;
using UnityEngine.UI;

public class InteractableSphere : MonoBehaviour
{
    [SerializeField] private EETDataProvider vergeScript;
    [SerializeField] private Collider collider;
    [SerializeField] private Renderer renderer;
    [SerializeField] private Toggle raycastTool;
    private Color originalColor;
    private bool isGazedAt;

    private void Start()
    {
        originalColor = renderer.material.color;
    }

    private void Update()
    {
        if (!raycastTool.isOn)
        {
            // Virtual collider-based interaction
            HandleVirtualColliderInteraction();
        }
        else
        {
            // Raycast-based interaction
            HandleRaycastInteraction();
        }
    }

    private void HandleVirtualColliderInteraction()
    {
        if (vergeScript.intersectedVirtualColliders.Contains(collider))
        {
            renderer.material.color = Color.red;
            Debug.Log("Intersected Virtual Collider");
        }
        else
        {
            renderer.material.color = originalColor;
        }
    }

    private void HandleRaycastInteraction()
    {
        if (!isGazedAt)
        {
            renderer.material.color = originalColor;
        }
        isGazedAt = false; // Reset gaze state for the next frame
    }

    public void OnGazeHit()
    {
        if (!raycastTool.isOn) return;
        
        renderer.material.color = Color.red;
        isGazedAt = true;
    }
}