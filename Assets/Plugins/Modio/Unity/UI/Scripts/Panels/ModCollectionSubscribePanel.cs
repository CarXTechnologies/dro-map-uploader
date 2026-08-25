using System.Threading.Tasks;
using Modio.Collections;
using Modio.Extensions;
using Modio.Errors;
using Modio.Unity.UI.Components;
using UnityEngine;

namespace Modio.Unity.UI.Panels
{
    public class ModCollectionSubscribePanel : ModioPanelBase
    {
        ModioUICollection _modioUICollection;

        protected override void Awake()
        {
            base.Awake();
            _modioUICollection = GetComponent<ModioUICollection>();
        }

        public void OpenPanel(ModioUICollection collection)
        {
            OpenPanel(collection.Collection);
        }

        public void OpenPanel(ModCollection collection)
        {
            OpenPanel();

            _modioUICollection.SetCollection(collection);
        }

        public void ConfirmPressed()
        {
            SubscribeWithDependenciesAndHandleResult().ForgetTaskSafely();
        }

        async Task SubscribeWithDependenciesAndHandleResult()
        {
            Error error;
            
            if (!await ModInstallationManagement.IsThereAvailableSpaceFor(_modioUICollection.Collection))
            {
                error = new FilesystemError(FilesystemErrorCode.INSUFFICIENT_SPACE);
                
                ClosePanel();
                ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.OpenPanel(error);
                return;
            }
            
            Task<Error> subWithDependenciesTask = _modioUICollection.Collection.Subscribe();

            var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();

            if (waitingPanel != null)
                error = await waitingPanel.OpenAndWaitForAsync(subWithDependenciesTask);
            else
                error = await subWithDependenciesTask;

            ClosePanel();

            if (error) ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.OpenPanel(error);
        }
    }
}
