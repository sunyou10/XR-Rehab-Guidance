using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ProgressStage
{
    Capture = 1,
    Upload = 2,
    Receive = 3,
    Parse = 4
}

public class ProgressUI : MonoBehaviour
{
    [Header("Panels")]
    // [SerializeField] private GameObject progressbar;
    [SerializeField] private GameObject notification;
    [SerializeField] private GameObject loading;
    
    [Header("UI Components")]
    // [SerializeField] private Slider slider;
    // [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text noticeText;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private GameObject loadingCircle;
    [SerializeField] private GameObject loadingCheck;

    private const int MaxStage = 4;

    private void Awake()
    {
        // if (!slider && progressbar) slider = progressbar.GetComponent<Slider>();

        // if (slider)
        // {
        //     slider.minValue = 0;
        //     slider.maxValue = MaxStage;
        //     slider.value = 0;
        // }
        SetProgressing(false);
        SetLoading(false);
    }

    public void SetProgressing(bool progressing)
    {
        // if (progressbar) progressbar.SetActive(progressing);
        if (notification) notification.SetActive(!progressing);
        if (loading) loading.SetActive(progressing);
    }

    public void SetLoading(bool loading)
    {
        if (loadingCircle)
        {
            loadingCircle.SetActive(loading);

            var circle = loadingCircle.GetComponent<VRUIP.LoadingCircle>();
            if (circle != null)
            {
                circle.Loading = loading;
            }
        }
        if (loadingCheck) loadingCheck.SetActive(!loading);
    }

    public void SetStage(ProgressStage stage)
    {
        // if (slider) slider.value = (int) stage -1;

        // if (progressText) progressText.text = $"{(int)stage}/{MaxStage}";

        if (loadingText)
        {
            switch (stage)
            {
                case ProgressStage.Capture: loadingText.text = "Capturing..."; break;
                case ProgressStage.Upload: loadingText.text = "Image Uploading..."; break;
                case ProgressStage.Receive: loadingText.text = "Waiting for Server Response..."; break;
                case ProgressStage.Parse: loadingText.text = "Response Parsing..."; break;
            }
        }
    }

    public void ShowNotice(string msg)
    {
        if (noticeText) noticeText.text = msg;
    }

    public void ShowError(string errMsg)
    {
        SetProgressing(false);
        if (noticeText) noticeText.text = errMsg;
    }

    public void ResetAll()
    {
        // if (slider) slider.value = 0;
        // if (progressText) progressText.text = "";
        if (noticeText) noticeText.text = "Press the Start Button\nREHABGEN will capture your environment";
        SetProgressing(false);
        SetLoading(false);
    }
}
