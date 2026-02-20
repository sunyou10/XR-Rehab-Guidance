using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public abstract class A_UIInteractions_no_color : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    public bool interactable = true;

    private UnityEvent _pointerEnterEvent = new UnityEvent();
    private UnityEvent _pointerExitEvent = new UnityEvent();
    private UnityEvent _pointerDownEvent = new UnityEvent();
    private UnityEvent _pointerUpEvent = new UnityEvent();
    private UnityEvent _pointerOverEvent = new UnityEvent();
    private UnityEvent _pointerOffEvent = new UnityEvent();
    private UnityEvent _pointerClickEvent = new UnityEvent();

    private bool _mouseOver;
    private bool _mouseDown;

    private void Update()
    {
        if (_mouseOver)
        {
            _pointerOverEvent.Invoke();
        }
        else
        {
            _pointerOffEvent.Invoke();
        }
    }

    protected void RegisterOnEnter(UnityAction function)
    {
        if (function == null) return;
        _pointerEnterEvent.AddListener(function);
    }

    protected void RegisterOnExit(UnityAction function)
    {
        if (function == null) return;
        _pointerExitEvent.AddListener(function);
    }
    
    protected void RegisterOnDown(UnityAction function)
    {
        if (function == null) return;
        _pointerDownEvent.AddListener(function);
    }
    
    protected void RegisterOnUp(UnityAction function)
    {
        if (function == null) return;
        _pointerUpEvent.AddListener(function);
    }
    
    protected void RegisterOnOver(UnityAction function)
    {
        if (function == null) return;
        _pointerOverEvent.AddListener(function);
    }
    
    protected void RegisterOnOff(UnityAction function)
    {
        if (function == null) return;
        _pointerOffEvent.AddListener(function);
    }
    
    protected void RegisterOnClicked(UnityAction function)
    {
        if (function == null) return;
        _pointerClickEvent.AddListener(function);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (!interactable) return;
        _mouseOver = true;
        if (!_mouseDown) _pointerEnterEvent.Invoke();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (!interactable) return;
        _mouseOver = false;
        if (!_mouseDown) _pointerExitEvent.Invoke();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!interactable) return;
        _mouseDown = true;
        _pointerDownEvent.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!interactable) return;
        _mouseDown = false;
        _pointerUpEvent.Invoke();
        if (!_mouseOver) _pointerExitEvent.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!interactable) return;
        _pointerClickEvent.Invoke();
    }

    private void OnDisable()
    {
        _pointerExitEvent.Invoke();
    }
}
