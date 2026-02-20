using UnityEngine;

public class PokeButtonAction : MonoBehaviour
{
    // Log Header
    private string HEADER = "[PokeButton]";

    public enum ActionType { Start, Pause, Resume }

    [SerializeField] private ActionType actionType;
    [SerializeField] private ButtonManager buttonManager;
    [SerializeField] private AutoLockAnchor lockAnchor;
    [SerializeField] private ShowHandPose showHandPose;

    // Debounce
    private float cooldownSec = 0.5f;
    private static float _cooldownUntil = -999f;

    public void InvokeAction()
    {
        if (Time.time < _cooldownUntil) return;
        _cooldownUntil = Time.time + cooldownSec;

        if (lockAnchor == null)
        {
            Debug.LogWarning($"{HEADER} AutoLockAnchor not assigned.");
            return;
        }

        switch (actionType)
        {
            case ActionType.Start:
                lockAnchor.OnClick_Start();
                break;
            
            case ActionType.Pause:
                showHandPose.Pause();
                break;

            case ActionType.Resume:
                showHandPose.Resume();
                break;
        }
    }
}
