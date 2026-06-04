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
            // We use standard polling in Update for simplicity, 
            // but the REAL improvement comes from capturing the EXACT dspTime.
        }

        public void UpdateMapping(Dictionary<Key, int> newMapping)
        {
            keyToLaneMap = new Dictionary<Key, int>(newMapping);
        }

        public void SetPaused(bool paused) => isPaused = paused;

        private void Update()
        {
            if (isPaused || keyToLaneMap == null) return;

            var kb = Keyboard.current;
            if (kb == null) return;

            foreach (var kvp in keyToLaneMap)
            {
                // To get better precision, we capture the dspTime when the input is detected.
                if (kb[kvp.Key].wasPressedThisFrame)
                {
                    double pressDspTime = AudioSettings.dspTime;
                    RhythmEvents.OnLaneDown?.Invoke(kvp.Value);
                    // Pass the precision time to a specialized event if needed, 
                    // but for now let's ensure the event is fired IMMEDIATELY.
                }
                if (kb[kvp.Key].wasReleasedThisFrame)
                {
                    RhythmEvents.OnLaneUp?.Invoke(kvp.Value);
                }
            }

            // Handle Pause Action
            if (playerInput != null && playerInput.actions["Back"].WasPressedThisFrame())
            {
                RhythmEvents.OnGamePause?.Invoke(!isPaused);
            }
        }
    }
}
