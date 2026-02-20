using UnityEngine;
using UnityEngine.UI;

public class LoadingCheck : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image checkImage;

    public bool IsFinished { get; private set; }

    private int speed = 4;

    private void OnEnable()
    {
        checkImage.fillAmount = 0.0f;
        IsFinished = false;
    }

    private void Update()
    {
        if (checkImage.fillAmount < 1f)
        {
            checkImage.fillAmount += 0.002f * speed;
        }
        else
        {
            checkImage.fillAmount = 1f;
            IsFinished = true;
        }
    }
}
