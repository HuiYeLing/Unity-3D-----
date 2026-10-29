using System.Collections;
using TMPro;
using UnityEngine;

namespace Inspection
{
    /// 轻量提示条：成功/失败/警告都走这里，保证用户一定看到明确反馈
    public class Toast : MonoBehaviour
    {
        public enum Kind { Info, Success, Warn, Error }

        [SerializeField] private CanvasGroup group;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private float holdSeconds = 2.2f;

        private static Toast instance;
        private Coroutine running;

        private void Awake()
        {
            instance = this;
            if (group != null) group.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        public static void Show(string message, Kind kind = Kind.Info)
        {
            if (instance == null)
            {
                Debug.Log("[Toast] " + message);
                return;
            }
            instance.Play(message, kind);
        }

        private void Play(string message, Kind kind)
        {
            if (group == null || label == null) return;

            if (running != null) StopCoroutine(running);
            label.text = message;
            label.color = kind switch
            {
                Kind.Success => new Color(0.18f, 0.66f, 0.44f),
                Kind.Warn => new Color(0.94f, 0.62f, 0.15f),
                Kind.Error => new Color(0.89f, 0.29f, 0.27f),
                _ => Color.white
            };
            running = StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            group.alpha = 1f;
            yield return new WaitForSecondsRealtime(holdSeconds);

            const float fade = 0.35f;
            for (float t = 0f; t < fade; t += Time.unscaledDeltaTime)
            {
                group.alpha = 1f - t / fade;
                yield return null;
            }
            group.alpha = 0f;
        }
    }
}
