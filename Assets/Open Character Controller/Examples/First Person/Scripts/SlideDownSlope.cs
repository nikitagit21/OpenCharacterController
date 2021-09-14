using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    [Serializable]
    public class SlideDownSlope : PlayerAbility
    {
        private IPlayerController _controller;

        [SerializeField]
        [Tooltip("The player will begin sliding on any slope with a steepness greater than or equal to this value.")]
        private float _slidingAngle = 45f;

        // TODO: These fields are duplicated in SlideDownSlope and Slide;
        // probably worth moving some of them to some shared object.
        [SerializeField]
        private float _groundFriction = 0.8f;

        [SerializeField]
        private PhysicMaterialCombine _groundFrictionCombine = PhysicMaterialCombine.Multiply;

        [SerializeField]
        private float _playerMass = 10f;

        public override bool isBlocking => true;

        public override bool canActivate => Mathf.Abs(Vector3.Angle(_controller.groundNormal, Vector3.up)) >= _slidingAngle;

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
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

            if (!canActivate)
            {
                Deactivate();
            }
        }
    }
}