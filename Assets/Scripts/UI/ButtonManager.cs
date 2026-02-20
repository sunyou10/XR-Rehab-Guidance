using UnityEngine;

public enum SessionState { Idle, Recording, Paused }

public class ButtonManager : MonoBehaviour
{
    [Header("Tables (Group)")]
    [SerializeField] private GameObject tableIdle;
    [SerializeField] private GameObject tableRecording;
    [SerializeField] private GameObject tablePaused;

    public SessionState CurrentState { get; private set; } = SessionState.Idle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetState(SessionState.Idle);
    }

    public void SetState(SessionState state)
    {
        CurrentState = state;

        if (tableIdle) tableIdle.SetActive(false);
        if (tableRecording) tableRecording.SetActive(false);
        if (tablePaused) tablePaused.SetActive(false);
        
        switch (state)
        {
            case SessionState.Idle:
                if (tableIdle) tableIdle.SetActive(true);
                break;
            case SessionState.Recording:
                if (tableRecording) tableRecording.SetActive(true);
                break;
            case SessionState.Paused:
                if (tablePaused) tablePaused.SetActive(true);
                break;

        }
    }
}
