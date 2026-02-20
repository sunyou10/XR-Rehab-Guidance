using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text accText;
    [SerializeField] private TMP_Text gradeText;

    private float animSeconds = 1f;

    public void Show(ShowHandPose.ScoreResult r)
    {
        if (panelRoot != null) panelRoot.SetActive(true);

        if (timeText != null) timeText.text = $"{r.timeSec:F0}s";
        if (accText != null) accText.text = $"{r.accuracy * 100f:F0}%";

        if (gradeText != null) gradeText.text = GradeFromScore(r.score);

        StopAllCoroutines();
        StartCoroutine(AnimateScore(r.score));
    }

    private IEnumerator AnimateScore(float score)
    {
        float t = 0f;
        float start = 0f;

        while (t < animSeconds)
        {
            t += Time.unscaledDeltaTime;
            float v = Mathf.Lerp(start, score, t / animSeconds);
            if (scoreText != null) scoreText.text = $"{v:F0}";
            yield return null;
        }
        if (scoreText != null) scoreText.text = $"{score:F0}";
    }

    private string GradeFromScore(float s)
    {
        if (s >= 90f) return "S";
        if (s >= 80f) return "A";
        if (s >= 70f) return "B";
        if (s >= 60f) return "C";
        return "D";
    }
}
