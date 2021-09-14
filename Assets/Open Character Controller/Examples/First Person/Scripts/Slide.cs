using UnityEngine;
using System;

namespace OpenCharacterController.Examples
{
    public interface ISlideIntent : IIntent
    {
        bool wantsToSlide { get; }
    }

    [Serializable]
    public class Slide : PlayerAbility
    {
        private IPlayerController _controller;
        private ISlideIntent _intent;

        [SerializeField]
        private float _speedRequiredToSlide = 3.5f;

        [SerializeField]
        private float _colliderHeight = 0.9f;

        [SerializeField]
        private float _eyeHeight = 0.8f;

        [SerializeField]
        private float _groundFriction = 0.8f;

        [SerializeField]
        private PhysicMaterialCombine _groundFrictionCombine = PhysicMaterialCombine.Multiply;

        [SerializeField]
        private float _playerMass = 10f;

        [SerializeField]
        private float _speedThresholdToExit = 0.8f;

        public override bool isBlocking => true;

        public override bool canActivate => 
            _intent.wantsToSlide && 
            _controller.speed >= _speedRequiredToSlide;

        public float SpeedRequiredToSlide { get => _speedRequiredToSlide; set => _speedRequiredToSlide = value; }

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
            _intent = _controller.GetIntent<ISlideIntent>();
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
            if (_controller.grounded)
            {
                // Add gravity projected onto the ground plane for acceleration
                var gravity = Physics.gravity;
                var ground = _controller.groundNormal;
                _controller.controlVelocity += (gravity - ground * Vector3.Dot(gravity, ground)) * Time.deltaTime;
                _controller.controlVelocity = CustomPhysics.ApplyGroundFrictionToVelocity(
                    _controller.groundMaterial,
                    _controller.controlVelocity,
                    _groundFrictionCombine,
                    _groundFriction,
                    _playerMass
                );
            }
            else
            {
                _controller.ApplyAirDrag();
            }

            if (_controller.speed <= _speedThresholdToExit)
            {
                Deactivate();
            }
        }
    }
}