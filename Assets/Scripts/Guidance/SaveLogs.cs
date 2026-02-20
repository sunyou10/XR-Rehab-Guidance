using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.Hands;

public class SaveLogs : MonoBehaviour
{
    // Logheader
    private string HEADER = "[SaveLogs]";

    // Log Setting
    private float sampleHz = 10f;
    private float deadzoneMeters = 0.005f;

    // 손 관련
    private Handedness handedness;

    // 최신 pose 캐시
    private bool _latestValid;
    private long _latestTime;
    private Pose _latestPose;

    // 거리 계산
    private Vector3? _prevPos;
    private float _totalDist;

    // === csv logging 관련 설정 ===
    private StreamWriter _writer;
    private StringBuilder _sb = new StringBuilder(8 * 1024);
    private int _bufferLines = 0;
    private const int FlushEveryLines = 60;  // 버퍼에 60줄 쌓이면 한 번에 write

    private string _sessionId;
    private bool _hasSession;  // 세션이 있는지 (시작 / 완료 시 바뀜)
    private bool _isSessionOn;  // 기록 중인지 (멈춤 / 재개 시 바뀜)
    // ===

    // === FastAPI Server 전송 관련 설정 ===
    private string serverUrl = "https://unscabrously-unhermetic-amari.ngrok-free.dev/logging";
    private string _currentCsvPath;
    // ===
    private Coroutine _logRoutine;

    public void SetLatestPose(Handedness h, bool valid, Pose pose)
    {
        handedness = h;
        _latestValid = valid;
        if (!valid) return;

        _latestTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        _latestPose = pose;
    }

    public void StartSession()
    {
        if (_hasSession) return;

        _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _hasSession = true;

        _isSessionOn = true;
        _prevPos = null;
        _totalDist = 0f;

        string dir = Path.Combine(Application.persistentDataPath, "logs");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string path = Path.Combine(dir, $"handlog_{_sessionId}.csv");
        _currentCsvPath = path;
        _writer = new StreamWriter(path, false, Encoding.UTF8);
        _writer.WriteLine("session_id,timestamp_ms,event,hand,pos_x,pos_y,pos_z,delta_dist,total_dist");
        AppendEvent("START");

        Debug.Log($"{HEADER} Writing CSV Session START | CSV Path: {path}");

        if (_logRoutine == null) _logRoutine = StartCoroutine(HandLogging());
    }

    private void AppendEvent(string evt)
    {
        if (_writer == null) return;

        long time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        _sb.Append(_sessionId).Append(',')
        .Append(time).Append(',')
        .Append(evt).Append(',')
        .Append(handedness == Handedness.Right ? "R" : "L").Append(',')
        .Append("0,0,0,0,").Append(_totalDist)
        .Append('\n');

        _bufferLines++;
    }

    private IEnumerator HandLogging()
    {
        float dt = 1f / sampleHz;
        float next = Time.time;

        while (true)
        {
            next += dt;
            float wait = next - Time.time;
            if (wait > 0f) yield return new WaitForSeconds(wait);
            else yield return null;

            if (!_isSessionOn) continue;
            if (_writer == null) continue;

            if (_latestValid)
            {
                Vector3 pos = _latestPose.position;
                
                float delta = 0f;
                if (_prevPos.HasValue)
                {
                    float d = Vector3.Distance(_prevPos.Value, pos);
                    if (d >= deadzoneMeters)
                    {
                        delta = d;
                        _totalDist += d;
                    }
                }
                _prevPos = pos;

                AppendCSV(_sessionId, _latestTime, handedness == Handedness.Right? "R" : "L", pos, delta, _totalDist);
            }
            else
            {
                _prevPos = null;
                AppendCSV(_sessionId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), handedness == Handedness.Right? "R" : "L", Vector3.zero, 0f, _totalDist);
            }

            if (_bufferLines >= FlushEveryLines) Flush();
        }
    }

    private void AppendCSV(string sid, long time, string hand, Vector3 pos, float delta, float total)
    {
        _sb.Append(sid).Append(',')
            .Append(time).Append(',')
            .Append("MOVE").Append(',')
            .Append(hand).Append(',')
            .Append(pos.x).Append(',').Append(pos.y).Append(',').Append(pos.z).Append(',')
            .Append(delta).Append(',')
            .Append(total).Append('\n');

        _bufferLines ++;
    }

    private void Flush()
    {
        if (_writer == null) return;
        if (_sb.Length == 0) return;

        _writer.Write(_sb.ToString());
        _writer.Flush();
        _sb.Clear();
        _bufferLines = 0;
    }

    public void StopSession()
    {
        if (!_hasSession) return;
        AppendEvent("STOP");

        _isSessionOn = false;
        _hasSession = false;

        FlushAndClose();
        Debug.Log($"Writing CSV Session STOP =====");

        _sessionId = null;

        if (!string.IsNullOrEmpty(_currentCsvPath) && File.Exists(_currentCsvPath))
            StartCoroutine(SendCSVFile(_currentCsvPath));

        if (_logRoutine != null)
        {
            StopCoroutine(_logRoutine);
            _logRoutine = null;
        }
    }

    private void FlushAndClose()
    {
        Flush();
        _writer?.Dispose();
        _writer = null;

        _sb.Clear();
        _bufferLines = 0;
    }

    private IEnumerator SendCSVFile(string filepath)
    {
        byte[] fileBytes;
        try
        {
            fileBytes = File.ReadAllBytes(filepath);
        }
        catch (Exception e)
        {
            Debug.LogError($"CSV read failed: {e}");
            yield break;
        }

        var form = new List<IMultipartFormSection>
        {
            new MultipartFormFileSection("data", fileBytes, Path.GetFileName(filepath), "text/csv")
        };

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            www.timeout = 20;
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Sending CSV success: {www.downloadHandler.text}");

                try { File.Delete(filepath); }
                catch (Exception e) { Debug.LogWarning($"Delete failed: {e}"); }
            }
            else
            {
                Debug.LogError($"Sending CSV failed: {www.result} | {www.responseCode} | {www.error}\n{www.downloadHandler.text}");
            }
        }
    }

    public void PauseSession()
    {
        if (!_hasSession || !_isSessionOn) return;

        _isSessionOn = false;
        _prevPos = null;
        AppendEvent("PAUSE");
        Flush();

        Debug.Log($"{HEADER} Writing CSV Session Paused =====");
    }

    public void ResumeSession()
    {
        if (!_hasSession || _isSessionOn) return;

        _isSessionOn = true;

        AppendEvent("RESUME");
        Debug.Log($"{HEADER} Writing CSV Session Resumed =====");
    }

    private void OnDisable()
    {
        _isSessionOn = false;

        if (_logRoutine != null)
        {
            StopCoroutine(_logRoutine);
            _logRoutine = null;
        }

        FlushAndClose();
    }
}
