using Modio.Unity.UI.Components;
using Modio.Unity.UI.Components.Selectables;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    public class ModOptionsPopupPanel : ModioPanelBase
    {
        ModioUIMod _modioUIMod;
        ModioUIUser _modioUIUser;
        ModioUICollection _modioUICollection;

        RectTransform _rectToPosition;
        RectTransform _rectToPositionWithin;
        ModioPopupPositioning _popupPositioning;
        ModioUIButton _buttonToHighlight;

        protected override void Awake()
        {
            base.Awake();
            _modioUIMod = GetComponent<ModioUIMod>();
            _modioUIUser = GetComponent<ModioUIUser>();
            _modioUICollection =  GetComponent<ModioUICollection>();
        }

        public void OpenPanel(ModioUIMod modUI)
        {
            OpenPanel();

            if (_popupPositioning == null) _popupPositioning = GetComponentInChildren<ModioPopupPositioning>();

            _modioUIMod.SetMod(modUI.Mod);
            _modioUIUser?.SetUser(modUI.Mod.Creator);

            var buttonToHighlight = modUI.GetComponent<ModioUIButton>();
            _buttonToHighlight = buttonToHighlight;

            _popupPositioning.PositionNextTo((RectTransform)modUI.transform);
        }
        public void OpenPanelWithCollection(ModioUICollection collectionUI)
        {
            OpenPanel();

            if (_popupPositioning == null) _popupPositioning = GetComponentInChildren<ModioPopupPositioning>();

            _modioUICollection.SetCollection(collectionUI.Collection);

            var buttonToHighlight = collectionUI.GetComponent<ModioUIButton>();
            _buttonToHighlight = buttonToHighlight;

            _popupPositioning.PositionNextTo((RectTransform)collectionUI.transform);
        }

        public override void OnLostFocus()
        {
            if (_buttonToHighlight != null)
                _buttonToHighlight.DoVisualOnlyStateTransition(IModioUISelectable.SelectionState.Normal, false);

            base.OnLostFocus();
        }

        public override void FocusedPanelLateUpdate()
        {
            base.FocusedPanelLateUpdate();

            if (_buttonToHighlight != null)
                _buttonToHighlight.DoVisualOnlyStateTransition(IModioUISelectable.SelectionState.Highlighted, false);
        }
    }
}
