using System;
using Modio.Unity.UI.Panels;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Modio.Unity.UI.Input
{
    public class ModioUIActionListener : MonoBehaviour
    {
        [SerializeField] UnityEvent _onActionPressed;
        [SerializeField] ModioUIInput.ModioAction _action;
        
        ModioPanelBase _parentPanel;

        void Awake()
        {
            _parentPanel = GetComponentInParent<ModioPanelBase>();
        }

        void OnEnable()
        {
            _parentPanel.OnHasFocusChanged += OnFocusChanged;

            if (_parentPanel.HasFocus)
            {
                OnFocusChanged(true);
            }
        }

        void OnDisable()
        {
            _parentPanel.OnHasFocusChanged -= OnFocusChanged;
        }

        void OnFocusChanged(bool hasFocus)
        {
            ModioUIInput.RemoveHandler(_action, OnActionPressed);
            
            if (hasFocus) 
                ModioUIInput.AddHandler(_action, OnActionPressed);
        }

        void OnActionPressed()
        {
            _onActionPressed?.Invoke();
        }
    }
}
