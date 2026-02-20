using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


[Serializable]
public class VlmResponse
{
    public string description;
}

public class XRCaptureManager : MonoBehaviour
{
    [Header ("Preview Toggle")]
    [SerializeField] private GameObject livePreviewRoot;
    [SerializeField] private GameObject capturePreviewRoot;

    [Header("Server")]
    public string serverUrl = "https://unscabrously-unhermetic-amari.ngrok-free.dev/upload";

    [Header("UI")]
    public RawImage previewImage;
    public TMP_Text resultText;
    public TMP_Text buttonText;

    [Header("Capture Source")]
    [SerializeField] private StreamPassthrough passthroughSource;

    private Texture2D _lastCapturedTex;
    private bool isResultDisplayed = false;

    public void OnCaptureButton()
    {
        if (!isResultDisplayed) StartCoroutine(CaptureAndSendCoroutine());
        else ResetUI();
    }

    public IEnumerator CaptureAndSendCoroutine()
    {
        yield return new WaitForEndOfFrame();

        // 지난 캡처본 삭제
        if (_lastCapturedTex != null)
        {
            Destroy(_lastCapturedTex);
            _lastCapturedTex = null;
        }


        // 캡처 및 화면 초기화
        var jpegBytes = passthroughSource != null ? passthroughSource.LatestJpeg : null;
        if (jpegBytes == null || jpegBytes.Length == 0)
        {
            Debug.LogError("[Capture] No JPEG frame from plugin yet.");
            if (buttonText) buttonText.text = "No Frame";
            yield break;
        }

        _lastCapturedTex = new Texture2D(2, 2, TextureFormat.RGB24, false);

        if (!_lastCapturedTex.LoadImage(jpegBytes))
        {
            Debug.LogError("[Capture] LoadImage failed.");
            if (buttonText) buttonText.text = "Decode Error";
            yield break;
        }

        if (livePreviewRoot) livePreviewRoot.SetActive(false);
        if (capturePreviewRoot) capturePreviewRoot.SetActive(true);

        if (previewImage) previewImage.texture = _lastCapturedTex;
        if (resultText) resultText.text = "";
        if (buttonText) buttonText.text = "Analyzing...";


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
                if (buttonText != null) buttonText.text = "Error";
                yield break;
            }

            string json = www.downloadHandler.text;
            Debug.Log("[Capture] Server response: " + json);

            VlmResponse response = null;
            try
            {
                response = JsonUtility.FromJson<VlmResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[Capture] JSON Parse Error: " + e.Message);
            }

            if (response != null)
            {
                if (resultText) resultText.text = response.description;
                if (buttonText != null) buttonText.text = "Reset";
                isResultDisplayed = true;
            }
            else
            {
                if (buttonText != null)
                {
                    buttonText.text = "Parse Error";
                }
            }
        }
    }

    private void ResetUI()
    {
        if (capturePreviewRoot) capturePreviewRoot.SetActive(false);
        if (livePreviewRoot) livePreviewRoot.SetActive(true);
        if (previewImage != null) previewImage.texture = null;
        if (resultText != null) resultText.text = "Result will be shown in this place";
        if (buttonText != null) buttonText.text = "Capture";
        isResultDisplayed = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (livePreviewRoot) livePreviewRoot.SetActive(true);
        if (capturePreviewRoot) capturePreviewRoot.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
