using System;
using UnityEngine;

namespace MMate.Core.State
{
    /// <summary>
    /// Avatar state machine controller.
    /// </summary>
    public class AvatarStateMachine : MonoBehaviour
    {
        private static AvatarStateMachine _instance;
        public static AvatarStateMachine Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AvatarStateMachine>();
                }
                return _instance;
            }
        }

        private IAvatarState _currentState;

        /// <summary>
        /// Current state instance.
        /// </summary>
        public IAvatarState CurrentState => _currentState;

        /// <summary>
        /// Current state type.
        /// </summary>
        public AvatarStateType CurrentStateType => _currentState?.StateType ?? AvatarStateType.Offline;

        /// <summary>
        /// Event triggered when state changes.
        /// </summary>
        public event Action<IAvatarState, IAvatarState> OnStateChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        /// <summary>
        /// Change to a new state.
        /// </summary>
        /// <param name="newState">The new state to transition to.</param>
        public void ChangeState(IAvatarState newState)
        {
            if (newState == null)
            {
                Debug.LogWarning("[AvatarStateMachine] Cannot change to null state.");
                return;
            }

            var previousState = _currentState;

            previousState?.Exit();

            _currentState = newState;
            _currentState.Enter();

            OnStateChanged?.Invoke(previousState, _currentState);

            Debug.Log($"[AvatarStateMachine] State changed: {previousState?.StateType.ToString() ?? "null"} -> {_currentState.StateType}");
        }

        private void Update()
        {
            _currentState?.Update(Time.deltaTime);
        }
    }
}
