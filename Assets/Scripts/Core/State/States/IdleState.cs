using UnityEngine;

namespace MMate.Core.State
{
    /// <summary>
    /// Idle state - avatar is idle with random idle actions.
    /// </summary>
    public class IdleState : BaseAvatarState
    {
        private float _idleTimer;
        private float _nextIdleActionTime;
        private bool _isIdleActionPlaying;

        private const float MinIdleActionInterval = 5f;
        private const float MaxIdleActionInterval = 15f;

        public override AvatarStateType StateType => AvatarStateType.Idle;

        public override void Enter()
        {
            Debug.Log("[IdleState] Avatar entering idle mode.");
            _idleTimer = 0f;
            _isIdleActionPlaying = false;
            ScheduleNextIdleAction();
        }

        public override void Exit()
        {
            Debug.Log("[IdleState] Avatar exiting idle mode.");
            _idleTimer = 0f;
        }

        public override void Update(float deltaTime)
        {
            _idleTimer += deltaTime;

            if (!_isIdleActionPlaying && _idleTimer >= _nextIdleActionTime)
            {
                TriggerIdleAction();
            }
        }

        private void ScheduleNextIdleAction()
        {
            _nextIdleActionTime = _idleTimer + Random.Range(MinIdleActionInterval, MaxIdleActionInterval);
        }

        private void TriggerIdleAction()
        {
            _isIdleActionPlaying = true;
            PerformIdleAction();
            ScheduleNextIdleAction();

            // Reset flag after action completes (simplified)
            _isIdleActionPlaying = false;
        }

        /// <summary>
        /// Perform a random idle action.
        /// </summary>
        protected virtual void PerformIdleAction()
        {
            // TODO: Implement idle actions (look around, stretch, etc.)
            // Example: animator.SetTrigger("IdleAction_" + Random.Range(1, 4));
            Debug.Log("[IdleState] Triggering idle action.");
        }
    }
}
