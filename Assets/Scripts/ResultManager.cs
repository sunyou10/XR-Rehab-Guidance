using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;


[Serializable]
public class RehabgenResponse
{
    public RehabInput input;
    public string Exercise;
    public string status;
    public string run_id;
}

[Serializable]
public class RehabInput
{
    public string image;
    public string symptom;
    public string long_term_goal;
    public string[] short_term_goals;
    public string[] quantitative_measures;
    public string detected_objects;
}

public class ResultManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private ProgressUI ui;
    [SerializeField] private ResultUI resultUI;
    [SerializeField] private LoadingCheck loadingCheck;
    [SerializeField] private TMP_Text buttonText;

    [Header("Server")]
    public string serverUrl = "https://unscabrously-unhermetic-amari.ngrok-free.dev/result";

    public IEnumerator getResult(string run_id)
    {
        string url = $"{serverUrl}/{run_id}";

        while (true)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                www.timeout = 20;
                yield return www.SendWebRequest();

                string body = www.downloadHandler != null ? www.downloadHandler.text : "";
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[REHABGEN] Error: {www.responseCode}, err={www.error}, body={body}");
                    ui.ShowError($"REHABGEN Server Error ({www.responseCode})\n{body}.");
                    yield break;
                }

                ui.SetStage(ProgressStage.Receive);  // 일단 응답 받음 (근데 돌아가는 중일 수도)
                
                string json = www.downloadHandler.text;
                RehabgenResponse response = JsonUtility.FromJson<RehabgenResponse>(json);
                Debug.Log("[REHABGEN] Process status: " + response.status);

                if (response.status == "running"){ yield return new WaitForSeconds(1f); continue; }
                else if (response.status == "done")
                {
                    yield return StartCoroutine(ParseResult(response));
                    yield break;
                }
                else if (response.status == "failed")
                {
                    ui.ShowError("REHABGEN failed analyze.");
                    yield break;
                }
                else
                {
                    Debug.LogWarning("[REHABGEN] IDK what is it -> response: " + json);
                    ui.ShowError("REHABGEN gave me weird answer.");
                    yield break;
                }
            }
        }
    }

    private IEnumerator ParseResult(RehabgenResponse response)
    {
        ui.SetStage(ProgressStage.Parse);

        string exercise = response.Exercise;
        if (exercise == null)
        {
            ui.ShowError("No Exercise Recommendation ;;");
            yield break;
        }
        ui.SetLoading(false);

        yield return new WaitUntil(() => loadingCheck.IsFinished);
        
        ui.SetProgressing(false);
        if (response.input == null) ui.ShowNotice("Input Missing.");
        else resultUI.ShowResult(response);
        if(buttonText) buttonText.text = "Pinch the Exercise\non the screen";
    }
}
