using System;
using UnityEngine;

namespace OpenCharacterController.Examples
{
    public interface ILookIntent : IIntent
    {
        Vector2 lookAmount { get; }
    }

    [Serializable]
    public class Look : PlayerAbility
    {
        private IPlayerController _controller;
        private ILookIntent _intent;

        [SerializeField, Range(0f, 90f), Tooltip("The maximum angle the player can look up, specified in degrees.")]
        private float _maxAngleUp = 85f;

        [SerializeField, Range(0f, 90f), Tooltip("The maximum angle the player can look down, specified in degrees.")]
        private float _maxAngleDown = 85f;

        public override bool canActivate => true;

        public override void OnStart(IPlayerController controller)
        {
            _controller = controller;
            _intent = _controller.GetIntent<ILookIntent>();
        }

        public override void Update()
        {
            var yaw = _controller.turnTransform.localEulerAngles.y;
            yaw = Mathf.Repeat(yaw + _intent.lookAmount.x, 360f);
            _controller.turnTransform.localEulerAngles = new Vector3(0, yaw, 0);

            var pitch = _controller.lookUpDownTransform.localEulerAngles.x;

            // Transform wraps pitch in [0, 360) range so we have to deal with that here
            if (pitch > 180.0f)
            {
                pitch -= 360.0f;
            }

            pitch = Mathf.Clamp(pitch + _intent.lookAmount.y, -_maxAngleUp, _maxAngleDown);
            _controller.lookUpDownTransform.localEulerAngles = new Vector3(pitch, 0, 0);
        }
    }
}
