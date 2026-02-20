using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.XR.Hands;

// ==== Data-Structure ====
[Serializable]
public class HandPoseSequence
{
    public Handedness handedness;
    public List<HandPoseSnapshot> steps = new();
}

[Serializable]
public class HandPoseSnapshot
{
    public SerializablePose wristPose;
    public List<JointPose> jointPoses = new();
    public List<JointRot> jointRots = new();
}

[Serializable]
public struct JointRot
{
    public XRHandJointID jointID;
    public SerializableQuat serializableQuat;
}

[Serializable]
public struct JointPose
{
    public XRHandJointID jointID;
    public SerializablePose serializablePose;
}

[Serializable]
public struct SerializablePose
{
    public Vector3 position;
    public SerializableQuat rotation;
    public Pose ToPose() => new Pose(position, rotation.ToQuaternion());
    public static SerializablePose FromPose(Pose p) => new SerializablePose
    {
        position = p.position,
        rotation = SerializableQuat.FromQuaternion(p.rotation)
    };
}

[Serializable]
public struct SerializableQuat
{
    public float x, y, z, w;
    public Quaternion ToQuaternion() => new Quaternion(x, y, z, w);
    public static SerializableQuat FromQuaternion(Quaternion q) => new SerializableQuat
    {
        x = q.x,
        y = q.y,
        z = q.z,
        w = q.w
    };
}
// ========


public class CaptureHandPose : MonoBehaviour
{
    // 로그
    private string HEADER = "[CaptureHandPose]";

    // 핸드 관련
    [SerializeField] private HandJointTracking handJointTracking;
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private Transform xrOrigin;
    private XRHandSubsystem handSubsystem;

    // 앵커 고정
    private Transform frozenAnchor;
    public void SetFrozenAnchor(Transform t) => frozenAnchor = t;


    // 운동 예제 번호
    [SerializeField] private int exNum;

    private void OnEnable()
    {
        if (handJointTracking == null) { Debug.LogError($"{HEADER} HandJointTracking is null."); return; }
        else if (handSubsystem == null) StartCoroutine(WaitForHandSubsystem());
    }

    private IEnumerator WaitForHandSubsystem(float timeoutSec = 5f)
    {
        float elapsed = 0f;

        while (handSubsystem == null && elapsed < timeoutSec)
        {
            handSubsystem = handJointTracking.handSubsystem;
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (handSubsystem == null) Debug.LogError($"{HEADER} XRHandSubsystem not found (timeout).");
    }

    public void OnClick_AddStep()
    {
        if (handSubsystem == null || !handSubsystem.running)
        {
            Debug.LogError($"{HEADER} OnClick_AddStep: HandSubsystem or running subsystem is missing.");
            return;   
        }

        XRHand hand = (handedness == Handedness.Right)? handSubsystem.rightHand : handSubsystem.leftHand;
        
        var snapshot = CaptureSnapshot(hand);
        if (snapshot == null)
        {
            Debug.LogWarning($"{HEADER} OnClick_AddStep: Capture failed.");
            return;
        }

        var path = GetSequencePath();
        var seq = LoadOrCreateSequence(path);

        seq.steps.Add(snapshot);

        File.WriteAllText(path, JsonUtility.ToJson(seq, true), Encoding.UTF8);
        Debug.Log($"Added step #{seq.steps.Count - 1} -> {path}");
    }

    private HandPoseSequence LoadOrCreateSequence(string path)
    {
        if (!File.Exists(path))
            return new HandPoseSequence { handedness = handedness, steps = new List<HandPoseSnapshot>() };
        
        var json = File.ReadAllText(path, Encoding.UTF8);
        var seq = JsonUtility.FromJson<HandPoseSequence>(json);

        if (seq == null) seq = new HandPoseSequence { handedness = handedness };
        if (seq.steps == null) seq.steps = new List<HandPoseSnapshot>();
        seq.handedness = handedness;

        return seq;
    }

    private string GetSequencePath()
    {
        string dir = Path.Combine(Application.persistentDataPath, "poses");

        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string suffix = (handedness == Handedness.Right) ? "R" : "L";
        return Path.Combine(dir, $"{exNum}_handpose_sequence_{suffix}.json");
    }

    private HandPoseSnapshot CaptureSnapshot(XRHand hand)
    {
        if (frozenAnchor == null)
        {
            Debug.LogError($"{HEADER} CaptureSnapshot: FrozenAnchor is not assigned.");
            return null;
        }

        if (!TryGetFrozenPose(hand, XRHandJointID.Wrist, out var pose)) return null;

        Vector3 p = pose.position;
        Quaternion q = pose.rotation;

        var snap = new HandPoseSnapshot
        {
            wristPose = new SerializablePose
            {
                position = p,
                rotation = SerializableQuat.FromQuaternion(q)
            },
            jointPoses = new List<JointPose>(PoseIds.Length),
            jointRots = new List<JointRot>(JointIds.Length)
        };

        // 구 모형 두는 joints (손목, 손가락) -> pose, rot 둘 다 기록
        foreach (var id in PoseIds)
        {
            if (!TryGetFrozenPose(hand, id, out var pose1)) continue;

            snap.jointPoses.Add(new JointPose
            {
                jointID = id,
                serializablePose = SerializablePose.FromPose(pose1)
            });
        }

        foreach (var id in JointIds)
        {
            if (!TryGetFrozenPose(hand, id, out var childPos)) continue;

            Quaternion localRot;
            var parent = GetParent(id);

            if (parent.HasValue && TryGetFrozenPose(hand, parent.Value, out var parentPos))
                localRot = Quaternion.Inverse(parentPos.rotation) * childPos.rotation;
            else
                localRot = childPos.rotation;

            snap.jointRots.Add(new JointRot
            {
                jointID = id,
                serializableQuat = SerializableQuat.FromQuaternion(localRot)
            });
        }

        return snap;
    }

    private bool TryGetFrozenPose(XRHand hand, XRHandJointID id, out Pose pose)
    {
        pose = default;
        var joint = hand.GetJoint(id);
        if (!joint.TryGetPose(out Pose poseInTracking)) return false;

        Vector3 worldPos =  xrOrigin.TransformPoint(poseInTracking.position);
        Quaternion worldRot = xrOrigin.rotation * poseInTracking.rotation;

        Vector3 localPos = frozenAnchor.InverseTransformPoint(worldPos);
        Quaternion localRot = Quaternion.Inverse(frozenAnchor.rotation) * worldRot;

        pose = new Pose(localPos, localRot);
        return true;
    }

    public void OnClick_ClearSequence()
    {
        var path = GetSequencePath();
        File.WriteAllText(path, JsonUtility.ToJson(new HandPoseSequence { handedness = handedness, steps = new List<HandPoseSnapshot>() }, true), Encoding.UTF8);
        Debug.Log($"Cleared sequence: {path}");
    }


    // ===== About Joint ====
    static readonly XRHandJointID[] JointIds =
    {
        XRHandJointID.Wrist,
        XRHandJointID.Palm,

        XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip,
        XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
        XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
        XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
        XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
    };

    static XRHandJointID? GetParent(XRHandJointID id)
    {
        if (id == XRHandJointID.Wrist) return null;

        if (id == XRHandJointID.Palm) return XRHandJointID.Wrist;

        // Thumb
        if (id == XRHandJointID.ThumbMetacarpal) return XRHandJointID.Wrist;
        if (id == XRHandJointID.ThumbProximal) return XRHandJointID.ThumbMetacarpal;
        if (id == XRHandJointID.ThumbDistal) return XRHandJointID.ThumbProximal;
        if (id == XRHandJointID.ThumbTip) return XRHandJointID.ThumbDistal;

        // Index
        if (id == XRHandJointID.IndexMetacarpal) return XRHandJointID.Wrist;
        if (id == XRHandJointID.IndexProximal) return XRHandJointID.IndexMetacarpal;
        if (id == XRHandJointID.IndexIntermediate) return XRHandJointID.IndexProximal;
        if (id == XRHandJointID.IndexDistal) return XRHandJointID.IndexIntermediate;
        if (id == XRHandJointID.IndexTip) return XRHandJointID.IndexDistal;

        // Middle
        if (id == XRHandJointID.MiddleMetacarpal) return XRHandJointID.Wrist;
        if (id == XRHandJointID.MiddleProximal) return XRHandJointID.MiddleMetacarpal;
        if (id == XRHandJointID.MiddleIntermediate) return XRHandJointID.MiddleProximal;
        if (id == XRHandJointID.MiddleDistal) return XRHandJointID.MiddleIntermediate;
        if (id == XRHandJointID.MiddleTip) return XRHandJointID.MiddleDistal;

        // Ring
        if (id == XRHandJointID.RingMetacarpal) return XRHandJointID.Wrist;
        if (id == XRHandJointID.RingProximal) return XRHandJointID.RingMetacarpal;
        if (id == XRHandJointID.RingIntermediate) return XRHandJointID.RingProximal;
        if (id == XRHandJointID.RingDistal) return XRHandJointID.RingIntermediate;
        if (id == XRHandJointID.RingTip) return XRHandJointID.RingDistal;

        // Little
        if (id == XRHandJointID.LittleMetacarpal) return XRHandJointID.Wrist;
        if (id == XRHandJointID.LittleProximal) return XRHandJointID.LittleMetacarpal;
        if (id == XRHandJointID.LittleIntermediate) return XRHandJointID.LittleProximal;
        if (id == XRHandJointID.LittleDistal) return XRHandJointID.LittleIntermediate;
        if (id == XRHandJointID.LittleTip) return XRHandJointID.LittleDistal;

        return null;
    }

    // 구 모형 둘 joints (위치 필요)
    static readonly XRHandJointID[] PoseIds =
    {
        XRHandJointID.Wrist,
        XRHandJointID.ThumbTip,
        XRHandJointID.IndexTip,
        XRHandJointID.MiddleTip,
        XRHandJointID.RingTip,
        XRHandJointID.LittleTip
    };
}
