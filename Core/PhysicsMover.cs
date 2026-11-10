using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace KinematicCharacterController
{
    /// <summary>
    /// Component that manages the movement of moving kinematic rigidbodies for
    /// proper interaction with characters
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class PhysicsMover : MonoBehaviour
    {
        private Vector3 _internalTransientPosition;
        private Quaternion _internalTransientRotation;

        [ReadOnly]
        [SerializeField, FormerlySerializedAs("Rigidbody")]
        private Rigidbody _rigidbody;

        /// <summary>
        /// Determines if the platform moves with rigidbody.MovePosition (when true), or with rigidbody.position (when false)
        /// </summary>
        public bool MoveWithPhysics = true;

        /// <summary>
        /// Index of this motor in KinematicCharacterSystem arrays
        /// </summary>
        [NonSerialized]
        public IMoverController MoverController;

        /// <summary>
        /// Remembers latest position in interpolation
        /// </summary>
        [NonSerialized]
        public Vector3 LatestInterpolationPosition;

        /// <summary>
        /// Remembers latest rotation in interpolation
        /// </summary>
        [NonSerialized]
        public Quaternion LatestInterpolationRotation;

        /// <summary>
        /// The latest movement made by interpolation
        /// </summary>
        [NonSerialized]
        public Vector3 PositionDeltaFromInterpolation;

        /// <summary>
        /// The latest rotation made by interpolation
        /// </summary>
        [NonSerialized]
        public Quaternion RotationDeltaFromInterpolation;

        /// <summary>
        /// Index of this motor in KinematicCharacterSystem arrays
        /// </summary>
        public int IndexInCharacterSystem { get; set; }

        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        public Vector3 Velocity { get; protected set; }

        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        public Vector3 AngularVelocity { get; protected set; }

        /// <summary>
        /// Remembers initial position before all simulation are done
        /// </summary>
        public Vector3 InitialTickPosition { get; set; }

        /// <summary>
        /// Remembers initial rotation before all simulation are done
        /// </summary>
        public Quaternion InitialTickRotation { get; set; }

        /// <summary>
        /// The character's position before the movement calculations began
        /// </summary>
        public Vector3 InitialSimulationPosition { get; private set; }

        /// <summary>
        /// The character's rotation before the movement calculations began
        /// </summary>
        public Quaternion InitialSimulationRotation { get; private set; }

        /// <summary>
        /// The mover's Rigidbody
        /// </summary>
        public Rigidbody Rigidbody => _rigidbody;

        /// <summary>
        /// The mover's rotation (always up-to-date during the character update phase)
        /// </summary>
        public Vector3 TransientPosition => _internalTransientPosition;

        /// <summary>
        /// The mover's rotation (always up-to-date during the character update phase)
        /// </summary>
        public Quaternion TransientRotation => _internalTransientRotation;

        private void Awake()
        {
            ValidateData();

            _internalTransientPosition = _rigidbody.position;
            _internalTransientRotation = _rigidbody.rotation;
            InitialSimulationPosition = _rigidbody.position;
            InitialSimulationRotation = _rigidbody.rotation;
            LatestInterpolationPosition = transform.position;
            LatestInterpolationRotation = transform.rotation;
        }

        private void OnEnable()
        {
            KinematicCharacterSystem.Instance.RegisterPhysicsMover(this);
        }

        private void OnDisable()
        {
            KinematicCharacterSystem.UnregisterPhysicsMover(this);
        }

        private void Reset()
        {
            ValidateData();
        }

        private void OnValidate()
        {
            ValidateData();
        }

        /// <summary>
        /// Handle validating all required values
        /// </summary>
        private void ValidateData()
        {
            _rigidbody = gameObject.GetComponent<Rigidbody>();
            _rigidbody.centerOfMass = Vector3.zero;
            _rigidbody.maxAngularVelocity = Mathf.Infinity;
            _rigidbody.maxDepenetrationVelocity = Mathf.Infinity;
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>
        /// Sets the mover's position directly
        /// </summary>
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
            _rigidbody.position = position;
            InitialSimulationPosition = position;
            _internalTransientPosition = position;
        }

        /// <summary>
        /// Sets the mover's rotation directly
        /// </summary>
        public void SetRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
            _rigidbody.rotation = rotation;
            InitialSimulationRotation = rotation;
            _internalTransientRotation = rotation;
        }

        /// <summary>
        /// Sets the mover's position and rotation directly
        /// </summary>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            _rigidbody.position = position;
            _rigidbody.rotation = rotation;
            InitialSimulationPosition = position;
            InitialSimulationRotation = rotation;
            _internalTransientPosition = position;
            _internalTransientRotation = rotation;
        }

        /// <summary>
        /// Returns all the state information of the mover that is pertinent for simulation
        /// </summary>
        public PhysicsMoverState GetState()
        {
            return new PhysicsMoverState
            {
                Position = _internalTransientPosition,
                Rotation = _internalTransientRotation,
                Velocity = Velocity,
                AngularVelocity = AngularVelocity
            };
        }

        /// <summary>
        /// Applies a mover state instantly
        /// </summary>
        public void ApplyState(in PhysicsMoverState state)
        {
            SetPositionAndRotation(state.Position, state.Rotation);
            Velocity = state.Velocity;
            AngularVelocity = state.AngularVelocity;
        }

        /// <summary>
        /// Caches velocity values based on delta time and target position/rotations
        /// </summary>
        internal void VelocityUpdate(float deltaTime)
        {
            InitialSimulationPosition = _internalTransientPosition;
            InitialSimulationRotation = _internalTransientRotation;

            MoverController.UpdateMovement(out _internalTransientPosition, out _internalTransientRotation, deltaTime);

            if (deltaTime > 0f)
            {
                Velocity = (_internalTransientPosition - InitialSimulationPosition) / deltaTime;

                Quaternion rotationFromCurrentToGoal = _internalTransientRotation * Quaternion.Inverse(InitialSimulationRotation);
                AngularVelocity = rotationFromCurrentToGoal.eulerAngles * (Mathf.Deg2Rad / deltaTime);
            }
        }

        internal void SimulateDisplacement()
        {
            transform.SetPositionAndRotation(_internalTransientPosition, _internalTransientRotation);
            _rigidbody.position = _internalTransientPosition;
            _rigidbody.rotation = _internalTransientRotation;
        }
    }

    /// <summary>
    /// Represents the entire state of a PhysicsMover that is pertinent for simulation.
    /// Use this to save state or revert to past state
    /// </summary>
    [Serializable]
    public struct PhysicsMoverState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
    }
}
