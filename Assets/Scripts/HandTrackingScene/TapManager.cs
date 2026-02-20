using UnityEngine;
using UnityEngine.UI;

public class TapManager : MonoBehaviour
{
    public Toggle captureTab;
    public Toggle sensorTab;

    [Header("Tab Views")]
    public GameObject captureView;
    public GameObject sensorView;

    [Header("Related Buttons")]
    public GameObject captureRelated;
    public GameObject exerciseRelated;

    void Awake()
    {
        captureTab.onValueChanged.AddListener(OnCaptureTab);
        sensorTab.onValueChanged.AddListener(OnSensorTab);

        Apply();
    }

    void OnDestroy()
    {
        captureTab.onValueChanged.RemoveListener(OnCaptureTab);
        sensorTab.onValueChanged.RemoveListener(OnSensorTab);
    }

    void OnCaptureTab(bool on) {if (on) Apply(); }
    void OnSensorTab(bool on) {if (on) Apply(); }

    void Apply()
    {
        bool captureOn = captureTab.isOn;
        captureView.SetActive(captureOn);
        captureRelated.SetActive(captureOn);

        sensorView.SetActive(!captureOn);
        exerciseRelated.SetActive(!captureOn);
    }
}
