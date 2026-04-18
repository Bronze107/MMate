namespace MMate.Core.State
{
    /// <summary>
    /// Avatar state types.
    /// </summary>
    public enum AvatarStateType
    {
        Online,
        Offline,
        Idle
    }

    /// <summary>
    /// Interface for avatar states.
    /// </summary>
    public interface IAvatarState
    {
        AvatarStateType StateType { get; }
        void Enter();
        void Exit();
        void Update(float deltaTime);
    }

    /// <summary>
    /// Base class for avatar states.
    /// </summary>
    public abstract class BaseAvatarState : IAvatarState
    {
        public abstract AvatarStateType StateType { get; }

        public virtual void Enter()
        {
        }

        public virtual void Exit()
        {
        }

        public virtual void Update(float deltaTime)
        {
        }
    }
}
