using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    [Serializable]
    public struct PlayerSpeed
    {
        [SerializeField]
        [Tooltip("Full speed (meters/second) the player moves forward.")]
        private float _forwardSpeed;

        [SerializeField]
        [Tooltip("Percentage of forward speed applied when moving sideways.")]
        [Range(0f, 1f)]
        private float _sidewaysModifier;

        [SerializeField]
        [Tooltip("Percentage of forward speed applied when moving backward.")]
        [Range(0f, 1f)]
        private float _backwardModifier;

        public PlayerSpeed(float forwardSpeed, float sidewaysModifier, float backwardModifier)
        {
            _forwardSpeed = forwardSpeed;
            _sidewaysModifier = sidewaysModifier;
            _backwardModifier = backwardModifier;
        }

        public float TargetSpeed(Vector2 moveInput)
        {
            if (moveInput.sqrMagnitude == 0)
            {
                return 0;
            }
            else
            {
                moveInput.Normalize();

                if (moveInput.y >= 0)
                {
                    moveInput.x *= _forwardSpeed * _sidewaysModifier;
                    moveInput.y *= _forwardSpeed;
                    return moveInput.magnitude;
                }
                else
                {
                    return _forwardSpeed * _backwardModifier;
                }
            }
        }
    }
}
