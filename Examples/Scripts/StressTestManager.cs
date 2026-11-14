using UnityEngine;
using UnityEngine.UI;

namespace KinematicCharacterController.Examples
{
    public class StressTestManager : MonoBehaviour
    {
        public Camera Camera;
        public LayerMask UIMask;

        public InputField CountField;
        public Image RenderOn;
        public Image SimOn;
        public Image InterpOn;
        public ExampleCharacterController CharacterPrefab;
        public ExampleAIController AIController;
        public int SpawnCount = 100;
        public float SpawnDistance = 2f;

        private PhysicsSimulation _simulation;

        private void Start()
        {
            CountField.text = SpawnCount.ToString();
            UpdateOnImages();

            KinematicCharacterSystem.Instance.AutoSimulation = false;
            KinematicCharacterSystem.Instance.Interpolate = false;
        }

        private void Update()
        {
            KinematicCharacterSystem.Simulate(Time.deltaTime);
        }

        private void UpdateOnImages()
        {
            RenderOn.enabled = Camera.cullingMask == -1;
            SimOn.enabled = _simulation.Value;
            InterpOn.enabled = KinematicCharacterSystem.Instance.Interpolate;
        }

        public void SetSpawnCount(string count)
        {
            if (int.TryParse(count, out int result))
                SpawnCount = result;
        }

        public void ToggleRendering()
        {
            if (Camera.cullingMask == -1)
                Camera.cullingMask = UIMask;
            else
                Camera.cullingMask = -1;

            UpdateOnImages();
        }

        public void TogglePhysicsSim()
        {
            _simulation.Switch();
            UpdateOnImages();
        }

        public void ToggleInterpolation()
        {
            KinematicCharacterSystem.Instance.Interpolate = !KinematicCharacterSystem.Instance.Interpolate;
            UpdateOnImages();
        }

        public void Spawn()
        {
            for (int i = 0; i < AIController.Characters.Count; i++)
            {
                Destroy(AIController.Characters[i].gameObject);
            }
            AIController.Characters.Clear();

            int charsPerRow = Mathf.CeilToInt(Mathf.Sqrt(SpawnCount));
            Vector3 firstPos = -Vector3.one * (charsPerRow * SpawnDistance * 0.5f);
            firstPos.y = 0f;

            for (int i = 0; i < SpawnCount; i++)
            {
                int row = i / charsPerRow;
                int col = i % charsPerRow;
                Vector3 pos = firstPos + (Vector3.right * (row * SpawnDistance)) + (Vector3.forward * (col * SpawnDistance));

                ExampleCharacterController newChar = Instantiate(CharacterPrefab);
                newChar.Motor.SetPosition(pos);

                AIController.Characters.Add(newChar);
            }
        }

        private struct PhysicsSimulation
        {
            public bool Value
            {
                get
                {
#if UNITY_2022_3_OR_NEWER
                    return Physics.simulationMode != SimulationMode.Script;
#else
                    return Physics.autoSimulation;
#endif
                }
                set
                {
#if UNITY_2022_3_OR_NEWER
                    Physics.simulationMode = value ? SimulationMode.FixedUpdate
                                                   : SimulationMode.Script;
#else
                    Physics.autoSimulation = value;
#endif
                }
            }

            public void Switch()
            {
                Value = !Value;
            }
        }
    }
}
