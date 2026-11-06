using System.Collections.Generic;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;

public class PauseStateHandler
{
    [RuntimeInitializeOnLoadMethod()]
    public static void Init()
    {
        EditorApplication.pauseStateChanged += HandlePauseStateChange;
    }

    private static void HandlePauseStateChange(PauseState state)
    {
        IReadOnlyList<KinematicCharacterMotor> motors = KinematicCharacterSystem.CharacterMotors;

        for (int i = 0; i < motors.Count; i++)
        {
            KinematicCharacterMotor motor = motors[i];
            motor.SetPositionAndRotation(motor.MotorTransform.position, motor.MotorTransform.rotation, true);
        }
    }
}
