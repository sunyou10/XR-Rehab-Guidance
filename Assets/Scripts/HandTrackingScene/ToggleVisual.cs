using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Toggle))]
public class TabVisual : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] GameObject indicator;

    Toggle toggle;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnToggleChanged);
        Refresh();
    }

    void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    void OnToggleChanged(bool _)
    {
        Refresh();
    }

    void Refresh()
    {
        bool on = toggle.isOn;

        if (indicator) indicator.SetActive(on);
    }
}
