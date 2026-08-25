using Modio.Collections;
using System;
using Modio.Unity.UI.Components.Localization;
using Modio.Unity.UI.Panels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.ModProperties
{
    [Serializable]
    public class CollectionPropertyFollowedToggle : ICollectionProperty
    {
        [SerializeField] Button _followButton;
        [SerializeField] Toggle _followToggle;
        [SerializeField] Button _unfollowButton;
        
        [SerializeField] TMP_Text _text;
        [SerializeField] ModioUILocalizedText _localisedText;
        
        [SerializeField] bool _showPopupToSubscribe;
        
        ModCollection _collection;

        public void OnCollectionUpdate(ModCollection collection)
        {
            _collection = collection;
            
            if (_text != null) _text.text = _collection.IsFollowed ? "UNFOLLOW" : "FOLLOW";

            if (_localisedText != null)
                _localisedText.SetKey(
                    _collection.IsFollowed ? ModioUILocalizationKeys.Btn_Unfollow : ModioUILocalizationKeys.Btn_Follow
                );
            
            
            if (_followToggle != null)
            {
                _followToggle.onValueChanged.RemoveListener(FollowToggleValueChanged);

                _followToggle.isOn = _collection.IsFollowed;

                _followToggle.onValueChanged.AddListener(FollowToggleValueChanged);

                _followToggle.gameObject.SetActive(true);
            }

            if (_followButton != null)
            {
                _followButton.onClick.RemoveListener(FollowButtonClicked);
                _followButton.onClick.AddListener(FollowButtonClicked);

                _followButton.gameObject.SetActive(
                    (_unfollowButton == null || !_collection.IsFollowed)
                );
            }

            if (_unfollowButton != null)
            {
                _unfollowButton.onClick.RemoveListener(FollowButtonClicked);
                _unfollowButton.onClick.AddListener(FollowButtonClicked);

                _unfollowButton.gameObject.SetActive(_collection.IsFollowed);
            }
        }
        
        void FollowButtonClicked()
        {
            UpdateFollowed(!_collection.IsFollowed);
        }

        void FollowToggleValueChanged(bool arg0)
        {
            UpdateFollowed(_followToggle.isOn);
        }
        
        void UpdateFollowed(bool shouldBeFollowed)
        {
            if (shouldBeFollowed)
            {
                if (_showPopupToSubscribe) 
                    ModioPanelManager.GetPanelOfType<ModCollectionSubscribePanel>()?.OpenPanel(_collection);
                
                var task = _collection.Follow();

                if (_followToggle != null) _followToggle.SetIsOnWithoutNotify(_collection.IsFollowed);

                ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
            }
            else
            {
                ModioPanelManager.GetPanelOfType<ModCollectionUnsubscribePanel>()?.OpenPanel(_collection);
            }
        }
    }
}
