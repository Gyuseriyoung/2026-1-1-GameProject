using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CookingGame
{
    public class OrderManager : MonoBehaviour
    {
        [Header("Scene Names")]
        public string gameplaySceneName = "Game Debug Scene";
        public string dialogueSceneName = "Customer Debug Scene";

        [Header("Initial Stage (For Debug)")]
        public StageData debugStage;

        [Header("UI References")]
        public GameObject dialoguePanel;
        public Image customerPortrait;
        public Animator customerAnimator; // Added for animator override support
        public TextMeshProUGUI customerNameText;
        public TextMeshProUGUI dialogueText;
        
        [Header("Timing")]
        public float arrivalDelay = 1.5f;
        public float typeSpeed = 0.05f;

        private int currentDialogueIndex = 0;
        private bool isShowingResult = false;
        private bool lastSuccess = false;
        private bool isTyping = false;
        private string currentFullText = "";
        private Coroutine typingCoroutine;

        private void Start()
        {
            if (CookingSession.CurrentStage == null && debugStage != null)
            {
                CookingSession.StartSession(debugStage);
            }

            if (CookingSession.CurrentStage != null)
            {
                if (CookingSession.IsReturningFromResult)
                {
                    ShowResultDialogue(CookingSession.LastGameSuccess);
                }
                else
                {
                    StartCoroutine(LoadCustomerWithDelay());
                }
            }
        }

        private void Update()
        {
            if (dialoguePanel != null && dialoguePanel.activeSelf)
            {
                if (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame || 
                    UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.numpadEnterKey.wasPressedThisFrame)
                {
                    if (isTyping)
                    {
                        CompleteTypingImmediately();
                    }
                    else if (isShowingResult)
                    {
                        AdvanceAfterResult();
                    }
                    else
                    {
                        OnNextDialogue();
                    }
                }
            }
        }

        private IEnumerator LoadCustomerWithDelay()
        {
            isShowingResult = false;
            if (customerPortrait != null) customerPortrait.gameObject.SetActive(false);
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            yield return new WaitForSeconds(arrivalDelay);
            LoadCustomer();
        }

        private void LoadCustomer()
        {
            if (CookingSession.CurrentCustomerIndex < CookingSession.CurrentStage.customerQueue.Count)
            {
                CookingSession.CurrentCustomer = CookingSession.CurrentStage.customerQueue[CookingSession.CurrentCustomerIndex];
                if (customerPortrait != null) customerPortrait.gameObject.SetActive(true);
                ShowOpeningDialogue();
            }
            else
            {
                Debug.Log("Stage Complete!");
                CookingSession.Clear();
                SceneTransitionManager.Instance.LoadScene("TitleScene");
            }
        }

        private void ShowOpeningDialogue()
        {
            currentDialogueIndex = 0;
            isShowingResult = false;
            dialoguePanel.SetActive(true);
            
            if (customerPortrait != null) customerPortrait.sprite = CookingSession.CurrentCustomer.portrait;
            if (customerAnimator != null)
            {
                if (CookingSession.CurrentCustomer.animatorOverride != null)
                    customerAnimator.runtimeAnimatorController = CookingSession.CurrentCustomer.animatorOverride;
                
                customerAnimator.Play("Customer_Idle");
            }
            if (customerNameText != null) customerNameText.text = CookingSession.CurrentCustomer.customerName;

            DisplayDialogue();
        }

        private void DisplayDialogue()
        {
            var customer = CookingSession.CurrentCustomer;
            if (customer.openingDialogues != null && currentDialogueIndex < customer.openingDialogues.Length)
            {
                StartTyping(customer.openingDialogues[currentDialogueIndex]);
            }
        }

        private void StartTyping(string text)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            currentFullText = text;
            typingCoroutine = StartCoroutine(TypeDialogueCoroutine(text));
        }

        private IEnumerator TypeDialogueCoroutine(string text)
        {
            isTyping = true;
            dialogueText.text = "";
            foreach (char c in text.ToCharArray())
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(typeSpeed);
            }
            isTyping = false;
            typingCoroutine = null;
        }

        private void CompleteTypingImmediately()
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = currentFullText;
            isTyping = false;
            typingCoroutine = null;
        }

        public void OnNextDialogue()
        {
            currentDialogueIndex++;
            if (currentDialogueIndex < CookingSession.CurrentCustomer.openingDialogues.Length)
            {
                DisplayDialogue();
            }
            else
            {
                StartGameplay();
            }
        }

        private void StartGameplay()
        {
            SceneTransitionManager.Instance.LoadScene(gameplaySceneName);
        }

        private void ShowResultDialogue(bool success)
        {
            CookingSession.IsReturningFromResult = false;
            isShowingResult = true;
            lastSuccess = success;
            
            dialoguePanel.SetActive(true);
            var customer = CookingSession.CurrentCustomer;
            
            if (customerPortrait != null) customerPortrait.sprite = customer.portrait;
            if (customerAnimator != null)
            {
                if (customer.animatorOverride != null)
                    customerAnimator.runtimeAnimatorController = customer.animatorOverride;
                
                customerAnimator.Play(success ? "Customer_Success" : "Customer_Fail");
            }
            if (customerNameText != null) customerNameText.text = customer.customerName;
            
            string resultText = success ? customer.successDialogue : customer.failureDialogue;
            StartTyping(resultText);
        }

        private void AdvanceAfterResult()
        {
            CookingSession.CurrentCustomerIndex++;
            StartCoroutine(LoadCustomerWithDelay());
        }
    }
}
