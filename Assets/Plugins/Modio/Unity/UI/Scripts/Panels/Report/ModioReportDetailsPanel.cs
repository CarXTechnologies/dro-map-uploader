using System;
using System.Threading.Tasks;
using Modio.Collections;
using Modio.Mods;
using Modio.Reports;
using Modio.Unity.UI.Components;
using Modio.Unity.UI.Components.Selectables;
using Modio.Unity.UI.Navigation;
using Modio.Users;
using TMPro;
using UnityEngine;

namespace Modio.Unity.UI.Panels.Report
{
    public class ModioReportDetailsPanel : ModioPanelBase
    {
        ReportType _reportType;

        [SerializeField] TMP_InputField _email, _description;

        [SerializeField] ModioUIButton _disableWhenInvalidToSubmit;

        ModioUIMod _modioUIMod;
        ModioUICollection _modioUICollection;
        Mod _lastMod;
        ModCollection _lastModCollection;

        protected override void Start()
        {
            base.Start();

            _description.onValueChanged.AddListener(OnDescriptionTextChanged);
            OnDescriptionTextChanged(_description.text);

            _modioUIMod = GetComponentInParent<ModioUIMod>();
            _modioUICollection = GetComponentInParent<ModioUICollection>();

            if (_modioUIMod != null)
            {
                _modioUIMod.onModUpdate.AddListener(OnModUpdated);
                OnModUpdated();
            }
            
            if (_modioUICollection != null)
            {
                _modioUICollection.onCollectionUpdate.AddListener(OnCollectionUpdated);
                OnCollectionUpdated();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_modioUIMod != null) _modioUIMod.onModUpdate.RemoveListener(OnModUpdated);
            if (_modioUICollection != null) _modioUICollection.onCollectionUpdate.RemoveListener(OnCollectionUpdated);
        }

        void OnModUpdated()
        {
            if (_lastMod == _modioUIMod?.Mod) return;
            _lastMod = _modioUIMod?.Mod;
            _description.text = "";
        }

        void OnCollectionUpdated()
        {
            if (_lastModCollection == _modioUICollection.Collection) return;
            _lastModCollection = _modioUICollection.Collection;
            _description.text = "";
        }

        void OnDescriptionTextChanged(string description)
        {
            _disableWhenInvalidToSubmit.interactable = !string.IsNullOrEmpty(description);
            var grid = _disableWhenInvalidToSubmit.GetComponentInParent<ModioGridNavigation>();
            if (grid != null) grid.NeedsNavigationCorrection();
        }

        public void OpenPanel(ReportType type)
        {
            _reportType = type;
            OpenPanel();
        }

        public void OnUserPressedBackButton()
        {
            ClosePanel();
            ModioPanelManager.GetPanelOfType<ModioReportTypePanel>().OpenPanel();
        }

        public void OnUserSubmittedReportDetails()
        {
            ClosePanel();
            if (User.Current == null) return;

            Task<Error> reportTask;

            if (_modioUIMod != null && _modioUIMod.Mod != null)
                reportTask = _modioUIMod.Mod.Report(
                    _reportType,
                    0,
                    _email.text,
                    _description.text
                );
            else
                reportTask = _modioUICollection.Collection.Report(
                    _reportType,
                    _email.text,
                    _description.text
                );

            ModioPanelManager.GetPanelOfType<ModioReportWaitingPanel>().OpenAndWaitFor(reportTask, ReportCompleted);
        }

        void ReportCompleted(Error error)
        {
            if (error)
                ModioPanelManager.GetPanelOfType<ModioReportErrorPanel>()?.OpenPanel(error);
            else
                ModioPanelManager.GetPanelOfType<ModioReportConfirmationPanel>()?.OpenPanel();
        }
    }
}
