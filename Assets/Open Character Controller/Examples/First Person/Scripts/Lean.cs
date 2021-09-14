using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    public interface ILeanIntent : IIntent
    {
        float leanAmount { get; }
    }

    [Serializable]
    public class Lean : PlayerAbility
    {
        private IPlayerController _controller;
        private ILeanIntent _intent;

        [SerializeField]
        private float _leanDistanceX = 0.65f;

        [SerializeField]
        private float _leanDistanceY = -.05f;

        [SerializeField]
        private float _leanAngle = 10f;

        [SerializeField]
        private float _leanAnimationSpeed = 10f;

        public override bool canActivate => true;
        public override bool updatesWhenNotActive => true;

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
            _intent = _controller.GetIntent<ILeanIntent>();
        }

        public override void FixedUpdate()
        {
            // Because we update when not active, we need to be careful
            // when we apply input.
            var amount = isActive ? _intent.leanAmount : 0;
            var leanTransform = _controller.leanTransform;

            var eyeLocalRot = leanTransform.localEulerAngles;
            var desiredEyeRotThisFrame = Mathf.LerpAngle(
                eyeLocalRot.z,
                -amount * _leanAngle,
                _leanAnimationSpeed * Time.deltaTime
            );

            var targetEyeLocalPos = new Vector3(
                amount * _leanDistanceX,
                Mathf.Abs(amount) * _leanDistanceY,
                0
            );
            var desiredEyePosThisFrame = Vector3.Lerp(
                leanTransform.localPosition,
                targetEyeLocalPos,
                _leanAnimationSpeed * Time.deltaTime
            );

            if (amount != 0)
            {
                var ray = new Ray(
                    leanTransform.parent.position,
                    _controller.TransformDirection(targetEyeLocalPos.normalized)
                );

                var didHit = Physics.SphereCast(
                    ray,
                    _controller.cameraCollisionRadius,
                    out var hit,
                    targetEyeLocalPos.magnitude
                );

                if (didHit && desiredEyePosThisFrame.sqrMagnitude > (hit.distance * hit.distance))
                {
                    desiredEyePosThisFrame = leanTransform.parent.InverseTransformPoint(
                        ray.origin + ray.direction * hit.distance
                    );

                    // Scale rotation to be the same percentage as our distance
                    desiredEyeRotThisFrame = Mathf.LerpAngle(
                        eyeLocalRot.z,
                        -amount * _leanAngle * (hit.distance / targetEyeLocalPos.magnitude),
                        _leanAnimationSpeed * Time.deltaTime
                    );
                }
            }
            else
            {
                desiredEyePosThisFrame = Vector3.Lerp(
                    leanTransform.localPosition,
                    Vector3.zero,
                    _leanAnimationSpeed * Time.deltaTime
                );
            }

            leanTransform.localPosition = desiredEyePosThisFrame;

            eyeLocalRot.z = desiredEyeRotThisFrame;
            leanTransform.localEulerAngles = eyeLocalRot;
        }
    }
}