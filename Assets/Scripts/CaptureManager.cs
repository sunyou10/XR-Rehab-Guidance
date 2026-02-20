using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


[Serializable]
public class UploadResponse
{
    public string image_path;
    public string run_id;
}

public class CaptureManager : MonoBehaviour
{
    [Header ("Preview")]
    [SerializeField] private GameObject livePreviewRoot;
    [SerializeField] private GameObject capturePreviewRoot;

    [Header("Server")]
    public string serverUrl = "https://unscabrously-unhermetic-amari.ngrok-free.dev/upload";

    [Header("UI")]
    [SerializeField] RawImage capturedImage;
    [SerializeField] RawImage capturedImageInResult;
    [SerializeField] private ProgressUI ui;

    [Header("Capture Source")]
    [SerializeField] private StreamPassthrough passthroughSource;

    private Texture2D _lastCapturedTex;
    private bool isResultDisplayed = false;

    [SerializeField] private ResultManager resultManager;

    public void OnCaptureButton()
    {
        if (!isResultDisplayed) StartCoroutine(CaptureAndSendCoroutine());
        else ResetUI();
    }

    IEnumerator CaptureAndSendCoroutine()
    {
        yield return new WaitForEndOfFrame();

        ui.SetProgressing(true);
        ui.SetLoading(true);

        // 지난 캡처본 삭제
        if (_lastCapturedTex != null)
        {
            Destroy(_lastCapturedTex);
            _lastCapturedTex = null;
        }


        // 캡처 및 화면 초기화
        ui.SetStage(ProgressStage.Capture);
        var jpegBytes = passthroughSource != null ? passthroughSource.LatestJpeg : null;
        if (jpegBytes == null || jpegBytes.Length == 0)
        {
            Debug.LogError("[Capture] No JPEG frame from plugin yet.");
            ui.ShowError("No JPEG frame from plugin");
            yield break;
        }

        _lastCapturedTex = new Texture2D(2, 2, TextureFormat.RGB24, false);

        if (!_lastCapturedTex.LoadImage(jpegBytes))
        {
            Debug.LogError("[Capture] LoadImage failed.");
            ui.ShowError("LoadImage failed");
            yield break;
        }

        if (livePreviewRoot) livePreviewRoot.SetActive(false);
        if (capturePreviewRoot) capturePreviewRoot.SetActive(true);

        if (capturedImage) capturedImage.texture = _lastCapturedTex;
        if (capturedImageInResult) capturedImageInResult.texture = _lastCapturedTex;       


        // 캡처 이미지 전송
        var form = new List<IMultipartFormSection>();
        form.Add(new MultipartFormFileSection("image", jpegBytes, "capture.jpg", "image/jpeg"));

        using (UnityWebRequest www = UnityWebRequest.Post(serverUrl, form))
        {
            www.timeout = 20;
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Capture] Error: {www.error}");
                ui.ShowError("REHABGEN Server Error.");
                yield break;
            }
            
            ui.SetStage(ProgressStage.Upload);
            string json = www.downloadHandler.text;
            Debug.Log("[Capture] Server response: " + json);

            UploadResponse response = null;
            try
            {
                response = JsonUtility.FromJson<UploadResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[Capture] JSON Parse Error: " + e.Message);
                ui.ShowError(e.Message);
            }

            if (response != null && resultManager != null)
            {
                StartCoroutine(resultManager.getResult(response.run_id));
                isResultDisplayed = true;
            }
            else
            {
                ui.ShowError("Parse Error");
            }
        }
    }

    private void ResetUI()
    {
        if (capturePreviewRoot) capturePreviewRoot.SetActive(false);
        if (livePreviewRoot) livePreviewRoot.SetActive(true);
        if (capturedImage != null) capturedImage.texture = null;
        ui.ResetAll();
        isResultDisplayed = false;
    }


    void Start()
    {
        if (livePreviewRoot) livePreviewRoot.SetActive(true);
        if (capturePreviewRoot) capturePreviewRoot.SetActive(false);
    }
}
