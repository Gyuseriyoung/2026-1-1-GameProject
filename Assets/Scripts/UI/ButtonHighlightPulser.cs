using UnityEngine;
using UnityEngine.UI;

namespace CookingGame
{
    public class ButtonHighlightPulser : MonoBehaviour
    {
        [Header("Pulse Settings")]
        public float pulseSpeed = 2f;
        public float pulseAmount = 0.05f;

        private Vector3 originalScale;

        private void Start()
        {
            originalScale = transform.localScale;
        }

        private void Update()
        {
            float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = originalScale * (1f + scaleOffset);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            // Find all Buttons in the scene
            var buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
            foreach (var btn in buttons)
            {
                string name = btn.gameObject.name.ToLower();
                
                // Specifically highlight Setting and Recipe buttons
                // "click button" is the recipe button in the dialogue scene
                if (name.Contains("setting") || name.Contains("click") || name.Contains("recipe"))
                {
                    if (btn.gameObject.GetComponent<ButtonHighlightPulser>() == null)
                    {
                        btn.gameObject.AddComponent<ButtonHighlightPulser>();
                    }
                }
            }
        }
    }
}
