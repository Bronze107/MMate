using System.Collections;
using UnityEngine;

namespace MMate.Avatar
{
    /// <summary>
    /// Avatar controller for managing character animations.
    /// </summary>
    public class AvatarController : MonoBehaviour
    {
        #region Animation Parameter Constants

        private static readonly int IsIdle = Animator.StringToHash("isIdle");
        private static readonly int IsTalking = Animator.StringToHash("isTalking");
        private static readonly int IdleActionIndex = Animator.StringToHash("idleActionIndex");

        #endregion

        #region Serialized Fields

        [SerializeField] private Animator animator;
        [SerializeField] private float idleActionInterval = 30f;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            StartCoroutine(IdleActionRoutine());
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Play idle animation state.
        /// </summary>
        public void PlayIdle()
        {
            animator.SetBool(IsIdle, true);
            animator.SetBool(IsTalking, false);
        }

        /// <summary>
        /// Play talking animation state.
        /// </summary>
        public void PlayTalk()
        {
            animator.SetBool(IsIdle, false);
            animator.SetBool(IsTalking, true);
        }

        /// <summary>
        /// Play a random idle action animation.
        /// </summary>
        /// <param name="index">Specific action index, or -1 for random.</param>
        public void PlayIdleAction(int index = -1)
        {
            int actionIndex = index < 0 ? Random.Range(0, 10) : index;
            animator.SetInteger(IdleActionIndex, actionIndex);
        }

        /// <summary>
        /// Set facial expression (reserved for future implementation).
        /// </summary>
        /// <param name="expressionName">Name of the expression to set.</param>
        public void SetExpression(string expressionName)
        {
            Debug.Log($"SetExpression: {expressionName} (not implemented)");
        }

        #endregion

        #region Private Methods

        private IEnumerator IdleActionRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(idleActionInterval);
                PlayIdleAction();
            }
        }

        #endregion
    }
}
