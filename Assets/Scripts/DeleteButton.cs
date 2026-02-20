using UnityEngine;
using UnityEngine.UI;

public class DeleteButton : MonoBehaviour
{
    [SerializeField] private Button deleteButton;
    
    public void Delete()
    {
        gameObject.SetActive(false);
    }
}
