using System.Collections;
using TMPro;
using UnityEngine;

namespace PVZ3D.UI.Tutorial
{
    /// <summary>
    /// Reusable "first-time tutorial pop-up" component. Sits on a HUD child
    /// GameObject (a TMP_Text panel placed next to the relevant mechanic) and
    /// fires exactly ONCE per session when its trigger calls <see cref="Trigger"/>.
    ///
    /// Visibility is driven solely by a <see cref="CanvasGroup"/> alpha tween,
    /// not <c>GameObject.SetActive</c>. Keeping the GameObject active avoids
    /// the Awake/Trigger race where activating-on-trigger would re-fire Awake
    /// and immediately re-deactivate, killing the coroutine.
    /// </summary>
    [DisallowMultipleComponent]
    public class TutorialPopup : MonoBehaviour
    {
        [Tooltip("The text element this pop-up drives. If left null, the first TMP_Text in children is auto-bound on Awake/Trigger.")]
        [SerializeField] private TMP_Text label;

        [Tooltip("CanvasGroup used for fading. If left null, one is added automatically on the same GameObject as the label.")]
        [SerializeField] private CanvasGroup fadeGroup;

        [Header("Timing")]
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float holdDuration = 4.5f;
        [SerializeField] private float fadeOutDuration = 0.6f;

        [Header("Behavior")]
        [Tooltip("If true, fires only once per session. If false, every Trigger() call replays.")]
        [SerializeField] private bool firstTimeOnly = true;

        private bool _hasFired;
        private Coroutine _running;

        public bool HasFired => _hasFired;

        private void Awake()
        {
            EnsureBindings();
            if (fadeGroup != null)
            {
                fadeGroup.alpha = 0f;
                fadeGroup.interactable = false;
                fadeGroup.blocksRaycasts = false;
            }
        }

        /// <summary>Fire the pop-up with the given text. No-op if already fired and firstTimeOnly is on.</summary>
        public void Trigger(string message)
        {
            if (firstTimeOnly && _hasFired) return;
            _hasFired = true;

            EnsureBindings(); // Awake may not have run yet on first frame; bind lazily.

            if (label != null) label.text = message;
            if (fadeGroup != null) fadeGroup.alpha = 0f; // start from 0 every time

            if (_running != null) StopCoroutine(_running);
            _running = StartCoroutine(FadeRoutine());
        }

        /// <summary>Re-arm the popup so it can fire again. Call on new game / scene reset.</summary>
        public void Reset()
        {
            _hasFired = false;
            if (_running != null) { StopCoroutine(_running); _running = null; }
            if (fadeGroup != null) fadeGroup.alpha = 0f;
        }

        // --- Editor-only debug hooks ---
        [ContextMenu("Test ▶ Fire Popup Now")]
        private void DebugFireNow()
        {
            EnsureBindings();
            string msg = label != null && !string.IsNullOrEmpty(label.text)
                ? label.text
                : "(test) Tutorial popup fired from inspector.";
            _hasFired = false;
            Trigger(msg);
        }

        [ContextMenu("Test ▶ Reset Fired Flag")]
        private void DebugReset() => Reset();

        // -----------------------------------------------------------------

        private void EnsureBindings()
        {
            if (label == null) label = GetComponentInChildren<TMP_Text>(true);
            if (fadeGroup == null)
            {
                fadeGroup = GetComponent<CanvasGroup>();
                if (fadeGroup == null && label != null)
                {
                    fadeGroup = label.GetComponent<CanvasGroup>();
                    if (fadeGroup == null) fadeGroup = label.gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private IEnumerator FadeRoutine()
        {
            // Fade in
            float t = 0f;
            while (t < fadeInDuration)
            {
                t += Time.unscaledDeltaTime;
                if (fadeGroup != null) fadeGroup.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, fadeInDuration));
                yield return null;
            }
            if (fadeGroup != null) fadeGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(holdDuration);

            // Fade out
            t = 0f;
            while (t < fadeOutDuration)
            {
                t += Time.unscaledDeltaTime;
                if (fadeGroup != null) fadeGroup.alpha = 1f - Mathf.Clamp01(t / Mathf.Max(0.01f, fadeOutDuration));
                yield return null;
            }
            if (fadeGroup != null) fadeGroup.alpha = 0f;

            _running = null;
        }
    }
}
