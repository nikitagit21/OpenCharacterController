using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    public interface ICrouchIntent : IIntent
    {
        bool wantsToStartCrouching { get; }
        bool wantsToStopCrouching { get; }
        Vector2 moveDirection { get; }
    }

    [Serializable]
    public class Crouch : PlayerAbility
    {
        private IPlayerController _controller;
        private ICrouchIntent _intent;

        [SerializeField]
        private float _colliderHeight = 0.9f;

        [SerializeField]
        private float _eyeHeight = 0.8f;

        [SerializeField]
        private PlayerSpeed _speed = new PlayerSpeed(0.8f, 1f, 1f);

        public override bool isBlocking => true;

        public override bool canActivate => _intent.wantsToStartCrouching || !_controller.canStandUp;

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
            _intent = controller.GetIntent<ICrouchIntent>();
        }

        public override void OnActivate()
        {
            _controller.ChangeHeight(_colliderHeight, _eyeHeight);
        }

        public override void OnDeactivate()
        {
            _controller.ResetHeight();
        }

        public override void FixedUpdate()
        {
            _controller.ApplyUserInputMovement(_intent.moveDirection, _speed);

            if (_intent.wantsToStopCrouching && _controller.canStandUp)
            {
                Deactivate();
            }
        }
    }
}
