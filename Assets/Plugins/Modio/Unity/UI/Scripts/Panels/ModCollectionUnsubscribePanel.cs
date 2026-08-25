using System.Threading.Tasks;
using Modio.Collections;
using Modio.Extensions;
using Modio.Unity.UI.Components;

namespace Modio.Unity.UI.Panels
{
    public class ModCollectionUnsubscribePanel : ModioPanelBase
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

        public void UnsubscribeFromAllPressed()
        {
            UnsubscribeFromAllAndHandleResult().ForgetTaskSafely();
            UnfollowAndHandleResult().ForgetTaskSafely();
        }

        public void LeaveModsSubscribedPressed()
        {
            UnfollowAndHandleResult().ForgetTaskSafely();
        }

        async Task UnsubscribeFromAllAndHandleResult()
        {
            var task = _modioUICollection.Collection.Unsubscribe();
            
            var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
            Error error;

            if (waitingPanel != null)
                error = await waitingPanel.OpenAndWaitForAsync(task);
            else
                error = await task;

            ClosePanel();

            if (error) ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.OpenPanel(error);
        }
        
        async Task UnfollowAndHandleResult()
        {
            var task = _modioUICollection.Collection.Unfollow();
            
            var waitingPanel = ModioPanelManager.GetPanelOfType<ModioWaitingPanelGeneric>();
            Error error;

            if (waitingPanel != null)
                error = await waitingPanel.OpenAndWaitForAsync(task);
            else
                error = await task;

            ClosePanel();

            if (error) ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.OpenPanel(error);
        }
    }
}
