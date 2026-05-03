using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace PVZ3D.Levels
{
    public class LevelTransition : MonoBehaviour
    {
        public Image fadeImage;
        public float fadeDuration = 1f;

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
    }
}
