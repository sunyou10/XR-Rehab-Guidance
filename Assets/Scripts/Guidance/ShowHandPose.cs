using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Hands;

public class ShowHandPose : MonoBehaviour
{
    [Serializable]
    public class Target
    {
        public XRHandJointID jointID;
        public Transform targetSphere;
        public float radius = 0.02f;

        [HideInInspector] public Renderer renderer;
        [HideInInspector] public Material material; 
        [HideInInspector] public bool isValid;
        [HideInInspector] public int okFrames;
    }

    [SerializeField] private List<Target> targets = new();
    private int requiredFrames = 15;
    private bool requiredAllTargets = true;

    // Log Header
    private string HEADER = "[ShowHandPose]";

    // HandPoseSequence 가져오기
    [SerializeField] private int exNum;
    private HandPoseSequence _sequence;

    // HandSubsystem 받아오기 관련
    [SerializeField] private HandJointTracking handJointTracking;
    [SerializeField] private Handedness handedness = Handedness.Right;
    [SerializeField] private Transform xrOrigin;
    private XRHandSubsystem handSubsystem;
    private Coroutine _waitRoutine;

    // 앵커고정
    [SerializeField] private Transform frozenAnchor;
    public void SetFrozenAnchor(Transform t) => frozenAnchor = t;

    // UI
    [SerializeField] private GameObject textRoot;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private ButtonManager buttonManager;
    private float stepSeconds = 1f;
    private bool _isAdvancing = false;
    private Coroutine _advanceRoutine = null;
    private bool _isPaused = false;

    // 가져온 HandPoseSequence 보여주기
    [SerializeField] private GameObject rightHandHologramRoot;
    [SerializeField] private Transform rightHandHologram; // 너가 보여준 R_ 구조
    [SerializeField] private GameObject handSphereRoot;
    private int _currentIndex;
    private Dictionary<XRHandJointID, Transform> _jointMap;
    private readonly Dictionary<XRHandJointID, Quaternion> _rotCache = new();
    private readonly Dictionary<XRHandJointID, Pose> _poseCache = new();

    // 평가용
    [Serializable]
    public struct ScoreResult
    {
        public float score;
        public float timeSec;
        public float accuracy;
        public int missResets;
    }
    public event Action<ScoreResult> OnFinished;
    private float _startTime;
    private float _pauseStart;
    private float _pausedAccum;
    private int _greenFrames, _yellowFrames, _redFrames;
    private int _missResets;
    private float ElapsedPlayTime => (Time.unscaledTime - _startTime) - _pausedAccum;

    // SaveLogs
    [SerializeField] private SaveLogs saveLogs;

    private void OnEnable()
    {
        if (handJointTracking == null) { Debug.LogError($"{HEADER} HandJointTracking is null."); return; }

        if (_waitRoutine == null)
            _waitRoutine = StartCoroutine(WaitForHandSubsystem());

        // 각 타겟 초기화
        foreach (var t in targets)
        {
            t.isValid = false;
            t.okFrames = 0;

            if (t.targetSphere == null) continue;

            t.renderer = t.targetSphere.GetComponent<Renderer>();
            if (t.renderer == null) continue;

            t.material = t.renderer.material;
            t.isValid = true;

            t.material.color = Color.red;
        }

        BuildHologram();

        var path = GetSequencePath();
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log($"{HEADER} Path File is null or empty.");
            return;
        }

        _sequence = LoadSequence(path);
        if (_sequence == null || _sequence.steps == null || _sequence.steps.Count == 0) return;
        
        _currentIndex = 0;

        // 평가 지표들 초기화
        _startTime = Time.unscaledTime;
        _pausedAccum = 0f;
        _greenFrames = _yellowFrames = _redFrames = 0;
        _missResets = 0;

        Debug.Log($"{HEADER} Loaded steps: {_sequence.steps.Count} from {path}");
        ShowStep(_currentIndex);
        rightHandHologramRoot.SetActive(true);

        if (saveLogs != null) saveLogs.StartSession();
    }

    private void BuildHologram()
    {
        if (rightHandHologram == null)
        {
            Debug.LogError($"{HEADER} rightHandHologram is null.");
            return;
        }

        string prefix = (handedness == Handedness.Right)? "R_" : "L_";
        _jointMap = BuildJointMap(rightHandHologram, prefix);

        if (_jointMap == null || _jointMap.Count == 0)
            Debug.LogError($"{HEADER} Joint map is empty.");
    }

    private string GetSequencePath()
    {
        string dir = Path.Combine(Application.persistentDataPath, "poses");
        if (!Directory.Exists(dir))
        {
            Debug.LogWarning($"{HEADER} Path ({dir}) doesn't exist.");
            return null;
        }

        string suffix = (handedness == Handedness.Right)? "R" : "L";
        return Path.Combine(dir, $"{exNum}_handpose_sequence_{suffix}.json");
    }

    private HandPoseSequence LoadSequence(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"{HEADER} Path ({path}) doesn't exist.");
            return null;
        }

        var json = File.ReadAllText(path, Encoding.UTF8);
        var seq = JsonUtility.FromJson<HandPoseSequence>(json);

        if (seq == null)
        {
            Debug.LogError($"{HEADER} Failed to parse hand pose sequence: {path}");
            return null;
        }

        if (seq.steps == null)
        {
            Debug.LogError($"{HEADER} No found steps: {path}");
            return null;
        }

        seq.handedness = handedness;
        return seq;
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

    private void OnDisable()
    {
        if (_waitRoutine != null)
        {
            StopCoroutine(_waitRoutine);
            _waitRoutine = null;
        }

        if (_advanceRoutine != null)
        {
            StopCoroutine(_advanceRoutine);
            _advanceRoutine = null;
        }
        _isAdvancing = false;

        if (saveLogs != null) saveLogs.enabled = false;
    }

    public void Pause()
    {
        if (_isPaused) return;
        _isPaused = true;

        _pauseStart = Time.unscaledTime;

        if (_advanceRoutine != null)
        {
            StopCoroutine(_advanceRoutine);
            _advanceRoutine = null;
        }
        _isAdvancing = false;

        StopAllCoroutines();
        StartCoroutine(FlashMessage("PAUSED"));

        if (saveLogs) saveLogs.PauseSession();
        if (buttonManager) buttonManager.SetState(SessionState.Paused);

        Debug.Log($"{HEADER} Paused");
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _isPaused = false;

        _pausedAccum += Time.unscaledTime - _pauseStart;
        
        StartCoroutine(FlashMessage("RESUME"));

        if (saveLogs) saveLogs.ResumeSession();
        if (buttonManager) buttonManager.SetState(SessionState.Recording);

        Debug.Log($"{HEADER} Resume");
    }

    void Update()
    {
        if (_isPaused) return;

        if (_sequence == null || _sequence.steps == null || _sequence.steps.Count == 0) return;
        if (_jointMap == null || _jointMap.Count == 0) return;
        
        if (handSubsystem == null || !handSubsystem.running)
        {
            Debug.LogError($"{HEADER} Update: HandSubsystem or running subsystem is missing.");
            return;   
        }

        XRHand hand = (handedness == Handedness.Right) ? handSubsystem.rightHand : handSubsystem.leftHand;

        if (!EvaluateTargets(hand)) return;

        AdvanceStep();
    }

    private bool EvaluateTargets(XRHand hand)
    {
        int satisfiedcount = 0;
        int validCount = 0;

        foreach (var t in targets)
        {
            if (!t.isValid) continue;
            validCount ++;

            var joint = hand.GetJoint(t.jointID);
            if (!joint.TryGetPose(out Pose poseInTracking))
            {
                t.okFrames = 0;
                SetColor(t, Color.red);
                continue;
            }

            Vector3 jointWorld = poseInTracking.position;
            float d = Vector3.Distance(jointWorld, t.targetSphere.position);          

            if (d <= t.radius)
            {
                _greenFrames ++;
                t.okFrames ++;
                SetColor(t, Color.green);
            }
            else if (d <= t.radius * 1.5f)
            {
                _yellowFrames++;
                if (t.okFrames > 0) _missResets++;
                t.okFrames = 0;
                SetColor(t, Color.yellow);
            }
            else
            {
                _redFrames++;
                if (t.okFrames > 0) _missResets++;
                t.okFrames = 0;
                SetColor(t, Color.red);
            }

            if (t.okFrames >= requiredFrames) satisfiedcount++;
        }

        if (validCount == 0) return false;

        return requiredAllTargets? satisfiedcount == validCount : satisfiedcount >= Mathf.Max(1, validCount -1);
    }

    private void AdvanceStep()
    {
        if (_isAdvancing) return;

        _isAdvancing = true;

        _currentIndex ++;

        if (_advanceRoutine != null) StopCoroutine(_advanceRoutine);
        _advanceRoutine = StartCoroutine(WaitForNextStep(_currentIndex));
        
        foreach (var t in targets) t.okFrames = 0;
    }

    private void Finish()
    {
        Debug.Log($"{HEADER} Sequence finished.");

        var result = ComputeScore();
        Debug.Log($"{HEADER} score={result.score:F1}, time={result.timeSec:F2}s, acc={result.accuracy:P1}, miss={result.missResets}");

        OnFinished?.Invoke(result);

        if (saveLogs != null) saveLogs.StopSession();
    }

    private ScoreResult ComputeScore()
    {
        float timeSec = ElapsedPlayTime;

        int total = _greenFrames + _yellowFrames + _redFrames;
        float accuracy = total > 0 ? (_greenFrames + 0.5f * _yellowFrames) / total : 0f;

        float Tgoal = 90f; // 목표 시간 1.5min
        float TimeScore = 70f * Mathf.Clamp01(Tgoal / Mathf.Max(timeSec, 0.001f));
        float accScore = 30f* accuracy;
        // float missPenalty = Mathf.Min(_missResets * 1.0f, 15f);

        float score = Mathf.Clamp(TimeScore + accScore, 0f, 100f);

        return new ScoreResult
        {
            score = score,
            timeSec = timeSec,
            accuracy = accuracy,
            missResets = _missResets
        };
    }

    private IEnumerator WaitForNextStep(int index)
    {
        if (countdownText != null)
            yield return ShowCountdown();

        if (index < 0 || index >= _sequence.steps.Count) // 단계 끝났을 때
        {
            if (countdownText != null)
                yield return FlashMessage("FINISH!");

            Finish();
            if (buttonManager) buttonManager.SetState(SessionState.Idle);
            gameObject.SetActive(false);

            _isAdvancing = false;
            _advanceRoutine = null;
            yield break;
        }

        ShowStep(index);

        _isAdvancing = false;
        _advanceRoutine = null;
    }

    private IEnumerator ShowCountdown()
    {
        countdownText.gameObject.SetActive(true);

        for ( int i = 3; i >= 1; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(stepSeconds);
        }
        countdownText.gameObject.SetActive(false);
    }

    private void ShowStep(int index)
    {
        if (_sequence == null || _sequence.steps.Count == 0) return;
        if (index < 0 || index >= _sequence.steps.Count) return;

        var step = _sequence.steps[index];

        if (_jointMap.TryGetValue(XRHandJointID.Wrist, out var wristT))
        {
            var wp = step.wristPose.ToPose();
            wristT.localPosition = wp.position;
            wristT.localRotation = wp.rotation;
        }

        _rotCache.Clear();
        foreach (var jr in step.jointRots)
            _rotCache[jr.jointID] = jr.serializableQuat.ToQuaternion();

        foreach (var id in ApplyRotOrder)
        {
            if (!_rotCache.TryGetValue(id, out var q)) continue;
            if (!_jointMap.TryGetValue(id, out var t)) continue;
            t.localRotation = q;
        }

        ShowSphere(index);
    }

    private void ShowSphere(int index)
    {
        var step = _sequence.steps[index];
        if (step.jointPoses == null) return;

        _poseCache.Clear();
        foreach (var jp in step.jointPoses)
            _poseCache[jp.jointID] = jp.serializablePose.ToPose();

        foreach (var t in targets)
        {
            if (t == null || t.targetSphere == null) continue;

            if (!_poseCache.TryGetValue(t.jointID, out var p)) continue;

            t.targetSphere.localPosition = p.position;
        }

        handSphereRoot.SetActive(true);
    }

    Dictionary<XRHandJointID, Transform> BuildJointMap(Transform root, string prefix)
    {
        var map = new Dictionary<XRHandJointID, Transform>();

        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var core = t.name.Substring(prefix.Length); // R_, L_ 같은 프리픽스 빼고 매핑
            if (Enum.TryParse(core, out XRHandJointID id))
                map[id] = t;
        }

        return map;
    }

    private IEnumerator FlashMessage(string msg)
    {
        if (countdownText == null) yield break;
        
        countdownText.gameObject.SetActive(true);
        countdownText.text = msg;
        yield return new WaitForSecondsRealtime(0.7f);
        countdownText.gameObject.SetActive(false);
    }
    
    private void SetColor(Target t, Color c)
    {
        if (t.material != null && t.material.color != c)
        {
            t.material.color = c;
        }
    }

    static readonly XRHandJointID[] ApplyRotOrder =
    {
        XRHandJointID.Wrist,
        XRHandJointID.Palm,

        XRHandJointID.ThumbMetacarpal, XRHandJointID.ThumbProximal, XRHandJointID.ThumbDistal, XRHandJointID.ThumbTip,
        XRHandJointID.IndexMetacarpal, XRHandJointID.IndexProximal, XRHandJointID.IndexIntermediate, XRHandJointID.IndexDistal, XRHandJointID.IndexTip,
        XRHandJointID.MiddleMetacarpal, XRHandJointID.MiddleProximal, XRHandJointID.MiddleIntermediate, XRHandJointID.MiddleDistal, XRHandJointID.MiddleTip,
        XRHandJointID.RingMetacarpal, XRHandJointID.RingProximal, XRHandJointID.RingIntermediate, XRHandJointID.RingDistal, XRHandJointID.RingTip,
        XRHandJointID.LittleMetacarpal, XRHandJointID.LittleProximal, XRHandJointID.LittleIntermediate, XRHandJointID.LittleDistal, XRHandJointID.LittleTip,
    };
}
