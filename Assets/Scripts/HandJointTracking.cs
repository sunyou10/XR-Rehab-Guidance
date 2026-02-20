using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Hands;

public class HandJointTracking : MonoBehaviour
{
    // Logheader
    private string HEADER = "[HandJointTracking]";
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private XRHandJointID handJointID = XRHandJointID.Palm;

    [SerializeField] private bool ShowPlacement = true;
    [SerializeField] private GameObject placementPrefab;
    [SerializeField] private Transform placementParent;
    private GameObject placementInstance;
    private TMP_Text placementText;

    public XRHandSubsystem handSubsystem { get; private set; }

    private Coroutine _handRoutine;

    // SaveLogs
    [SerializeField] private SaveLogs saveLogs;

    private void OnEnable()
    {
        if (_handRoutine == null) _handRoutine = StartCoroutine(WaitForHandSubsystem());
    }

    private IEnumerator WaitForHandSubsystem(float timeoutSec = 5f)
    {
        float elapsed = 0f;
        List<XRHandSubsystem> subsystems = new();

        while (handSubsystem == null && elapsed < timeoutSec)
        {
            SubsystemManager.GetSubsystems(subsystems);
            foreach(var sub in subsystems)
            {
                if (sub != null && sub.running)
                {
                    handSubsystem = sub;
                    Debug.Log($"{HEADER} XRHandSubsystem Ready!");
                    break;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (handSubsystem == null) Debug.LogError($"{HEADER} XRHandSubsystem not found (timeout).");
    }

    private void Update()
    {
        if (handSubsystem == null) return;

        XRHand hand = (handedness == Handedness.Right) ? handSubsystem.rightHand : handSubsystem.leftHand;
        if (!hand.isTracked) 
        { 
            // Debug.LogWarning($"{HEADER} Hand is not tracked."); 
            return; 
        }

        XRHandJoint joint = hand.GetJoint(handJointID);
        if (!joint.TryGetPose(out Pose pose))
        {
            if (saveLogs != null) saveLogs.SetLatestPose(handedness, false, default);
            Debug.LogWarning($"{HEADER} Hand Pose not found.");
            return;
        }

        if (saveLogs != null) saveLogs.SetLatestPose(handedness, true, pose);

        if (ShowPlacement)
        {
            if (placementInstance == null)
            {
                placementInstance = Instantiate(placementPrefab, placementParent);

                placementText = placementInstance.GetComponentInChildren<TMP_Text>(true); 
            }

            placementInstance.transform.localPosition = pose.position;
            placementInstance.transform.localRotation = Quaternion.LookRotation(placementInstance.transform.position - Camera.main.transform.position);

            if (placementText != null)
            {
                Vector3 p = pose.position;
                placementText.text = $"x: {p.x:F3}\ny: {p.y:F3}\nz: {p.z:F3}";
            }   
        }
    }
}
