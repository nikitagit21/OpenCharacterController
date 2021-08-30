using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OpenCharacterController
{
    public sealed class CharacterBody : MonoBehaviour
    {
        private readonly Collider[] _overlapColliders = new Collider[16];

        private Rigidbody _body;
        private CapsuleCollider _collider;
        private RaycastHit _lastGroundHit;

        private float _capsuleHeight;
        private Vector3 _capsuleCenter;

        [SerializeField, Tooltip("The total height of the character. The capsule height is this value minus the step height.")]
        private float _height = 1.7f;

        [SerializeField, Tooltip("The collision radius of the character. If this value is less than half the computed capsule height, this value is replaced by half the computed capsule height.")]
        private float _radius = 0.35f;

        [SerializeField, Tooltip("The amount the character can step up or down while retaining a grounded state.")]
        private float _stepHeight = 0.35f;

        [SerializeField, Tooltip("An extra value used when sweeping the capsule through the world to improve collision detection.")]
        private float _skinThickness = 0.05f;

        [SerializeField, Tooltip("Mask of all layers to use when raycasting for the ground.")]
        private LayerMask _groundMask = 1; // "Default" layer by default

        [SerializeField, Tooltip("When turned on, the feet are treated as a cylinder. When turned off, the feet are treated as a capsule.")]
        private bool _cylinderFeet = true;

        public Vector3 position => _body.position;

        public Bounds bounds
        {
            get
            {
                // In order to use this in the editor when hitting F to frame
                // the body, we can't utilize the Rigidbody or CapsuleCollider
                // as those are created only when the game starts.
                var diameter = Mathf.Min(_radius, _height - _stepHeight / 2f) * 2f;
                return new Bounds(
                    transform.position + new Vector3(0, _height / 2f, 0),
                    new Vector3(diameter, _height, diameter)
                );
            }
        }

        public float height
        {
            get => _height;
            set
            {
                value = Mathf.Max(value, 0f);
                if (_height != value)
                {
                    var growing = value > _height;
                    _height = value;
                    _capsuleHeight = _height - _stepHeight;
                    _capsuleCenter = new Vector3(0, _capsuleHeight / 2f + _stepHeight, 0);
                    ResizeCollider(growing);
                }
            }
        }

        public float radius
        {
            get => _radius;
            set
            {
                value = Mathf.Clamp(value, 0f, _capsuleHeight / 2f);
                if (_radius != value)
                {
                    var growing = value > _radius;
                    _radius = value;
                    ResizeCollider(growing);
                }
            }
        }

        public float stepHeight
        {
            get => _stepHeight;
            set
            {
                value = Mathf.Clamp(value, 0, _height);
                if (_stepHeight != value)
                {
                    // Smaller step height means larger capsule
                    var growing = value < _stepHeight;
                    _stepHeight = value;
                    _capsuleHeight = _height - _stepHeight;
                    _capsuleCenter = new Vector3(0, _capsuleHeight / 2f + _stepHeight, 0);
                    ResizeCollider(growing);
                }
            }
        }

        public bool cylinderFeet
        {
            get => _cylinderFeet;
            set => _cylinderFeet = value;
        }
        
        private void Start()
        {
            CreateRigidbody();
            CreateCollider();
        }

        private void CreateRigidbody()
        {
            _body = gameObject.AddComponent<Rigidbody>();
#if UNITY_EDITOR
            _body.hideFlags = HideFlags.HideInInspector;
#endif

            // Must be dynamic for collision checks to work
            _body.isKinematic = false;

            // Controller should handle gravity
            _body.useGravity = false;

            // Don't fall down, please
            _body.freezeRotation = true;

            // We're doing all collision checks ourselves so we don't want the
            // physics engine doing any collision detection/response.
            _body.detectCollisions = false;

            // Interpolate the body for smoother motion
            _body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        private void CreateCollider()
        {
            _collider = gameObject.AddComponent<CapsuleCollider>();
#if UNITY_EDITOR
            _collider.hideFlags = HideFlags.HideInInspector;
#endif

            ResizeCollider();
        }

        public Vector3 MoveWithVelocity(Vector3 velocity)
        {
            // NOTE: Capping iterations here to avoid any chance of an infinite
            // loop. I don't know what situations would cause us to make more
            // than 10 moves before zeroing out our distance but whatever.
            const int MaxIterations = 10;

            var deltaTime = Time.deltaTime;
            var originalPosition = position;
            var newPosition = originalPosition;
            var movement = velocity * deltaTime;

            for (
                int iteration = 0;
                iteration < MaxIterations && !Mathf.Approximately(movement.sqrMagnitude, 0);
                iteration++
            )
            {
                Sweep(ref newPosition, ref movement);
            }

            _body.position = newPosition;

            return (newPosition - originalPosition) / deltaTime;
        }

        private void Sweep(ref Vector3 position, ref Vector3 movement)
        {
            var moveDirection = movement.normalized;

            // Shift the body back just slightly before sweeping forward. This
            // prevents us from slipping into geometry when our collision
            // geometry is exactly planar with an object.
            var skinMovement = moveDirection * _skinThickness;
            position -= skinMovement;
            movement += skinMovement;

            // Teleport the body to our exact intermediate location so the sweep
            // test is correct for this iteration.
            _body.position = position;
            var didCollide = _body.SweepTest(
                moveDirection,
                out var hit,
                movement.magnitude
            );

            if (didCollide)
            {
                // TODO: OnCollision event

                var allowedMovement = moveDirection * hit.distance;
                position += allowedMovement;
                movement -= allowedMovement;

                // Remove all movement opposite the normal of the surface we
                // collided with. This allows us to continue iterating to
                // "slide" players along a wall without constantly going back
                // into the wall.
                movement += hit.normal * Vector3.Dot(movement, -hit.normal);
            }
            else
            {
                position += movement;
                movement = Vector3.zero;
            }
        }

        public void Translate(Vector3 movement)
        {
            _body.position += movement;
        }

        public bool CheckForGround(
            bool stickToGround,
            out RaycastHit hit,
            out float verticalMovementApplied
         )
        {
            const float PaddingForFloatingPointErrors = 0.001f;

            var sphereCastHeight = _stepHeight + _radius;
            var maximumDistance = sphereCastHeight + PaddingForFloatingPointErrors;

            if (stickToGround)
            {
                maximumDistance += _stepHeight;
            }

            maximumDistance -= _radius;

            var origin = position + Vector3.up * sphereCastHeight;
            var hitGround = Physics.SphereCast(
                origin,
                _radius,
                Vector3.down,
                out hit,
                maximumDistance,
                _groundMask,
                QueryTriggerInteraction.Ignore
            );

            if (hitGround)
            {
                if (_cylinderFeet)
                {
                    // We're using a sphere but really want it to act like a
                    // cylinder. This bit of math tries to add to the distance to
                    // treat the curve of the sphere as if it was a cylinder.
                    var cylinderCorrection = hit.point.y - (origin.y - hit.distance - _radius);
                    hit.distance -= cylinderCorrection;
                }

                verticalMovementApplied = sphereCastHeight - hit.distance - _radius;

                // Raycasts are interesting here. We want to provide a
                // RaycastHit to the caller so they have the normal and other
                // information to work with. However because we do a SphereCast
                // above we might be hitting an edge of a platform. So what we
                // do here is do a single point raycast to gauge if we're over a
                // ledge or not.
                if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out hit,
                    maximumDistance,
                    _groundMask,
                    QueryTriggerInteraction.Ignore
                ))
                {
                    _lastGroundHit = hit;
                }
                else
                {
                    hit = _lastGroundHit;
                }

                _body.position += Vector3.up * verticalMovementApplied;
            }
            else
            {
                hit = default;
                verticalMovementApplied = default;
            }

            return hitGround;
        }

        private void ResizeCollider(bool growing = false)
        {
            _collider.height = _capsuleHeight;
            _collider.radius = _radius;
            _collider.center = _capsuleCenter;

            if (growing)
            {
                ResolveOverlaps();
            }
        }

        private void ResolveOverlaps()
        {
            // Iterative collision resolution to try and make the capsule not
            // colliding. If we fail, we simply fail and the capsule is left
            // colliding with the world.

            const int MaxIterations = 5;

            var position = _body.position;

            for (int iteration = 0; iteration < MaxIterations; iteration++)
            {
                int numColliders = FindOverlappingColliders(position);
                if (numColliders <= 0)
                {
                    break;
                }

                if (FindShortestPenetration(numColliders, out Vector3 translation))
                {
                    position += translation;
                }
            }

            _body.position = position;
        }

        private bool FindShortestPenetration(
            int numColliders,
            out Vector3 translation
        )
        {
            var shortestDistance = float.MaxValue;
            var foundShortest = false;
            translation = default;

            for (int index = 0; index < numColliders; index++)
            {
                var otherCollider = _overlapColliders[index];

                Vector3 otherPosition;
                Quaternion otherRotation;
                if (otherCollider.attachedRigidbody)
                {
                    otherPosition = otherCollider.attachedRigidbody.position;
                    otherRotation = otherCollider.attachedRigidbody.rotation;
                }
                else
                {
                    otherPosition = otherCollider.transform.position;
                    otherRotation = otherCollider.transform.rotation;
                }

                if (Physics.ComputePenetration(
                    _collider,
                    position,
                    Quaternion.identity,
                    otherCollider,
                    otherPosition,
                    otherRotation,
                    out var direction,
                    out var distance
                ))
                {
                    if (distance < shortestDistance)
                    {
                        foundShortest = true;
                        translation = direction * distance;
                        shortestDistance = distance;
                    }
                }
            }

            return foundShortest;
        }

        private int FindOverlappingColliders(Vector3 position)
        {
            var pointOffset = position + Vector3.up * (_capsuleHeight - _radius);
            var point0 = _capsuleCenter + pointOffset;
            var point1 = _capsuleCenter - pointOffset;

            return Physics.OverlapCapsuleNonAlloc(
              point0,
              point1,
              _radius,
              _overlapColliders
            );
        }

        private void OnValidate()
        {
            _height = Mathf.Max(_height, 0);
            _stepHeight = Mathf.Clamp(_stepHeight, 0, _height);
            _capsuleHeight = _height - _stepHeight;
            _capsuleCenter = new Vector3(0, _capsuleHeight / 2f + _stepHeight, 0);
            _radius = Mathf.Clamp(_radius, 0, _capsuleHeight / 2f);
            _skinThickness = Mathf.Max(_skinThickness, 0);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var radius = Mathf.Min(_radius, _capsuleHeight / 2f);
            var offset = _capsuleHeight / 2f - radius;
            var point0 = _capsuleCenter + Vector3.up * offset;
            var point1 = _capsuleCenter + Vector3.down * offset;

            using (new Handles.DrawingScope(transform.localToWorldMatrix))
            {
                // Draw the capsule that is our geometry shape for most collision detection
                Handles.color = Color.green;

                Handles.DrawWireDisc(point0, Vector3.up, radius);
                Handles.DrawWireDisc(point1, Vector3.down, radius);

                Handles.DrawWireArc(point0, Vector3.left, Vector3.back, -180, radius);
                Handles.DrawWireArc(point0, Vector3.back, Vector3.left, 180, radius);
                Handles.DrawWireArc(point1, Vector3.left, Vector3.back, 180, radius);
                Handles.DrawWireArc(point1, Vector3.back, Vector3.left, -180, radius);

                Handles.DrawLine(
                    _capsuleCenter + new Vector3(0, offset, -radius),
                    _capsuleCenter + new Vector3(0, -offset, -radius)
                );
                Handles.DrawLine(
                    _capsuleCenter + new Vector3(0, offset, radius),
                    _capsuleCenter + new Vector3(0, -offset, radius)
                );
                Handles.DrawLine(
                    _capsuleCenter + new Vector3(-radius, offset, 0),
                    _capsuleCenter + new Vector3(-radius, -offset, 0)
                );
                Handles.DrawLine(
                    _capsuleCenter + new Vector3(radius, offset, 0),
                    _capsuleCenter + new Vector3(radius, -offset, 0)
                );

                // Draw the bottom we simulate when doing ground checks
                Handles.color = Color.yellow;
                if (_cylinderFeet)
                {
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, radius);
                    Handles.DrawLine(
                        new Vector3(radius, 0, 0),
                        new Vector3(radius, _stepHeight + radius, 0)
                    );
                    Handles.DrawLine(
                        new Vector3(-radius, 0, 0),
                        new Vector3(-radius, _stepHeight + radius, 0)
                    );
                    Handles.DrawLine(
                        new Vector3(0, 0, radius),
                        new Vector3(0, _stepHeight + radius, radius)
                    );
                    Handles.DrawLine(
                        new Vector3(0, 0, -radius),
                        new Vector3(0, _stepHeight + radius, -radius)
                    );
                }
                else
                {
                    Handles.DrawWireDisc(new Vector3(0, radius, 0), Vector3.up, radius);
                    Handles.DrawWireArc(
                        new Vector3(0, radius, 0), 
                        Vector3.left, 
                        Vector3.back, 
                        180, 
                        radius
                    );
                    Handles.DrawWireArc(
                        new Vector3(0, radius, 0), 
                        Vector3.back, 
                        Vector3.left, 
                        -180, 
                        radius
                    );
                    Handles.DrawLine(
                        new Vector3(radius, radius, 0),
                        new Vector3(radius, _stepHeight + radius, 0)
                    );
                    Handles.DrawLine(
                        new Vector3(-radius, radius, 0),
                        new Vector3(-radius, _stepHeight + radius, 0)
                    );
                    Handles.DrawLine(
                        new Vector3(0, radius, radius),
                        new Vector3(0, _stepHeight + radius, radius)
                    );
                    Handles.DrawLine(
                        new Vector3(0, radius, -radius),
                        new Vector3(0, _stepHeight + radius, -radius)
                    );
                }
            }
        }
#endif
    }
}
