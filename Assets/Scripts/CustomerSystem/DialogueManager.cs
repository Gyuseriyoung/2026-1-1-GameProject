using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CookingGame
{
    public class DialogueManager : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject dialoguePanel;
        public TextMeshProUGUI dialogueText;

        [Header("Timing")]
        public float typeSpeed = 0.05f;

        [Header("Audio")]
        public AudioClip defaultTypingSound;

        private string[] currentLines;
        private int currentLineIndex;
        private bool isTyping;
        private string currentFullText = "";
        private Coroutine typingCoroutine;
        
        private Action onDialogueComplete;

        private void Update()
        {
            if (!IsVisible() || !WasAdvancePressed()) return;
            HandleAdvanceInput();
        }

        public void PlayDialogue(string[] lines, Action onComplete)
        {
            if (lines == null || lines.Length == 0)
            {
                onComplete?.Invoke();
                return;
            }

            currentLines = lines;
            currentLineIndex = 0;
            onDialogueComplete = onComplete;
            SetVisible(true);
            ShowCurrentLine();
        }

        public void PlayDialogue(string line, Action onComplete)
        {
            PlayDialogue(new string[] { line }, onComplete);
        }

        private void ShowCurrentLine()
        {
            if (currentLineIndex >= currentLines.Length)
            {
                SetVisible(false);
                onDialogueComplete?.Invoke();
                return;
            }

            StartTyping(currentLines[currentLineIndex]);
        }

        private void HandleAdvanceInput()
        {
            if (isTyping)
            {
                CompleteTypingImmediately();
            }
            else
            {
                currentLineIndex++;
                ShowCurrentLine();
            }
        }

        private void StartTyping(string text)
        {
            StopTypingCoroutine();
            currentFullText = text;
            typingCoroutine = StartCoroutine(TypeDialogueCoroutine(text));
        }

        private IEnumerator TypeDialogueCoroutine(string text)
        {
            isTyping = true;
            if (dialogueText != null) dialogueText.text = "";

            foreach (char c in text.ToCharArray())
            {
                if (dialogueText != null) dialogueText.text += c;
                
                if (!char.IsWhiteSpace(c) && defaultTypingSound != null && AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX(defaultTypingSound);
                }
                
                yield return new WaitForSeconds(typeSpeed);
            }

            isTyping = false;
            typingCoroutine = null;
        }

        private void CompleteTypingImmediately()
        {
            StopTypingCoroutine();
            if (dialogueText != null) dialogueText.text = currentFullText;
            isTyping = false;
        }

        private void StopTypingCoroutine()
        {
            if (typingCoroutine == null) return;
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        public bool IsVisible()
        {
            return dialoguePanel != null && dialoguePanel.activeSelf;
        }

        public void SetVisible(bool visible)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(visible);
        }

        private bool WasAdvancePressed()
        {
            var keyboard = Keyboard.current;
            return keyboard != null
                   && (keyboard.spaceKey.wasPressedThisFrame
                       || keyboard.enterKey.wasPressedThisFrame
                       || keyboard.numpadEnterKey.wasPressedThisFrame);
        }
    }
}