using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR;

namespace PVZ3D.Levels
{
    public class LevelTransition : MonoBehaviour
    {
        public Image fadeImage;
        public float fadeDuration = 1f;
        [SerializeField] private bool playFadeHaptics = true;
        [SerializeField, Range(0f, 1f)] private float hapticAmplitude = 0.25f;
        [SerializeField] private float hapticDuration = 0.15f;

        private static readonly List<InputDevice> hapticDevices = new List<InputDevice>();

        private void Start()
        {
            StartCoroutine(FadeInFromBlack());
        }

        public void FadeAndLoadScene(string sceneName)
        {
            StartCoroutine(FadeToBlackAndLoad(sceneName));
        }

        private IEnumerator FadeToBlackAndLoad(string sceneName)
        {
            PlayFadeHaptic();

            float elapsedTime = 0f;
            Color c = fadeImage.color;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                c.a = Mathf.Clamp01(elapsedTime / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }

            SceneManager.LoadScene(sceneName);
        }

        private IEnumerator FadeInFromBlack()
        {
            float elapsedTime = 0f;
            Color c = fadeImage.color;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                c.a = 1f - Mathf.Clamp01(elapsedTime / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }

            fadeImage.color = new Color(0f, 0f, 0f, 0f);
        }

        private void PlayFadeHaptic()
        {
            if (!playFadeHaptics) return;

            hapticDevices.Clear();
            InputDevices.GetDevicesWithCharacteristics(
                InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.HeldInHand,
                hapticDevices);

            foreach (InputDevice device in hapticDevices)
            {
                if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) &&
                    capabilities.supportsImpulse)
                {
                    device.SendHapticImpulse(0u, hapticAmplitude, hapticDuration);
                }
            }
        }
    }
}
