using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothFollowHUD : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float followDistance = 2.0f;  // Distance from the user's head
    [SerializeField] private float heightOffset = 0.5f;    // Height offset relative to the user's head
    [SerializeField] private float smoothSpeed = 0.125f; // Speed of the smooth following
    [SerializeField] private float rotationXOffset = 0f; 
    private Vector3 targetPosition;
    
    private Transform userHead;    // The user's head (usually the VR camera)

    private void Start()
    {
        userHead = Camera.main.transform;
    }

    void Update()
    {
        FollowUser();
    }

    private void FollowUser()
    {
        // Calculate the target position in front of the user's head
        targetPosition = userHead.position + userHead.forward * followDistance;
        targetPosition.y = userHead.position.y + heightOffset;

        // Smoothly move the UI window to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed);

        // Make the UI window face the user
        transform.LookAt(userHead);
        transform.Rotate(rotationXOffset, 180, 0); // Rotate 180 degrees to face the user
    }
}
