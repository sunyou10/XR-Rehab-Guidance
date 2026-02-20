using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public void NextSceneWithBtn()
    {
        SceneManager.LoadScene(1);
    }
}
