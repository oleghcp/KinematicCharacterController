using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace KinematicCharacterController
{
    /// <summary>
    /// The system that manages the simulation of KinematicCharacterMotor and PhysicsMover
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class KinematicCharacterSystem : MonoBehaviour
    {
        private static KinematicCharacterSystem _instance;

        private float _lastCustomInterpolationStartTime = -1f;
        private float _lastCustomInterpolationDeltaTime = -1f;

        private List<KinematicCharacterMotor> _characterMotors = new List<KinematicCharacterMotor>();
        private List<PhysicsMover> _physicsMovers = new List<PhysicsMover>();

        /// <summary>
        /// Determines if the system simulates automatically.
        /// If true, the simulation is done on FixedUpdate
        /// </summary>
        [Tooltip("Determines if the system simulates automatically. If true, the simulation is done on FixedUpdate")]
        public bool AutoSimulation = true;

        /// <summary>
        /// Should interpolation of characters and PhysicsMovers be handled
        /// </summary>
        [Tooltip("Should interpolation of characters and PhysicsMovers be handled")]
        public bool Interpolate = true;

        /// <summary>
        /// Gets the KinematicCharacterSystem instance if any
        /// </summary>
        public static KinematicCharacterSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject systemGameObject = new GameObject(nameof(KinematicCharacterSystem));
                    _instance = systemGameObject.AddComponent<KinematicCharacterSystem>();

                    systemGameObject.hideFlags = HideFlags.NotEditable;
                    _instance.hideFlags = HideFlags.NotEditable;

                    DontDestroyOnLoad(systemGameObject);
                }

                return _instance;
            }
        }

        public static bool Existing => _instance != null;

        public static IReadOnlyList<KinematicCharacterMotor> CharacterMotors => _instance._characterMotors;
        public static IReadOnlyList<PhysicsMover> PhysicsMovers => _instance._physicsMovers;

        // This is to prevent duplicating the singleton game object on script recompiles
        private void OnDisable()
        {
            Destroy(gameObject);
        }

        private void FixedUpdate()
        {
            if (!AutoSimulation)
                return;

            float deltaTime = Time.deltaTime;

            if (Interpolate)
                PreSimulationInterpolationUpdate(deltaTime);

            Simulate(deltaTime, _characterMotors, _physicsMovers);

            if (Interpolate)
                PostSimulationInterpolationUpdate(deltaTime);
        }

        private void LateUpdate()
        {
            if (Interpolate)
                CustomInterpolationUpdate();
        }

        /// <summary>
        /// Sets the maximum capacity of the character motors list, to prevent allocations when adding characters
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetCharacterMotorsCapacity(int capacity)
        {
            List<KinematicCharacterMotor> motors = _instance._characterMotors;

            if (capacity < motors.Count)
                capacity = motors.Count;

            motors.Capacity = capacity;
        }

        /// <summary>
        /// Sets the maximum capacity of the physics movers list, to prevent allocations when adding movers
        /// </summary>
        /// <param name="capacity"></param>
        public static void SetPhysicsMoversCapacity(int capacity)
        {
            List<PhysicsMover> movers = _instance._physicsMovers;

            if (capacity < movers.Count)
                capacity = movers.Count;

            movers.Capacity = capacity;
        }

        /// <summary>
        /// Registers a KinematicCharacterMotor into the system
        /// </summary>
        internal void RegisterCharacterMotor(KinematicCharacterMotor motor)
        {
            _characterMotors.Add(motor);
        }

        /// <summary>
        /// Unregisters a KinematicCharacterMotor from the system
        /// </summary>
        internal static void UnregisterCharacterMotor(KinematicCharacterMotor motor)
        {
            if (_instance)
                _instance._characterMotors.Remove(motor);
        }

        /// <summary>
        /// Registers a PhysicsMover into the system
        /// </summary>
        internal void RegisterPhysicsMover(PhysicsMover mover)
        {
            _physicsMovers.Add(mover);
            mover.Rigidbody.interpolation = RigidbodyInterpolation.None;
        }

        /// <summary>
        /// Unregisters a PhysicsMover from the system
        /// </summary>
        internal static void UnregisterPhysicsMover(PhysicsMover mover)
        {
            if (_instance)
                _instance._physicsMovers.Remove(mover);
        }

        /// <summary>
        /// Remembers the point to interpolate from for KinematicCharacterMotors and PhysicsMovers
        /// </summary>
        private static void PreSimulationInterpolationUpdate(float deltaTime)
        {
            // Save pre-simulation poses and place transform at transient pose
            List<KinematicCharacterMotor> motors = _instance._characterMotors;
            for (int i = 0; i < motors.Count; i++)
            {
                KinematicCharacterMotor motor = motors[i];

                motor.InitialTickPosition = motor.TransientPosition;
                motor.InitialTickRotation = motor.TransientRotation;

                motor.MotorTransform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }

            List<PhysicsMover> movers = _instance._physicsMovers;
            for (int i = 0; i < movers.Count; i++)
            {
                PhysicsMover mover = movers[i];

                mover.InitialTickPosition = mover.TransientPosition;
                mover.InitialTickRotation = mover.TransientRotation;

                mover.transform.SetPositionAndRotation(mover.TransientPosition, mover.TransientRotation);
                mover.Rigidbody.position = mover.TransientPosition;
                mover.Rigidbody.rotation = mover.TransientRotation;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Simulate(float deltaTime)
        {
            Simulate(deltaTime, _instance._characterMotors, _instance._physicsMovers);
        }

        /// <summary>
        /// Ticks characters and/or movers
        /// </summary>
        public static void Simulate(float deltaTime, List<KinematicCharacterMotor> motors, List<PhysicsMover> movers)
        {
            int characterMotorsCount = motors.Count;
            int physicsMoversCount = movers.Count;

            // Update PhysicsMover velocities
            for (int i = 0; i < physicsMoversCount; i++)
            {
                movers[i].VelocityUpdate(deltaTime);
            }

            // Character controller update phase 1
            for (int i = 0; i < characterMotorsCount; i++)
            {
                motors[i].UpdatePhase1(deltaTime);
            }

            // Simulate PhysicsMover displacement
            for (int i = 0; i < physicsMoversCount; i++)
            {
                movers[i].SimulateDisplacement();
            }

            // Character controller update phase 2 and move
            for (int i = 0; i < characterMotorsCount; i++)
            {
                KinematicCharacterMotor motor = motors[i];

                motor.UpdatePhase2(deltaTime);
                motor.MotorTransform.SetPositionAndRotation(motor.TransientPosition, motor.TransientRotation);
            }
        }

        /// <summary>
        /// Initiates the interpolation for KinematicCharacterMotors and PhysicsMovers
        /// </summary>
        public static void PostSimulationInterpolationUpdate(float deltaTime)
        {
            _instance._lastCustomInterpolationStartTime = Time.time;
            _instance._lastCustomInterpolationDeltaTime = deltaTime;

            // Return interpolated roots to their initial poses
            List<KinematicCharacterMotor> motors = _instance._characterMotors;
            for (int i = 0; i < motors.Count; i++)
            {
                KinematicCharacterMotor motor = motors[i];
                motor.MotorTransform.SetPositionAndRotation(motor.InitialTickPosition, motor.InitialTickRotation);
            }

            List<PhysicsMover> movers = _instance._physicsMovers;
            for (int i = 0; i < movers.Count; i++)
            {
                PhysicsMover mover = movers[i];

                if (mover.MoveWithPhysics)
                {
                    mover.Rigidbody.position = mover.InitialTickPosition;
                    mover.Rigidbody.rotation = mover.InitialTickRotation;

                    mover.Rigidbody.MovePosition(mover.TransientPosition);
                    mover.Rigidbody.MoveRotation(mover.TransientRotation);
                }
                else
                {
                    mover.Rigidbody.position = mover.TransientPosition;
                    mover.Rigidbody.rotation = mover.TransientRotation;
                }
            }
        }

        /// <summary>
        /// Handles per-frame interpolation
        /// </summary>
        private static void CustomInterpolationUpdate()
        {
            float interpolationFactor = Mathf.Clamp01((Time.time - _instance._lastCustomInterpolationStartTime) / _instance._lastCustomInterpolationDeltaTime);

            // Handle characters interpolation
            List<KinematicCharacterMotor> motors = _instance._characterMotors;
            for (int i = 0; i < motors.Count; i++)
            {
                KinematicCharacterMotor motor = motors[i];

                motor.MotorTransform.SetPositionAndRotation(Vector3.Lerp(motor.InitialTickPosition, motor.TransientPosition, interpolationFactor),
                                                            Quaternion.Slerp(motor.InitialTickRotation, motor.TransientRotation, interpolationFactor));
            }

            // Handle PhysicsMovers interpolation
            List<PhysicsMover> movers = _instance._physicsMovers;
            for (int i = 0; i < movers.Count; i++)
            {
                PhysicsMover mover = movers[i];
                Transform mTransform = mover.transform;

                mTransform.SetPositionAndRotation(Vector3.Lerp(mover.InitialTickPosition, mover.TransientPosition, interpolationFactor),
                                                  Quaternion.Slerp(mover.InitialTickRotation, mover.TransientRotation, interpolationFactor));

                Vector3 newPos = mTransform.position;
                Quaternion newRot = mTransform.rotation;
                mover.PositionDeltaFromInterpolation = newPos - mover.LatestInterpolationPosition;
                mover.RotationDeltaFromInterpolation = Quaternion.Inverse(mover.LatestInterpolationRotation) * newRot;
                mover.LatestInterpolationPosition = newPos;
                mover.LatestInterpolationRotation = newRot;
            }
        }
    }
}
