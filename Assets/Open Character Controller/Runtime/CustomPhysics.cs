using UnityEngine;

namespace OpenCharacterController
{
    public static class CustomPhysics
    {
        public static Vector3 ApplyGroundFrictionToVelocity(
            PhysicMaterial groundMaterial,
            Vector3 velocity,
            PhysicMaterialCombine playerFrictionCombine,
            float playerFriction,
            float playerMass
        )
        {
            /*
             * Because we control all of the velocity for our player we also
             * have to apply friction. Here we're doing a rough job of
             * applying friction based on physic materials.
             * https://docs.unity3d.com/Manual/class-PhysicMaterial.html
             */

            float frictionAmount;

            if (
                playerFrictionCombine == PhysicMaterialCombine.Maximum ||
                groundMaterial.frictionCombine == PhysicMaterialCombine.Maximum
            )
            {
                frictionAmount = Mathf.Max(groundMaterial.dynamicFriction, playerFriction);
            }
            else if (
                playerFrictionCombine == PhysicMaterialCombine.Multiply ||
                groundMaterial.frictionCombine == PhysicMaterialCombine.Multiply
            )
            {
                frictionAmount = playerFriction * groundMaterial.dynamicFriction;
            }
            else if (
                playerFrictionCombine == PhysicMaterialCombine.Minimum ||
                groundMaterial.frictionCombine == PhysicMaterialCombine.Minimum
            )
            {
                frictionAmount = Mathf.Min(groundMaterial.dynamicFriction, playerFriction);
            }
            else
            {
                frictionAmount = (playerFriction + groundMaterial.dynamicFriction) * 0.5f;
            }

            var friction = Vector3.ClampMagnitude(
                -velocity.normalized * (playerMass * frictionAmount * Time.deltaTime),
                velocity.magnitude
            );

            return velocity + friction;
        }
    }
}