using System;
using Modio.Collections;
using Modio.Mods;
using Modio.Unity.UI.Components;

namespace Modio.Unity.UI.Panels.Report
{
    public class ModioReportPanel : ModioPanelBase
    {
        ModioUIMod _modioUIMod;
        ModioUICollection _modioUICollection;

        protected override void Awake()
        {
            base.Awake();
            _modioUIMod = GetComponent<ModioUIMod>();
            _modioUICollection = GetComponent<ModioUICollection>();
        }

        public void OpenReportFlow(Mod mod)
        {
            _modioUICollection.SetCollection(null);
            _modioUIMod.SetMod(mod);

            ModioPanelManager.GetPanelOfType<ModioReportTypePanel>()?.OpenPanel();
        }

        public void OpenReportFlow(ModCollection collection)
        {
            _modioUIMod.SetMod(null);
            _modioUICollection.SetCollection(collection);
            
            ModioPanelManager.GetPanelOfType<ModioReportTypePanel>()?.OpenPanel();
        }

        void LateUpdate()
        {
            if (HasFocus) ClosePanel();
        }
    }
}
