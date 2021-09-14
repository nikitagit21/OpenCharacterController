using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    public interface IWalkIntent : IIntent
    {
        Vector2 moveDirection { get; }
    }

    [Serializable]
    public class Walk : PlayerAbility
    {
        private IPlayerController _controller;
        private IWalkIntent _intent;

        [SerializeField]
        private PlayerSpeed _speed = new PlayerSpeed(2f, 1f, 0.95f);

        public override bool isBlocking => true;

        public override bool canActivate => _controller.canStandUp;

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
            _intent = _controller.GetIntent<IWalkIntent>();
        }

        public override void OnActivate()
        {
            _controller.ResetHeight();
        }

        public override void FixedUpdate()
        {
            _controller.ApplyUserInputMovement(_intent.moveDirection, _speed);
        }
    }
}