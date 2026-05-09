using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RhythmSystem
{
    public class RhythmGameManager : MonoBehaviour
    {
        [Header("Data")]
        public ChartData currentChart;
        public string chartFileName;

        [Header("Audio")]
        public AudioSource audioSource;
        private double gameStartTime;

        [Header("Gameplay Settings")]
        public float scrollSpeed = 500f; 
        public float hitPointX = 800f; 
        public float perfectWindow = 0.05f; 

        [Header("Prefabs & Container")]
        public GameObject notePrefab;
        public Transform noteContainer;

        private List<NoteData> pendingNotes = new List<NoteData>();
        private int pendingNoteIndex = 0;
        private List<NoteController> activeNotes = new List<NoteController>();
        private Dictionary<int, float> laneCurrentY = new Dictionary<int, float>();

        void Start()
        {
            LoadChart();
            InitializeLanes();
            StartGame();
        }

        private void LoadChart()
        {
            currentChart = ChartIO.LoadFromFile(chartFileName);
            if (currentChart != null)
            {
                pendingNotes = currentChart.notes.OrderBy(n => n.time).ToList();
                scrollSpeed = currentChart.globalScrollSpeed;
            }
        }

        private void InitializeLanes()
        {
            if (currentChart == null) return;
            foreach (var lane in currentChart.lanes)
            {
                laneCurrentY[lane.laneIndex] = lane.defaultY;
            }
        }

        private void StartGame()
        {
            float startOffset = currentChart != null ? currentChart.startOffset / 1000f : 2.0f;
            gameStartTime = AudioSettings.dspTime + startOffset;
            audioSource.PlayScheduled(gameStartTime);
        }

        void Update()
        {
            if (AudioSettings.dspTime < gameStartTime && !audioSource.isPlaying) return;

            float currentTime = (float)(AudioSettings.dspTime - gameStartTime);
            
            ProcessNoteSpawning(currentTime);
            MoveActiveNotes(currentTime);
            HandleInput(currentTime);
            ProcessGimmicks(currentTime);
        }

        private void ProcessNoteSpawning(float currentTime)
        {
            float spawnAheadTime = 2.0f;
            while (pendingNoteIndex < pendingNotes.Count && (pendingNotes[pendingNoteIndex].time / 1000f) <= currentTime + spawnAheadTime)
            {
                SpawnNote(pendingNotes[pendingNoteIndex]);
                pendingNoteIndex++;
            }
        }

        private void SpawnNote(NoteData data)
        {
            GameObject obj = Instantiate(notePrefab, noteContainer);
            NoteController controller = obj.GetComponent<NoteController>();
            controller.data = data;
            activeNotes.Add(controller);
        }

        private void MoveActiveNotes(float currentTime)
        {
            for (int i = activeNotes.Count - 1; i >= 0; i--)
            {
                NoteController note = activeNotes[i];
                float noteTime = note.data.time / 1000f;
                float xPos = hitPointX - (noteTime - currentTime) * scrollSpeed;
                float yPos = laneCurrentY.ContainsKey(note.data.laneIndex) ? laneCurrentY[note.data.laneIndex] : 0;
                
                note.transform.localPosition = new Vector3(xPos, yPos, 0);

                if (currentTime > noteTime + perfectWindow)
                {
                    HandleMiss(note, i);
                }
            }
        }

        private void HandleMiss(NoteController note, int index)
        {
            Debug.Log("Miss!");
            Destroy(note.gameObject);
            activeNotes.RemoveAt(index);
        }

        private void HandleInput(float currentTime)
        {
            if (currentChart == null) return;
            foreach (var lane in currentChart.lanes)
            {
                if (Input.GetKeyDown(lane.keyBinding))
                {
                    CheckHit(lane.laneIndex, currentTime);
                }
            }
        }

        private void CheckHit(int laneIndex, float currentTime)
        {
            var targetNote = activeNotes
                .Where(n => n.data.laneIndex == laneIndex)
                .OrderBy(n => Mathf.Abs(currentTime - (n.data.time / 1000f)))
                .FirstOrDefault();

            if (targetNote != null)
            {
                float diff = Mathf.Abs(currentTime - (targetNote.data.time / 1000f));
                if (diff <= perfectWindow)
                {
                    Debug.Log("Perfect!");
                    activeNotes.Remove(targetNote);
                    Destroy(targetNote.gameObject);
                }
            }
        }

        private void ProcessGimmicks(float currentTime)
        {
            if (currentChart == null) return;
            foreach (var gimmick in currentChart.gimmicks)
            {
                if (currentTime >= gimmick.time / 1000f)
                {
                    if (gimmick.type == GimmickType.LaneMove)
                    {
                        laneCurrentY[gimmick.targetLane] = gimmick.value;
                    }
                }
            }
        }
    }
}
