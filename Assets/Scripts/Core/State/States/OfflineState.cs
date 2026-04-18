using UnityEngine;

namespace MMate.Core.State
{
    /// <summary>
    /// Offline state - avatar is not available.
    /// </summary>
    public class OfflineState : BaseAvatarState
    {
        public override AvatarStateType StateType => AvatarStateType.Offline;

        public override void Enter()
        {
            Debug.Log("[OfflineState] Avatar is now offline.");
            ShowOfflineUI();
        }

        public override void Exit()
        {
            Debug.Log("[OfflineState] Avatar reconnecting...");
            HideOfflineUI();
        }

        private void ShowOfflineUI()
        {
            // TODO: Show offline UI notification
            // Example: UIManager.ShowOfflineNotification();
            Debug.Log("[OfflineState] Showing offline UI notification.");
        }

        private void HideOfflineUI()
        {
            // TODO: Hide offline UI notification
            // Example: UIManager.HideOfflineNotification();
        }
    }
}
