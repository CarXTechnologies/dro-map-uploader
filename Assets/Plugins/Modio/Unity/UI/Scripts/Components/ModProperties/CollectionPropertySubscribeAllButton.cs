using System;
using Modio.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertySubscribeAllButton : ICollectionProperty
    {
        [SerializeField] Button _subscribeAllButton;
        
        ModCollection _collection;

        public void OnCollectionUpdate(ModCollection collection)
        {
            _collection = collection;

            if (_subscribeAllButton != null)
            {
                _subscribeAllButton.onClick.RemoveListener(SubscribeAllButtonClicked);
                _subscribeAllButton.onClick.AddListener(SubscribeAllButtonClicked);

                _subscribeAllButton.gameObject.SetActive(collection.IsFollowed);
            }
        }

        void SubscribeAllButtonClicked()
        {
            _collection.Subscribe();
        }
    }
}
