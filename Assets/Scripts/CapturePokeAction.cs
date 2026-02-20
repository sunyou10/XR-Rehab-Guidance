using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class CapturePokeAction : MonoBehaviour
{
    [SerializeField] private CaptureManager captureManager;
    [SerializeField] private XRSimpleInteractable interactable;
    [SerializeField] private TMP_Text buttonText;

    private float cooldownSec = 0.25f;
    private static float _cooldownUntil  = -999f;

    private bool _locked;

    private void Reset()
    {
        // 같은 오브젝트에 XRSimpleInteractable 붙어있으면 자동 할당
        interactable = GetComponent<XRSimpleInteractable>();
    }

    private void SetLocked(bool locked)
    {
        _locked = locked;
        interactable.enabled = !locked;
    }

    public void InvokeAction()
    {
        if (_locked) return;
        if (Time.time < _cooldownUntil) return;
        _cooldownUntil = Time.time + cooldownSec;

        if (captureManager == null)
        {
            Debug.LogWarning("Capture Manager not assigned.");
            return;
        }

        if (interactable == null)
        {
            Debug.LogWarning("XRSimpleInteractable not assigned.");
            return;
        }

        SetLocked(true);
        if(buttonText) buttonText.text = "Analyzing...";

        captureManager.OnCaptureButton();
    }
}
