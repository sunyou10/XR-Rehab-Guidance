using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResultUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject capturePanel;
    [SerializeField] private GameObject notification;
    [SerializeField] private GameObject resultPanel;

    [Header("UI Raw Image")]
    [SerializeField] private RawImage CapturedImage;

    [Header("UI Text Components")]
    [SerializeField] private TMP_Text symptom;
    [SerializeField] private TMP_Text detectedObject;
    [SerializeField] private TMP_Text longTermGoal;
    [SerializeField] private TMP_Text element1;
    [SerializeField] private TMP_Text element2;
    [SerializeField] private TMP_Text element3;
    [SerializeField] private TMP_Text exercise;

    public void ShowResult(RehabgenResponse response)
    {
        if (longTermGoal == null || detectedObject == null || symptom == null || element1 == null || element2 == null || element3 == null || exercise == null)
        {
            Debug.LogError("[ShowResult] UI reference missing (Inspector!)");
            return;
        }

        symptom.text = response.input.symptom ?? "No Symptom";
        longTermGoal.text = response.input.long_term_goal ?? "No Goal";
        exercise.text = response.Exercise ?? "No Exercise";

        var goals = response.input.short_term_goals;
        element1.text = (goals != null && goals.Length > 0) ? goals[0] : "-";
        element2.text = (goals != null && goals.Length > 1) ? goals[1] : "-";
        element3.text = (goals != null && goals.Length > 2) ? goals[2] : "-";

        string raw = response.input.detected_objects;
        string[] objects = raw.Split(',');

        detectedObject.text = $"- {objects[0]}\n";
        for (int i = 1; i < objects.Length; i++)
        {
            detectedObject.text += $"- {objects[i].Trim()}\n";
        }

        if (capturePanel) capturePanel.SetActive(false);
        if (notification) notification.SetActive(false);
        if (resultPanel) resultPanel.SetActive(true);
    }
}
