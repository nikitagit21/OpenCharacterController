using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    public interface IJumpIntent : IIntent
    {
        bool wantsToJump { get; }
    }

    [Serializable]
    public class Jump : PlayerAbility
    {
        private IPlayerController _controller;
        private IJumpIntent _intent;

        [SerializeField]
        private float _height = 1.5f;

        public override bool canActivate => _controller.grounded && _intent.wantsToJump;

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
            _intent = _controller.GetIntent<IJumpIntent>();
        }

        public override void OnActivate()
        {
            _controller.verticalVelocity = Mathf.Sqrt(2f * _height * -Physics.gravity.y);
            _controller.grounded = false;

            // Jump is a fire-and-forget ability; it doesn't need to stay activated
            Deactivate();
        }
    }
}
