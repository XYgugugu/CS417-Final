using System.Collections.Generic;
using UnityEngine;

namespace PVZ3D.Core
{
    public class AudioFeedbackManager : MonoBehaviour
    {
        public static AudioFeedbackManager Instance { get; private set; }

        [Header("Output")]
        [SerializeField] private float masterVolume = 0.35f;
        [SerializeField] private float uiVolume = 0.2f;
        [SerializeField] private float gameplayVolume = 0.28f;
        [SerializeField] private float resultVolume = 0.38f;

        private AudioSource source;
        private readonly Dictionary<int, AudioClip> clipCache = new Dictionary<int, AudioClip>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            source = GetComponent<AudioSource>();
            if (source == null)
            {
                source = gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        private void OnEnable()
        {
            GameEvents.OnPurchaseResult += HandlePurchaseResult;
            GameEvents.OnResourceCollected += HandleResourceCollected;
            GameEvents.OnZombieKilled += HandleZombieKilled;
            GameEvents.OnPlantFired += HandlePlantFired;
            GameEvents.OnBaseDamaged += HandleBaseDamaged;
            GameEvents.OnGameWon += HandleGameWon;
            GameEvents.OnGameLost += HandleGameLost;
            GameEvents.OnWaveStarted += HandleWaveStarted;
        }

        private void OnDisable()
        {
            GameEvents.OnPurchaseResult -= HandlePurchaseResult;
            GameEvents.OnResourceCollected -= HandleResourceCollected;
            GameEvents.OnZombieKilled -= HandleZombieKilled;
            GameEvents.OnPlantFired -= HandlePlantFired;
            GameEvents.OnBaseDamaged -= HandleBaseDamaged;
            GameEvents.OnGameWon -= HandleGameWon;
            GameEvents.OnGameLost -= HandleGameLost;
            GameEvents.OnWaveStarted -= HandleWaveStarted;
        }

        public void PlayUiHover()
        {
            PlayTone(880f, 0.035f, uiVolume * 0.85f);
        }

        public void PlayUiClick()
        {
            PlayTone(990f, 0.05f, uiVolume);
        }

        private void HandlePurchaseResult(bool success, string _)
        {
            if (success)
            {
                PlayTone(720f, 0.07f, gameplayVolume);
            }
            else
            {
                PlayTone(250f, 0.1f, gameplayVolume * 1.1f);
            }
        }

        private void HandleResourceCollected(string type, int amount)
        {
            float baseFreq = type == "Sun" ? 640f : 520f;
            float dur = type == "Sun" ? 0.08f : 0.06f;
            float amountBoost = Mathf.Clamp(amount / 50f, 0f, 0.25f);
            PlayTone(baseFreq * (1f + amountBoost), dur, gameplayVolume * 0.95f);
        }

        private void HandleZombieKilled(int _)
        {
            PlayTone(430f, 0.08f, gameplayVolume * 1.1f);
        }

        private void HandlePlantFired(int _)
        {
            PlayTone(560f, 0.035f, gameplayVolume * 0.55f);
        }

        private void HandleBaseDamaged(int amount)
        {
            float freq = Mathf.Clamp(220f - (amount * 6f), 150f, 220f);
            PlayTone(freq, 0.14f, gameplayVolume * 1.2f);
        }

        private void HandleWaveStarted(int wave)
        {
            float freq = 500f + (wave * 40f);
            PlayTone(freq, 0.09f, gameplayVolume);
        }

        private void HandleGameWon()
        {
            PlayTone(660f, 0.12f, resultVolume);
            PlayTone(880f, 0.12f, resultVolume);
            PlayTone(1040f, 0.14f, resultVolume);
        }

        private void HandleGameLost()
        {
            PlayTone(420f, 0.18f, resultVolume);
            PlayTone(260f, 0.22f, resultVolume);
        }

        private void PlayTone(float frequency, float duration, float volume)
        {
            if (source == null)
            {
                return;
            }

            int key = Mathf.RoundToInt(frequency * 10f) * 1000 + Mathf.RoundToInt(duration * 1000f);
            if (!clipCache.TryGetValue(key, out AudioClip clip) || clip == null)
            {
                clip = CreateToneClip(Mathf.Clamp(frequency, 120f, 2000f), Mathf.Clamp(duration, 0.02f, 0.4f));
                clipCache[key] = clip;
            }

            source.PlayOneShot(clip, Mathf.Clamp01(volume * masterVolume));
        }

        private static AudioClip CreateToneClip(float frequency, float duration)
        {
            const int sampleRate = 44100;
            int sampleCount = Mathf.Max(128, Mathf.CeilToInt(duration * sampleRate));
            float[] samples = new float[sampleCount];
            float fadeOutStart = sampleCount * 0.78f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)sampleRate;
                float sine = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float envelope = i < fadeOutStart ? 1f : 1f - ((i - fadeOutStart) / (sampleCount - fadeOutStart));
                samples[i] = sine * envelope * 0.5f;
            }

            AudioClip clip = AudioClip.Create($"Tone_{frequency:F0}_{duration:F2}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
