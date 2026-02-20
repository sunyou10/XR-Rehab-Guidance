using UnityEngine;
using UnityEngine.UI;

public class LoadingCircle : MonoBehaviour
{

    [Header("Components")]
    [SerializeField] private Image loadingImage1;
    [SerializeField] private Image loadingImage2;

    private int speed = 1;

    private void OnEnable()
    {
        loadingImage1.fillAmount = 0.25f;
        loadingImage2.fillAmount = 0.25f;
    }

    private void Update()
    {
        loadingImage1.transform.Rotate(Vector3.forward, -1f * speed);
        loadingImage2.transform.Rotate(Vector3.forward, -1f * speed);
    }
}
