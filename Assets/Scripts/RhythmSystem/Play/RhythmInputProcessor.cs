using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace RhythmSystem.Play
{
    public class RhythmInputProcessor : MonoBehaviour
    {
        private PlayerInput playerInput;
        private bool isPaused = false;
        
        // Maps KeyCode to Lane Index
        private Dictionary<Key, int> keyToLaneMap = new Dictionary<Key, int>();

        public void Initialize(PlayerInput input)
        {
            playerInput = input;
            if (playerInput != null)
            {
                playerInput.onActionTriggered += OnActionTriggered;
            }
        }

        public void UpdateMapping(Dictionary<Key, int> newMapping)
        {
            keyToLaneMap = new Dictionary<Key, int>(newMapping);
        }

        private void OnDestroy()
        {
            if (playerInput != null)
            {
                playerInput.onActionTriggered -= OnActionTriggered;
            }
        }

        public void SetPaused(bool paused) => isPaused = paused;

        private void Update()
        {
            if (isPaused || keyToLaneMap == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            foreach (var kvp in keyToLaneMap)
            {
                if (kb[kvp.Key].wasPressedThisFrame)
                {
                    RhythmEvents.OnLaneDown?.Invoke(kvp.Value);
                }
                if (kb[kvp.Key].wasReleasedThisFrame)
                {
                    RhythmEvents.OnLaneUp?.Invoke(kvp.Value);
                }
            }
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            string actionName = context.action.name;

            if (context.performed)
            {
                if (actionName == "Back")
                {
                    RhythmEvents.OnGamePause?.Invoke(!isPaused);
                    return;
                }
            }
        }
    }
}
