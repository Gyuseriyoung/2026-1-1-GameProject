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

            if (playPauseAction != null) playPauseAction.performed += _ => editorManager.PlayPause();
            if (stopAction != null) stopAction.performed += _ => editorManager.StopPlayback();
            if (copyAction != null) copyAction.performed += _ => editorManager.noteManager.CopySelection();
            if (pasteAction != null) pasteAction.performed += _ => editorManager.noteManager.PasteClipboard();
            if (deleteAction != null) deleteAction.performed += _ => editorManager.noteManager.DeleteSelection();
            if (quickSaveAction != null) quickSaveAction.performed += _ => editorManager.QuickSave();
        }

        public void Enable() => inputActions?.Enable();
        public void Disable() => inputActions?.Disable();

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
            if (addNoteAction.WasPressedThisFrame()) editorManager.noteManager.AddNoteAtMouse(MousePosition);
            else if (removeNoteAction.WasPressedThisFrame()) editorManager.noteManager.RemoveNoteAtMouse(MousePosition);
        }

        private void HandleSelectMode()
        {
            if (addNoteAction.WasPressedThisFrame() || removeNoteAction.WasPressedThisFrame())
            {
                editorManager.noteManager.ToggleSelectionAtMouse(MousePosition);
            }
        }

        private void HandleGimmickMode()
        {
            if (addNoteAction.WasPressedThisFrame()) 
                editorManager.noteManager.AddGimmickAtMouse(MousePosition, editorManager.currentSelectedGimmickType);
            else if (removeNoteAction.WasPressedThisFrame()) 
                editorManager.noteManager.RemoveGimmickAtMouse(MousePosition);
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
