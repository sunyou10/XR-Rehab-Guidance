using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Hands;

public class AutoLockAnchor : MonoBehaviour
{
    // 로그 헤더
    private string HEADER = "[AutoLockAnchor]";

    // HandSubsystem 받아오기 관련
    [SerializeField] private HandJointTracking handJointTracking;
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private Transform xrOrigin;
    private XRHandSubsystem handSubsystem;

    // 앵커 고정
    public bool IsLocked { get; private set; }
    [SerializeField] private Transform frozenAnchor;
    [SerializeField] private Transform guideWrist;

    // UI
    [SerializeField] private TMP_Text countDownText;
    [SerializeField] private ButtonManager buttonManager;

    // 캡처
    [SerializeField] private CaptureHandPose capture;

    // Show
    [SerializeField] private ShowHandPose showHand;

    // 손 따라오기 설정
    [SerializeField] private Transform virtualHandRoot;

    // CountDown
    private float lockCountdownSeconds = 3f;
    private Vector3 _candidatePos;
    private Quaternion _candidateRot;
    private Coroutine _lockCountdownRoutine;

    private Coroutine _waitRoutine;

    private void OnEnable()
    {
        if (handJointTracking == null) { Debug.LogError($"{HEADER} HandJointTracking is null."); return; }
        
        if (_waitRoutine == null)
        _waitRoutine = StartCoroutine(WaitForHandSubsystem());
    }

    private IEnumerator WaitForHandSubsystem(float timeoutSec = 5f)
    {
        float elapsed = 0f;

        while (elapsed < timeoutSec)
        {
            var hs = handJointTracking.handSubsystem; // HandJointTracking이 잡아둔 것 사용
            if (hs != null && hs.running)
            {
                handSubsystem = hs;
                Debug.Log($"{HEADER} XRHandSubsystem Ready (running)!");
                _waitRoutine = null;
                yield break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        _waitRoutine = null;
        if (handSubsystem == null) Debug.LogError($"{HEADER} XRHandSubsystem not found (timeout).");
    }

    // 앵커 세팅 시작
    public void OnClick_Start()
    {
        if (IsLocked) return;

        if (!IsHandReady(out var jointWorld, out var jointWorldRot))
        {
            CancelLockCountdown();
            return;
        }

        if (virtualHandRoot != null)
        {
            virtualHandRoot.SetPositionAndRotation(jointWorld, jointWorldRot);
            virtualHandRoot.gameObject.SetActive(true);
        }

        _candidatePos = jointWorld;
        _candidateRot = jointWorldRot;
        
        if ( _lockCountdownRoutine == null) _lockCountdownRoutine = StartCoroutine(LockCountdown());
    }

    private bool IsHandReady(out Vector3 worldPos, out Quaternion worldRot)
    {
        worldPos = default;
        worldRot = default;

        if (handSubsystem == null || !handSubsystem.running)
        {
            Debug.LogError($"{HEADER} Update: HandSubsystem or running subsystem is missing.");
            return false;   
        }

        XRHand hand = (handedness == Handedness.Right) ? handSubsystem.rightHand : handSubsystem.leftHand;

        var joint = hand.GetJoint(XRHandJointID.Wrist);
        if (!joint.TryGetPose(out Pose poseInTracking)) return false;

        worldPos = xrOrigin.TransformPoint(poseInTracking.position);
        worldRot = xrOrigin.rotation * poseInTracking.rotation;

        return true;
    }

    private IEnumerator LockCountdown()
    {
        if (countDownText != null) countDownText.gameObject.SetActive(true);

        float remain = lockCountdownSeconds;

        while (remain > 0f)
        {
            int sec = Mathf.CeilToInt(remain);
            if (countDownText != null) countDownText.text = sec.ToString();

            remain -= Time.deltaTime;
            yield return null;
        }

        if (!LockNowAt(_candidatePos, _candidateRot))
        {
            CancelLockCountdown();
            yield break;
        }

        if (countDownText != null)
        {
            countDownText.text = "EXERCISE START!";
            yield return new WaitForSeconds(0.7f);
            countDownText.gameObject.SetActive(false);
        }

        if (buttonManager) buttonManager.SetState(SessionState.Recording);
        gameObject.SetActive(false);

        _lockCountdownRoutine = null;
    }

    private bool LockNowAt(Vector3 worldPos, Quaternion worldRot)
    {
        if (frozenAnchor == null)
        {
            Debug.LogError($"{HEADER} FrozenAnchor is missing.");
            return false;
        }

        frozenAnchor.SetPositionAndRotation(worldPos, worldRot);
        IsLocked = true;

        Debug.Log($"{HEADER} Locked!");

        if (capture != null)
        {
            capture.enabled = true;
            capture.SetFrozenAnchor(frozenAnchor);
        }

        if (showHand != null)
        {
            showHand.enabled = true;
            showHand.SetFrozenAnchor(frozenAnchor);      
        }

        return true;
    }

    private void CancelLockCountdown()
    {
        if (_lockCountdownRoutine != null)
        {
            StopCoroutine(_lockCountdownRoutine);
            _lockCountdownRoutine = null;
        }

        if (countDownText != null)
        {
            countDownText.gameObject.SetActive(false);
        }
    }
}
