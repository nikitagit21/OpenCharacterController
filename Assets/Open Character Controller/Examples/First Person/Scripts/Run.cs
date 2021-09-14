using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    public interface IRunIntent : IIntent
    {
        bool wantsToStartRunning { get; }
        bool wantsToStopRunning { get; }
        Vector2 moveDirection { get; }
    }

    [Serializable]
    public class Run : PlayerAbility
    {
        private IPlayerController _controller;
        private IRunIntent _intent;

        [SerializeField]
        private PlayerSpeed _speed = new PlayerSpeed(6f, 0.9f, 0.6f);

        public override bool isBlocking => true;

        public override bool canActivate => _intent.wantsToStartRunning && _controller.canStandUp;

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
            _intent = _controller.GetIntent<IRunIntent>();
        }

        public override void OnActivate()
        {
            _controller.ResetHeight();
        }

        public override void FixedUpdate()
        {
            _controller.ApplyUserInputMovement(_intent.moveDirection, _speed);

            if (_intent.wantsToStopRunning)
            {
                Deactivate();
            }
        }
    }
}