using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RhythmSystem
{
    public class EditorNoteManager : MonoBehaviour
    {
        private EditorManager editorManager;

        public GameObject notePrefab;
        public GameObject gimmickMarkerPrefab;

        private List<EditorNoteController> activeNotes = new List<EditorNoteController>();
        private List<EditorGimmickController> activeGimmicks = new List<EditorGimmickController>();
        private HashSet<NoteData> selectedNotes = new HashSet<NoteData>();
        private GimmickEvent selectedGimmick;
        private List<NoteData> clipboard = new List<NoteData>();

        public HashSet<NoteData> SelectedNotes => selectedNotes;
        public GimmickEvent SelectedGimmick => selectedGimmick;

        private NoteData creatingHoldNote;
        private EditorNoteController creatingHoldVisual;

        public void Init(EditorManager manager)
        {
            editorManager = manager;
        }

        public void UpdateNoteVisuals()
        {
            foreach (var note in activeNotes) Destroy(note.gameObject);
            activeNotes.Clear();

            foreach (var noteData in editorManager.currentChart.notes)
            {
                SpawnNoteVisual(noteData);
            }
        }

        public void SpawnNoteVisual(NoteData note)
        {
            if (notePrefab == null || editorManager.timelineContent == null) return;

            GameObject noteObj = Instantiate(notePrefab, editorManager.timelineContent);
            EditorNoteController controller = noteObj.GetComponent<EditorNoteController>();
            
            if (controller == null)
                controller = noteObj.AddComponent<EditorNoteController>();

            controller.data = note;
            controller.SetSelection(selectedNotes.Contains(note));
            controller.ApplyMergeSprite(editorManager.mergeObjectData);

            RectTransform rt = noteObj.GetComponent<RectTransform>();
            float yPos = editorManager.timelineManager.GetLaneYAt(note.laneIndex, note.time);
            
            rt.anchoredPosition = new Vector2(-(note.time / 1000f) * editorManager.currentScrollSpeed, yPos);
            
            activeNotes.Add(controller);
        }

        public void UpdateGimmickVisuals()
        {
            foreach (var gm in activeGimmicks) Destroy(gm.gameObject);
            activeGimmicks.Clear();
            
            var grouped = editorManager.currentChart.gimmicks
                .GroupBy(g => new { Time = Mathf.RoundToInt(g.time), Lane = g.targetLane })
                .ToList();

            foreach (var group in grouped)
            {
                int subIndex = 0;
                foreach (var gimmick in group)
                {
                    if (gimmickMarkerPrefab == null) break;
                    GameObject gmObj = Instantiate(gimmickMarkerPrefab, editorManager.timelineContent);
                    EditorGimmickController controller = gmObj.GetComponent<EditorGimmickController>();
                    if (controller == null) controller = gmObj.AddComponent<EditorGimmickController>();

                    controller.data = gimmick;
                    
                    float xPos = -(gimmick.time / 1000f) * editorManager.currentScrollSpeed;
                    float baseLaneY = editorManager.timelineManager.GetLaneYAt(gimmick.targetLane, gimmick.time);
                    
                    float yOffset = subIndex * 30f; 
                    gmObj.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, baseLaneY + yOffset);
                    
                    controller.SetSelection(selectedGimmick == gimmick);
                    activeGimmicks.Add(controller);
                    subIndex++;
                }
            }
        }

        public void ToggleGimmickSelectionAtMouse(Vector2 mousePos)
        {
            float timeMs = editorManager.timelineManager.GetTimeFromMouse(mousePos) * 1000f;
            int lane = editorManager.timelineManager.GetLaneFromMouse(mousePos);

            var gimmick = editorManager.currentChart.gimmicks
                .Where(g => Mathf.Abs(g.time - timeMs) < 100f && g.targetLane == lane)
                .OrderBy(g => Mathf.Abs(g.time - timeMs))
                .FirstOrDefault();

            SelectGimmick(gimmick);
        }

        public void SelectGimmick(GimmickEvent gimmick)
        {
            selectedGimmick = gimmick;
            if (selectedGimmick != null)
            {
                editorManager.editorUIController.LoadGimmickData(selectedGimmick);
            }
            UpdateGimmickVisuals();
        }

        public void UpdateSelectedGimmickValue(float val)
        {
            if (selectedGimmick != null)
            {
                selectedGimmick.value = val;
                
                // If BPM change, also update timing points
                if (selectedGimmick.type == GimmickType.BPMChange)
                {
                    var existingTP = editorManager.currentChart.timingPoints.Find(tp => Mathf.Abs(tp.time - selectedGimmick.time) < 0.5f);
                    if (existingTP != null) existingTP.bpm = val;
                }
                
                // Update visuals for the specific gimmick
                var controller = activeGimmicks.FirstOrDefault(g => g.data == selectedGimmick);
                if (controller != null) controller.UpdateVisuals();
            }
        }

        public void AddGimmickAtMouse(Vector2 mousePos, GimmickType type)
        {
            float time = editorManager.timelineManager.GetTimeFromMouse(mousePos);
            float snappedTimeMs = editorManager.GetSnappedTime(time) * 1000f;
            int targetLane = editorManager.timelineManager.GetLaneFromMouse(mousePos);

            float val = 0;
            if (editorManager.editorUIController.gimmickValueInput != null)
            {
                float.TryParse(editorManager.editorUIController.gimmickValueInput.text, out val);
            }

            if (type == GimmickType.LaneAdd || type == GimmickType.LaneRemove)
            {
                if (val != 0) targetLane = (int)val;
            }

            if (editorManager.currentChart.gimmicks.Any(g => 
                Mathf.Abs(g.time - snappedTimeMs) < 0.5f && 
                g.type == type && 
                g.targetLane == targetLane)) return;

            if (type == GimmickType.BPMChange)
            {
                var existingTP = editorManager.currentChart.timingPoints.Find(tp => Mathf.Abs(tp.time - snappedTimeMs) < 0.5f);
                if (existingTP != null) existingTP.bpm = val;
                else editorManager.currentChart.timingPoints.Add(new TimingPoint { time = snappedTimeMs, bpm = val, meter = 4 });
            }

            GimmickEvent newGimmick = new GimmickEvent
            {
                time = snappedTimeMs,
                type = type,
                targetLane = targetLane,
                value = val 
            };
            editorManager.currentChart.gimmicks.Add(newGimmick);
            SelectGimmick(newGimmick);
            editorManager.RefreshAllVisuals();
        }

        public void RemoveGimmickAtMouse(Vector2 mousePos)
        {
            float timeMs = editorManager.timelineManager.GetTimeFromMouse(mousePos) * 1000f;
            int targetLane = editorManager.timelineManager.GetLaneFromMouse(mousePos);

            var gimmick = editorManager.currentChart.gimmicks
                .Where(g => Mathf.Abs(g.time - timeMs) < 100f)
                .OrderBy(g => (g.targetLane == targetLane ? 0 : 1000) + Mathf.Abs(g.time - timeMs))
                .FirstOrDefault();

            if (gimmick != null)
            {
                if (gimmick.type == GimmickType.BPMChange)
                {
                    editorManager.currentChart.timingPoints.RemoveAll(tp => Mathf.Abs(tp.time - gimmick.time) < 0.5f && tp.time > 0);
                }
                
                editorManager.currentChart.gimmicks.Remove(gimmick);
                editorManager.RefreshAllVisuals();
            }
        }

        public void StartHoldNoteCreation(Vector2 mousePos)
        {
            float time = editorManager.timelineManager.GetTimeFromMouse(mousePos);
            float snappedTimeMs = editorManager.GetSnappedTime(time) * 1000f;
            int laneIndex = editorManager.timelineManager.GetLaneFromMouse(mousePos);

            if (laneIndex < 0) return;

            creatingHoldNote = new NoteData 
            { 
                time = snappedTimeMs, 
                laneIndex = laneIndex, 
                type = NoteType.Hold,
                length = 0,
                mergeType = editorManager.currentSelectedMergeType,
                objectIndex = editorManager.currentSelectedMergeIndex
            };

            // Temporary visual
            GameObject noteObj = Instantiate(notePrefab, editorManager.timelineContent);
            creatingHoldVisual = noteObj.GetComponent<EditorNoteController>();
            if (creatingHoldVisual == null) creatingHoldVisual = noteObj.AddComponent<EditorNoteController>();
            
            creatingHoldVisual.data = creatingHoldNote;
            creatingHoldVisual.ApplyMergeSprite(editorManager.mergeObjectData);
            
            UpdateHoldNoteCreation(mousePos);
        }

        public void UpdateHoldNoteCreation(Vector2 mousePos)
        {
            if (creatingHoldNote == null) return;

            float time = editorManager.timelineManager.GetTimeFromMouse(mousePos);
            float snappedTimeMs = editorManager.GetSnappedTime(time) * 1000f;
            
            creatingHoldNote.length = Mathf.Max(0, snappedTimeMs - creatingHoldNote.time);

            RectTransform rt = creatingHoldVisual.GetComponent<RectTransform>();
            float yPos = editorManager.timelineManager.GetLaneYAt(creatingHoldNote.laneIndex, creatingHoldNote.time);
            rt.anchoredPosition = new Vector2(-(creatingHoldNote.time / 1000f) * editorManager.currentScrollSpeed, yPos);
            
            creatingHoldVisual.UpdateVisuals();
        }

        public void FinalizeHoldNoteCreation()
        {
            if (creatingHoldNote == null) return;

            if (creatingHoldNote.length > 0)
            {
                editorManager.currentChart.notes.Add(creatingHoldNote);
                activeNotes.Add(creatingHoldVisual);
            }
            else
            {
                Destroy(creatingHoldVisual.gameObject);
            }

            creatingHoldNote = null;
            creatingHoldVisual = null;
        }

        public void AddNoteAtMouse(Vector2 mousePos)
        {
            if (editorManager.currentSelectedNoteType == NoteType.Hold)
            {
                StartHoldNoteCreation(mousePos);
                return;
            }

            float time = editorManager.timelineManager.GetTimeFromMouse(mousePos);
            float snappedTimeMs = editorManager.GetSnappedTime(time) * 1000f;
            int laneIndex = editorManager.timelineManager.GetLaneFromMouse(mousePos);

            if (laneIndex < 0) return; 

            if (editorManager.currentChart.notes.Any(n => Mathf.Abs(n.time - snappedTimeMs) < 0.5f && n.laneIndex == laneIndex)) return;

            NoteData newNote = new NoteData 
            { 
                time = snappedTimeMs, 
                laneIndex = laneIndex, 
                type = NoteType.Tap,
                mergeType = editorManager.currentSelectedMergeType,
                objectIndex = editorManager.currentSelectedMergeIndex
            };
            editorManager.currentChart.notes.Add(newNote);
            SpawnNoteVisual(newNote);
        }

        public void RemoveNoteAtMouse(Vector2 mousePos)
        {
            var note = GetNoteAtMouse(mousePos);
            if (note != null)
            {
                editorManager.currentChart.notes.Remove(note);
                selectedNotes.Remove(note);
                UpdateNoteVisuals();
            }
        }

        public void ToggleSelectionAtMouse(Vector2 mousePos)
        {
            var note = GetNoteAtMouse(mousePos);
            if (note != null)
            {
                if (selectedNotes.Contains(note)) selectedNotes.Remove(note);
                else selectedNotes.Add(note);
                
                activeNotes.FirstOrDefault(c => c.data == note)?.SetSelection(selectedNotes.Contains(note));
            }
            else ClearSelection();
        }

        public NoteData GetNoteAtMouse(Vector2 mousePos)
        {
            float timeMs = editorManager.timelineManager.GetTimeFromMouse(mousePos) * 1000f;
            int lane = editorManager.timelineManager.GetLaneFromMouse(mousePos);

            return editorManager.currentChart.notes
                .Where(n => n.laneIndex == lane && Mathf.Abs(n.time - timeMs) < 50f)
                .OrderBy(n => Mathf.Abs(n.time - timeMs))
                .FirstOrDefault();
        }

        public void ClearSelection()
        {
            selectedNotes.Clear();
            foreach (var note in activeNotes) note.SetSelection(false);
        }

        public void CopySelection()
        {
            if (selectedNotes.Count == 0) return;
            float minTime = selectedNotes.Min(n => n.time);
            clipboard = selectedNotes.Select(n => new NoteData {
                time = n.time - minTime, laneIndex = n.laneIndex, type = n.type, length = n.length,
                mergeType = n.mergeType, objectIndex = n.objectIndex
            }).ToList();
        }

        public void PasteClipboard()
        {
            if (clipboard.Count == 0) return;
            ClearSelection();
            float pasteBaseTimeMs = editorManager.EditorTime * 1000f;
            foreach (var clipNote in clipboard)
            {
                NoteData newNote = new NoteData {
                    time = pasteBaseTimeMs + clipNote.time, laneIndex = clipNote.laneIndex, type = clipNote.type, length = clipNote.length,
                    mergeType = clipNote.mergeType, objectIndex = clipNote.objectIndex
                };
                editorManager.currentChart.notes.Add(newNote);
                selectedNotes.Add(newNote);
            }
            UpdateNoteVisuals();
        }

        public void DeleteSelection()
        {
            editorManager.currentChart.notes.RemoveAll(n => selectedNotes.Contains(n));
            selectedNotes.Clear();
            UpdateNoteVisuals();
        }
    }
}
