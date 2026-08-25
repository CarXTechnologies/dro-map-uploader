using System;
using Modio.Unity.UI.Panels;
using Modio.Users;
using UnityEngine;
using UnityEngine.UI;

namespace Modio.Unity.UI.Components.UserProperties
{
    [Serializable]
    public class UserPropertyFollowToggle : IUserProperty
    {
        [SerializeField] Button _followButton;
        [SerializeField] Button _unFollowButton;
        
        UserProfile _user;
        
        public void OnUserUpdate(UserProfile user)
        {
            _user = user;

            if (_followButton is not null)
            {
                _followButton.onClick.RemoveListener(ToggleFollowButtonClicked);
                _followButton.onClick.AddListener(ToggleFollowButtonClicked);
                
                _followButton.gameObject.SetActive(!_user.IsFollowedByLoggedInUser);
            }

            if (_unFollowButton is not null)
            {
                _unFollowButton.onClick.RemoveListener(ToggleFollowButtonClicked);
                _unFollowButton.onClick.AddListener(ToggleFollowButtonClicked);
                
                _unFollowButton.gameObject.SetActive(_user.IsFollowedByLoggedInUser);
            }
        }

        void ToggleFollowButtonClicked()
        {
            UpdateFollowed(!_user.IsFollowedByLoggedInUser);
        }

        void UpdateFollowed(bool shouldBeFollowed)
        {
            var task = shouldBeFollowed ? _user.Follow() : _user.Unfollow();
            ModioPanelManager.GetPanelOfType<ModioErrorPanelGeneric>()?.MonitorTaskThenOpenPanelIfError(task);
        }
    }
}
