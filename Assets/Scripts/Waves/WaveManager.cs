using System.Collections;
using System.Collections.Generic;
using PVZ3D.Core;
using UnityEngine;

namespace PVZ3D.Waves
{
    [System.Serializable]
    public class WavePreset
    {
        public int ZombieCount = 5;
        public float SpawnInterval = 1.25f;
        [Range(0f, 1f)] public float ToughZombieChance;
    }

    public class WaveManager : MonoBehaviour
    {
        public static WaveManager Instance { get; private set; }

        [Header("Wave Flow")]
        [SerializeField] private float interWaveDelay = 1.8f;
        [SerializeField] private List<WavePreset> presets = new List<WavePreset>();

        private Coroutine waveRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureDefaultPresets();
        }

        public void BeginMatch(int totalWaves, float prepDuration)
        {
            StopAllWaveCoroutines();
            waveRoutine = StartCoroutine(RunWaves(totalWaves, prepDuration));
        }

        public void StopAllWaveCoroutines()
        {
            if (waveRoutine != null)
            {
                StopCoroutine(waveRoutine);
                waveRoutine = null;
            }
        }

        private IEnumerator RunWaves(int totalWaves, float prepDuration)
        {
            if (prepDuration > 0f)
            {
                yield return new WaitForSeconds(prepDuration);
            }

            GameManager.Instance?.OnPrepEnded();

            for (int waveIndex = 1; waveIndex <= totalWaves; waveIndex++)
            {
                if (GameManager.Instance != null && GameManager.Instance.State.Phase == GamePhase.Lose)
                {
                    yield break;
                }

                WavePreset preset = GetPresetForWave(waveIndex);
                GameManager.Instance?.SetWave(waveIndex, totalWaves);
                GameEvents.RaiseWaveStarted(waveIndex);

                yield return StartCoroutine(SpawnWave(waveIndex, preset));
                yield return StartCoroutine(WaitUntilWaveCleared());

                if (GameManager.Instance != null && GameManager.Instance.State.Phase == GamePhase.Lose)
                {
                    yield break;
                }

                GameManager.Instance?.MarkWaveCompleted(waveIndex);

                if (waveIndex < totalWaves && interWaveDelay > 0f)
                {
                    yield return new WaitForSeconds(interWaveDelay);
                }
            }

            GameManager.Instance?.MarkAllWavesSpawned();
        }

        private IEnumerator SpawnWave(int waveIndex, WavePreset preset)
        {
            if (ZombieSpawner.Instance == null)
            {
                yield break;
            }

            int lanes = Grid.LawnGridManager.Instance != null ? Grid.LawnGridManager.Instance.Lanes : 5;
            if (lanes <= 0)
            {
                yield break;
            }
            for (int i = 0; i < preset.ZombieCount; i++)
            {
                if (GameManager.Instance != null && GameManager.Instance.State.Phase == GamePhase.Lose)
                {
                    yield break;
                }

                bool tough = Random.value < preset.ToughZombieChance;
                int lane = Random.Range(0, lanes);
                ZombieSpawner.Instance.SpawnZombie(lane, tough);

                float wait = Mathf.Max(0.15f, preset.SpawnInterval);
                yield return new WaitForSeconds(wait);
            }
        }

        private IEnumerator WaitUntilWaveCleared()
        {
            while (GameManager.Instance != null && GameManager.Instance.State.AliveZombies > 0)
            {
                if (GameManager.Instance.State.Phase == GamePhase.Lose)
                {
                    yield break;
                }

                yield return null;
            }
        }

        private WavePreset GetPresetForWave(int waveIndex)
        {
            if (presets.Count == 0)
            {
                EnsureDefaultPresets();
            }

            int idx = Mathf.Clamp(waveIndex - 1, 0, presets.Count - 1);
            return presets[idx];
        }

        private void EnsureDefaultPresets()
        {
            if (presets.Count > 0)
            {
                return;
            }

            presets.Add(new WavePreset { ZombieCount = 4, SpawnInterval = 1.25f, ToughZombieChance = 0.08f });
            presets.Add(new WavePreset { ZombieCount = 6, SpawnInterval = 1.05f, ToughZombieChance = 0.2f });
            presets.Add(new WavePreset { ZombieCount = 8, SpawnInterval = 0.92f, ToughZombieChance = 0.32f });
        }
    }
}
