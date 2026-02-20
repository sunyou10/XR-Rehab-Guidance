using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using VRUIP;

public class Element : A_UIInteractions_no_color
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private bool hasOverlay;
    [SerializeField] private bool hasTextContent;
    [SerializeField] private Image image;
    [SerializeField] private Image overlay;
    [SerializeField] private TMP_Text textContent;

    protected bool initialized;
    
    // Colors
    private readonly Color clickColor = new Color(0, 0, 0, 200/255f);
    private readonly Color hoverColor = new Color(1, 1, 1, 30/255f);

    public Vector2 Size
    {
        get
        {
            var rect = rectTransform.rect;
            var scale = rectTransform.localScale;
            return new Vector2(rect.width * scale.x, rect.height * scale.y);
        }
    }

    protected void Start()
    {
        if (!initialized) Initialize();
    }

    public Element CreateElement(Vector3 position, Transform parent)
    {
        var clone = Instantiate(this, parent);
        clone.transform.localPosition = position;
        clone.Initialize();
        return clone;
    }

    public void CreateElementFromInfo(Collection.ElementInfo info, Vector3 position, Transform parent)
    {
        var element = CreateElement(position, parent);
        element.SetInfo(info);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Create a copy of this element in the editor.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="parent"></param>
    public Element CreateElementEditor(Vector3 position, Transform parent)
    {
        var clone = PrefabUtility.InstantiatePrefab(gameObject, parent) as GameObject;
        if (clone == null) throw new System.Exception("Could not instantiate element.");
        clone.transform.localPosition = position;
        return clone.GetComponent<Element>();
    }
    
    /// <summary>
    /// Create an element from an ElementInfo.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="position"></param>
    /// <param name="parent"></param>
    public void CreateElementFromInfoEditor(Collection.ElementInfo info, Vector3 position, Transform parent)
    {
        var element = CreateElementEditor(position, parent);
        element.SetInfo(info);
    }
#endif

    protected virtual void Initialize()
    {
        initialized = true;
        
        if (hasOverlay)
        {
            RegisterOnEnter(() => overlay.gameObject.SetActive(true));
            RegisterOnExit(() => overlay.gameObject.SetActive(false));
            RegisterOnDown(() => overlay.color = clickColor);
            RegisterOnUp(() => overlay.color = hoverColor);
        }
    }

    protected void SetInfo(Collection.ElementInfo info)
    {
        image.sprite = info.sprite;
        textContent.text = info.textContent;
    }
}