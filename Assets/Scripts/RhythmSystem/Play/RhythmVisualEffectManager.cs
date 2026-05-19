using UnityEngine;
using TMPro;

namespace RhythmSystem.Play
{
    public class RhythmVisualEffectManager : MonoBehaviour
    {
        public TMP_Text judgmentText;
        public TMP_Text comboText;
        public MergeManager mergeManager;

        public void Initialize()
        {
            RhythmEvents.OnNoteHit += OnNoteHit;
            RhythmEvents.OnNoteMiss += OnNoteMiss;
            RhythmEvents.OnGameStart += ClearUI;
        }

        private void OnDestroy()
        {
            RhythmEvents.OnNoteHit -= OnNoteHit;
            RhythmEvents.OnNoteMiss -= OnNoteMiss;
            RhythmEvents.OnGameStart -= ClearUI;
        }

        private void OnNoteHit(NoteHitEventArgs args)
        {
            ShowJudgment(args.rating.ToString());
            UpdateCombo(args.combo);
            
            if (mergeManager != null)
            {
                mergeManager.CreateMergeObject(args.note.Data.mergeType, args.note.Data.objectIndex);
            }
        }

        private void OnNoteMiss(NoteMissEventArgs args)
        {
            ShowJudgment("Miss");
            UpdateCombo(args.combo);
        }

        private void ShowJudgment(string rating)
        {
            if (judgmentText != null)
            {
                judgmentText.text = rating;
                if (rating.Contains("Perfect")) judgmentText.color = Color.yellow;
                else if (rating.Contains("Great")) judgmentText.color = Color.green;
                else if (rating.Contains("Good")) judgmentText.color = Color.cyan;
                else judgmentText.color = Color.red;
            }
        }

        private void UpdateCombo(int combo)
        {
            if (comboText != null)
            {
                comboText.text = combo > 0 ? $"{combo}" : "";
            }
        }

        private void ClearUI()
        {
            if (judgmentText != null) judgmentText.text = "";
            if (comboText != null) comboText.text = "";
        }
    }
}
