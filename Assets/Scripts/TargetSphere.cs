using UnityEngine;

public class TargetSphere : MonoBehaviour
{
    [SerializeField] private Collider collider;
    [SerializeField] private Renderer renderer;
    
    private Color originalColor;
    private bool interactWithGaze;
    private void Start()
    {
        originalColor = renderer.material.color;
    }
    
    private void Update()
    {
        CheckForGazeInteraction();
        HandleVirtualColliderInteraction();
    }
    
    private void CheckForGazeInteraction()
    {
        // Use Physics.OverlapSphere to find objects colliding with this collider
        Collider[] hits = Physics.OverlapSphere(transform.position, collider.bounds.extents.x);

        interactWithGaze = false; // Reset interaction state
        
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Gaze"))
            {
                interactWithGaze = true; // Found an object with the "gaze" tag
                break;
            }
        }
    }
    private void HandleVirtualColliderInteraction()
    {
        if (interactWithGaze)
        {
            renderer.material.color = Color.green;
            Debug.Log("Intersected Virtual Collider");
        }
        else
        {
            renderer.material.color = originalColor;
        }
    }
}