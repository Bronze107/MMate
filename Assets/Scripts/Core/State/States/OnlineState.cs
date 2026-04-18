using UnityEngine;

namespace MMate.Core.State
{
    /// <summary>
    /// Online state - avatar is active and ready for input.
    /// </summary>
    public class OnlineState : BaseAvatarState
    {
        public override AvatarStateType StateType => AvatarStateType.Online;

        public override void Enter()
        {
            Debug.Log("[OnlineState] Avatar is now online. Playing idle animation.");
            PlayIdleAnimation();
        }

        public override void Exit()
        {
            Debug.Log("[OnlineState] Avatar going offline.");
        }

        public override void Update(float deltaTime)
        {
            // Ready to receive user input
            ProcessInput();
        }

        private void PlayIdleAnimation()
        {
            // TODO: Trigger idle animation on avatar
            // Example: animator.SetTrigger("Idle");
        }

        private void ProcessInput()
        {
            // TODO: Handle user input for avatar movement/actions
        }
    }
}
