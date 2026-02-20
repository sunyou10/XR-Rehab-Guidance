using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Android;
using TMPro;

public class StreamPassthrough : MonoBehaviour
{
    [SerializeField] private RawImage LiveRawImage;
    [SerializeField] private int captureWidth = 1280;
    [SerializeField] private int captureHeight = 720;
    [SerializeField] private float startDelaySeconds = 1f;
    [SerializeField] private TMP_Dropdown cameraIdDropdown;

    public byte[] LatestJpeg { get; private set; }

    private Texture2D cameraTexture;

    #if UNITY_ANDROID && !UNITY_EDITOR
        private const string PluginClassName = "com.example.cameraplugin.CameraPlugin";
        private AndroidJavaClass pluginClass;
        private AndroidJavaObject unityActivity;
        private bool isInitialized;
    #endif

    private void Start()
    {
        if (LiveRawImage == null)
        {
            Debug.LogError("[GalaxyXRPassthroughRenderer] Target renderer is not assigned.");
            enabled = false;
            return;
        }

        cameraTexture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        LiveRawImage.texture = cameraTexture;

        StartCoroutine(InitFlow());
    }

    private IEnumerator InitFlow()
    {
        yield return new WaitForSeconds(startDelaySeconds);

        #if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera)){
                bool finished = false;
                bool granted = false;

                var callbacks = new PermissionCallbacks();
                callbacks.PermissionGranted += _ => {
                    granted = true; finished = true;
                };
                callbacks.PermissionDenied += _ => {
                    granted = false; finished = true;
                };
                callbacks.PermissionDeniedAndDontAskAgain += _ =>{
                    granted = false; finished = true;
                };

                Permission.RequestUserPermission(Permission.Camera, callbacks);

                while (!finished) yield return null;

                if (!granted){
                    Debug.LogError("[StreamPassthrough] Camera permission denied.");
                    yield break;
                }
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer")){
            unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }
            
            // Java plugin calass object
            pluginClass = new AndroidJavaClass(PluginClassName);

            if (cameraIdDropdown != null) {
                cameraIdDropdown.onValueChanged.RemoveListener(OnCameraDropdownChanged);
                cameraIdDropdown.onValueChanged.AddListener(OnCameraDropdownChanged);
            }
            string initialId = "0";
            if (cameraIdDropdown != null && cameraIdDropdown.options.Count > 0){
                initialId = ExtractCameraId(cameraIdDropdown.options[cameraIdDropdown.value].text) ?? "0";
            }

            pluginClass.CallStatic("setCameraId", initialId);
            pluginClass.CallStatic("start", unityActivity, captureWidth, captureHeight);
            isInitialized = true;
            
            Debug.Log($"[StreamPassthrough] Started with cameraId= {initialId}");
        #endif
    }

    #if UNITY_ANDROID && !UNITY_EDITOR
    private void OnCameraDropdownChanged(int index)
    {
        if (pluginClass == null || unityActivity == null) return;
        if (cameraIdDropdown == null || index < 0 || index >= cameraIdDropdown.options.Count) return;

        string label = cameraIdDropdown.options[index].text;
        string id = ExtractCameraId(label);

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError($"[StreamPassthrough] Failed to parse camera id from '{label}'");
            return;
        }

        Debug.Log($"[StreamPassthrough] Switching camera to id={id} (label='{label}')");

        pluginClass.CallStatic("setCameraId", id);

        if (isInitialized)
        {
            pluginClass.CallStatic("restart", unityActivity, captureWidth, captureHeight);
        }
        else
        {
            pluginClass.CallStatic("start", unityActivity, captureWidth, captureHeight);
            isInitialized = true;
        }
    }

    private string ExtractCameraId(string label)
    {
        foreach (char c in label)
        {
            if (char.IsDigit(c)) return c.ToString();
        }
        return null;
    }
    #endif

    private void OnDestroy()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            pluginClass?.CallStatic("stop");
            pluginClass = null;
            unityActivity = null;
            isInitialized = false;
        #endif
    }

    private void Update()
    {
        #if UNITY_ANDROID && !UNITY_EDITOR
            if (!isInitialized || pluginClass == null){
                return;
            }

            if (LiveRawImage == null || !LiveRawImage.gameObject.activeInHierarchy) return;

            var jpegSBytes = pluginClass.CallStatic<sbyte[]>("getLatestJpeg");
            if (jpegSBytes == null || jpegSBytes.Length == 0){
                return;
            }

            var jpegBytes = new byte[jpegSBytes.Length];
            Buffer.BlockCopy(jpegSBytes, 0, jpegBytes, 0, jpegSBytes.Length);

            LatestJpeg = jpegBytes;

            if (cameraTexture.LoadImage(jpegBytes)){
                LiveRawImage.texture = cameraTexture;
            }
        #endif
    }
}
