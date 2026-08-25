using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Modio.Collections;
using Modio.Extensions;
using Modio.Mods;
using Modio.Unity.UI.Components.Localization;
using UnityEngine;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertySubscribedToMods : ICollectionProperty
    {
        [SerializeField]
        GameObject _enableIfSubscribedAll;
        [SerializeField]
        GameObject _enableIfFollowedButNotAllSubscribed;

        [SerializeField]
        ModioUILocalizedText _subscriptionCountLoc;

        ModCollection _modCollection;

        public void OnCollectionUpdate(ModCollection collection)
        {
            _modCollection = collection;

            if (!_modCollection.IsFollowed)
            {
                if (_enableIfSubscribedAll != null)
                    _enableIfSubscribedAll.SetActive(false);
                if (_enableIfFollowedButNotAllSubscribed != null)
                    _enableIfFollowedButNotAllSubscribed.SetActive(false);
                return;
            }
            
            if (_enableIfSubscribedAll != null)
                _enableIfSubscribedAll.SetActive(true);
            if (_enableIfFollowedButNotAllSubscribed != null)
                _enableIfFollowedButNotAllSubscribed.SetActive(false);

            DoWork(_modCollection).ForgetTaskSafely();
        }

        async Task DoWork(ModCollection collection)
        {
            (Error error, IReadOnlyList<Mod> mods) = await collection.GetMods();
            
            if(error) return;
            
            //Make sure we haven't changed collections since
            if(collection != _modCollection) return;

            int subscribedCount = mods.Count(mod => mod.IsSubscribed);

            bool allSubscribed = subscribedCount == mods.Count;
            
            if (_enableIfSubscribedAll != null)
                _enableIfSubscribedAll.SetActive(allSubscribed);
            if (_enableIfFollowedButNotAllSubscribed != null)
                _enableIfFollowedButNotAllSubscribed.SetActive(!allSubscribed);
            
            if(!allSubscribed && _subscriptionCountLoc != null)
                _subscriptionCountLoc.SetFormatArgs(subscribedCount, mods.Count);
        }
    }
}
