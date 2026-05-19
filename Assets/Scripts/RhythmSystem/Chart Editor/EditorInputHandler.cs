using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace RhythmSystem
{
    public class EditorInputHandler : MonoBehaviour
    {
        private EditorManager editorManager;

        public InputActionAsset inputActions;
        private InputAction playPauseAction;
        private InputAction stopAction;
        private InputAction addNoteAction;
        private InputAction removeNoteAction;
        private InputAction scrollAction;
        private InputAction modifierAction;
        private InputAction mousePosAction;
        private InputAction copyAction;
        private InputAction pasteAction;
        private InputAction deleteAction;
        private InputAction quickSaveAction;

        private bool isDraggingTimeline = false;
        private bool isCreatingHold = false;
        private Vector2 lastMousePosition;

        public Vector2 MousePosition => mousePosAction.ReadValue<Vector2>();

        public void Init(EditorManager manager)
        {
            editorManager = manager;
            SetupInputs();
        }

        private void SetupInputs()
        {
            if (inputActions == null) return;
            var editorMap = inputActions.FindActionMap("Editor");
            if (editorMap == null) return;

            playPauseAction = editorMap.FindAction("PlayPause");
            stopAction = editorMap.FindAction("Stop");
            addNoteAction = editorMap.FindAction("AddNote");
            removeNoteAction = editorMap.FindAction("RemoveNote");
            scrollAction = editorMap.FindAction("Scroll");
            modifierAction = editorMap.FindAction("Modifier");
            mousePosAction = editorMap.FindAction("MousePos");
            copyAction = editorMap.FindAction("Copy");
            pasteAction = editorMap.FindAction("Paste");
            deleteAction = editorMap.FindAction("Delete");
            quickSaveAction = editorMap.FindAction("QuickSave");

            UnregisterCallbacks();

            if (playPauseAction != null) playPauseAction.performed += OnPlayPause;
            if (stopAction != null) stopAction.performed += OnStop;
            if (copyAction != null) copyAction.performed += OnCopy;
            if (pasteAction != null) pasteAction.performed += OnPaste;
            if (deleteAction != null) deleteAction.performed += OnDelete;
            if (quickSaveAction != null) quickSaveAction.performed += OnQuickSave;
        }

        private void UnregisterCallbacks()
        {
            if (playPauseAction != null) playPauseAction.performed -= OnPlayPause;
            if (stopAction != null) stopAction.performed -= OnStop;
            if (copyAction != null) copyAction.performed -= OnCopy;
            if (pasteAction != null) pasteAction.performed -= OnPaste;
            if (deleteAction != null) deleteAction.performed -= OnDelete;
            if (quickSaveAction != null) quickSaveAction.performed -= OnQuickSave;
        }

        private void OnPlayPause(InputAction.CallbackContext _) => editorManager.PlayPause();
        private void OnStop(InputAction.CallbackContext _) => editorManager.StopPlayback();
        private void OnCopy(InputAction.CallbackContext _) => editorManager.noteManager.CopySelection();
        private void OnPaste(InputAction.CallbackContext _) => editorManager.noteManager.PasteClipboard();
        private void OnDelete(InputAction.CallbackContext _) => editorManager.noteManager.DeleteSelection();
        private void OnQuickSave(InputAction.CallbackContext _) => editorManager.QuickSave();

        public void Enable() => inputActions?.Enable();
        public void Disable() 
        {
            inputActions?.Disable();
            UnregisterCallbacks();
        }

        private void OnDestroy()
        {
            UnregisterCallbacks();
        }

        public void HandleInputs()
        {
            bool overUI = IsOverBlockingUI();

            if (!overUI)
            {
                HandleNavigation();
            }
            
            if (!editorManager.isPlaying && !overUI)
            {
                switch (editorManager.currentMode)
                {
                    case EditorMode.Place:
                        HandlePlaceMode();
                        break;
                    case EditorMode.Select:
                        HandleSelectMode();
                        break;
                    case EditorMode.Gimmick:
                        HandleGimmickMode();
                        break;
                }
            }
        }

        private void HandlePlaceMode()
        {
            if (addNoteAction.WasPressedThisFrame()) 
            {
                editorManager.noteManager.AddNoteAtMouse(MousePosition);
                if (editorManager.currentSelectedNoteType == NoteType.Hold)
                {
                    isCreatingHold = true;
                }
            }
            else if (isCreatingHold)
            {
                if (addNoteAction.IsPressed())
                {
                    editorManager.noteManager.UpdateHoldNoteCreation(MousePosition);
                }
                else
                {
                    editorManager.noteManager.FinalizeHoldNoteCreation();
                    isCreatingHold = false;
                }
            }
            else if (removeNoteAction.WasPressedThisFrame()) 
            {
                editorManager.noteManager.RemoveNoteAtMouse(MousePosition);
            }
        }

        private void HandleSelectMode()
        {
            if (addNoteAction.WasPressedThisFrame() || removeNoteAction.WasPressedThisFrame())
            {
                // Try selecting gimmick first, then note
                float timeMs = editorManager.timelineManager.GetTimeFromMouse(MousePosition) * 1000f;
                int lane = editorManager.timelineManager.GetLaneFromMouse(MousePosition);
                var gimmick = editorManager.currentChart.gimmicks
                    .FirstOrDefault(g => Mathf.Abs(g.time - timeMs) < 100f && g.targetLane == lane);

                if (gimmick != null)
                {
                    editorManager.noteManager.SelectGimmick(gimmick);
                }
                else
                {
                    editorManager.noteManager.ToggleSelectionAtMouse(MousePosition);
                }
            }
        }

        private void HandleGimmickMode()
        {
            if (addNoteAction.WasPressedThisFrame()) 
            {
                float timeMs = editorManager.timelineManager.GetTimeFromMouse(MousePosition) * 1000f;
                int lane = editorManager.timelineManager.GetLaneFromMouse(MousePosition);
                
                var existingGimmick = editorManager.currentChart.gimmicks
                    .FirstOrDefault(g => Mathf.Abs(g.time - timeMs) < 100f && g.targetLane == lane);

                if (existingGimmick != null)
                {
                    editorManager.noteManager.SelectGimmick(existingGimmick);
                }
                else
                {
                    editorManager.noteManager.AddGimmickAtMouse(MousePosition, editorManager.currentSelectedGimmickType);
                }
            }
            else if (removeNoteAction.WasPressedThisFrame()) 
            {
                editorManager.noteManager.RemoveGimmickAtMouse(MousePosition);
            }
        }

        private void HandleNavigation()
        {
            // --- Timeline Dragging (Middle Mouse) ---
            if (Mouse.current != null)
            {
                if (Mouse.current.middleButton.wasPressedThisFrame)
                {
                    isDraggingTimeline = true;
                    lastMousePosition = Mouse.current.position.ReadValue();
                }
                else if (Mouse.current.middleButton.wasReleasedThisFrame)
                {
                    isDraggingTimeline = false;
                }

                if (isDraggingTimeline)
                {
                    Vector2 currentMousePos = Mouse.current.position.ReadValue();
                    float deltaX = currentMousePos.x - lastMousePosition.x;
                    
                    if (Mathf.Abs(deltaX) > 0.01f)
                    {
                        float timeDelta = deltaX / editorManager.currentScrollSpeed;
                        editorManager.SetPlaybackTime(editorManager.EditorTime + timeDelta);
                        lastMousePosition = currentMousePos;
                    }
                    return;
                }
            }

            // --- Scroll Logic ---
            if (scrollAction == null) return;

            Vector2 scrollDelta = scrollAction.ReadValue<Vector2>();
            if (scrollDelta.y == 0) return;

            if (modifierAction != null && modifierAction.IsPressed())
            {
                editorManager.currentScrollSpeed = Mathf.Clamp(editorManager.currentScrollSpeed + (scrollDelta.y > 0 ? 10f : -10f), 100f, 5000f);
                editorManager.editorUIController.RefreshScrollField();
                editorManager.RefreshAllVisuals();
            }
            else
            {
                float targetTime = editorManager.EditorTime + (scrollDelta.y > 0 ? -0.1f : 0.1f);
                editorManager.SetPlaybackTime(targetTime);
            }
        }

        public bool IsOverBlockingUI()
        {
            if (EventSystem.current == null) return false;
            
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = MousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            return results.Any(r => r.gameObject.GetComponentInParent<Selectable>() != null);
        }
    }
}
