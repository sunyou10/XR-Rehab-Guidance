using UnityEngine;

public class ScoreUIConnector : MonoBehaviour
{
    [SerializeField] private ShowHandPose showHandPose;
    [SerializeField] private ScoreUI scoreUI;

    private void OnEnable()
    {
        if (showHandPose != null)
            showHandPose.OnFinished += HandleFinished;
    }

    private void HandleFinished(ShowHandPose.ScoreResult r)
    {
        if (scoreUI != null) scoreUI.Show(r);
    }

    private void OnDisable()
    {
        if (showHandPose != null)
            showHandPose.OnFinished -= HandleFinished;
    }
}
