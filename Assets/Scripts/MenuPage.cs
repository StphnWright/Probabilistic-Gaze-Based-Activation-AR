using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MenuPage : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private InputActionProperty bumperInputAction;
    private void Start()
    {
        bumperInputAction.action.Enable();
        bumperInputAction.action.performed += OnTriggerPerformed;
    }

    private void OnTriggerPerformed(InputAction.CallbackContext obj)
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

    private void UpdateCanvasGroupVisibility(bool isActive)
    {
        canvasGroup.alpha = isActive ? 1 : 0;
        canvasGroup.interactable = isActive;
    }
}