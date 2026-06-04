using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CookingGame
{
    public class OrderManager : MonoBehaviour
    {
        private enum DialogueState
        {
            WaitingArrival,
            Opening,
            Result
        }

        [Header("Scene Names")]
        public string gameplaySceneName = "Game Debug Scene";
        public string dialogueSceneName = "Customer Debug Scene";

        [Header("Initial Stage (For Debug)")]
        public StageData debugStage;

        [Header("UI References")]
        public GameObject dialoguePanel;
        public Image customerPortrait;
        public Animator customerAnimator;
        public TextMeshProUGUI customerNameText;
        public TextMeshProUGUI dialogueText;

        [Header("Timing")]
        public float arrivalDelay = 1.5f;
        public float typeSpeed = 0.05f;

        private DialogueState state = DialogueState.WaitingArrival;
        private int currentDialogueIndex;
        private bool isTyping;
        private string currentFullText = "";
        private Coroutine typingCoroutine;

        private void Start()
        {
            EnsureSession();

            if (CookingSession.CurrentStage == null) return;

            if (CookingSession.IsReturningFromResult)
            {
                ShowResultDialogue(CookingSession.LastGameSuccess);
            }
            else
            {
                StartCoroutine(LoadCustomerWithDelay());
            }
        }

        private void Update()
        {
            if (!IsDialogueVisible() || !WasAdvancePressed()) return;
            HandleAdvanceInput();
        }

        private void EnsureSession()
        {
            if (CookingSession.CurrentStage == null && debugStage != null)
            {
                CookingSession.StartSession(debugStage);
            }
        }

        private IEnumerator LoadCustomerWithDelay()
        {
            state = DialogueState.WaitingArrival;
            SetDialogueVisible(false);
            SetPortraitVisible(false);

            yield return new WaitForSeconds(arrivalDelay);

            LoadCustomer();
        }

        private void LoadCustomer()
        {
            if (CookingSession.CurrentCustomerIndex >= CookingSession.CurrentStage.customerQueue.Count)
            {
                CompleteStage();
                return;
            }

            CookingSession.CurrentCustomer =
                CookingSession.CurrentStage.customerQueue[CookingSession.CurrentCustomerIndex];

            ShowOpeningDialogue();
        }

        private void ShowOpeningDialogue()
        {
            state = DialogueState.Opening;
            currentDialogueIndex = 0;

            SetDialogueVisible(true);
            SetPortraitVisible(true);
            BindCustomerView(CookingSession.CurrentCustomer, "Customer_Idle");
            ShowCurrentOpeningLine();
        }

        private void ShowCurrentOpeningLine()
        {
            var customer = CookingSession.CurrentCustomer;
            if (customer.openingDialogues == null || currentDialogueIndex >= customer.openingDialogues.Length)
            {
                StartGameplay();
                return;
            }

            StartTyping(customer.openingDialogues[currentDialogueIndex]);
        }

        private void ShowResultDialogue(bool success)
        {
            CookingSession.IsReturningFromResult = false;
            state = DialogueState.Result;

            SetDialogueVisible(true);
            SetPortraitVisible(true);

            var customer = CookingSession.CurrentCustomer;
            BindCustomerView(customer, success ? "Customer_Success" : "Customer_Fail");
            StartTyping(success ? customer.successDialogue : customer.failureDialogue);
        }

        private void HandleAdvanceInput()
        {
            if (isTyping)
            {
                CompleteTypingImmediately();
                return;
            }

            if (state == DialogueState.Result)
            {
                AdvanceAfterResult();
                return;
            }

            OnNextDialogue();
        }

        public void OnNextDialogue()
        {
            currentDialogueIndex++;
            ShowCurrentOpeningLine();
        }

        private void AdvanceAfterResult()
        {
            CookingSession.CurrentCustomerIndex++;
            StartCoroutine(LoadCustomerWithDelay());
        }

        private void StartGameplay()
        {
            SceneTransitionManager.Instance.LoadScene(gameplaySceneName);
        }

        private void CompleteStage()
        {
            Debug.Log("Stage Complete!");
            CookingSession.Clear();
            SceneTransitionManager.Instance.LoadScene("TitleScene");
        }

        private void BindCustomerView(CustomerData customer, string animationName)
        {
            if (customerPortrait != null) customerPortrait.sprite = customer.portrait;
            if (customerNameText != null) customerNameText.text = customer.customerName;

            if (customerAnimator == null) return;

            if (customer.animatorOverride != null)
            {
                customerAnimator.runtimeAnimatorController = customer.animatorOverride;
            }

            customerAnimator.Play(animationName);
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

        private bool IsDialogueVisible()
        {
            return dialoguePanel != null && dialoguePanel.activeSelf;
        }

        private void SetDialogueVisible(bool visible)
        {
            if (dialoguePanel != null) dialoguePanel.SetActive(visible);
        }

        private void SetPortraitVisible(bool visible)
        {
            if (customerPortrait != null) customerPortrait.gameObject.SetActive(visible);
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
