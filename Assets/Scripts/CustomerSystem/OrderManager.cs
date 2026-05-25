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
        public TextMeshProUGUI customerNameText;
        public TextMeshProUGUI dialogueText;
        public Button nextButton;
        [Header("Timing")]
        public float arrivalDelay = 1.5f;

        private int currentDialogueIndex = 0;

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

        private IEnumerator LoadCustomerWithDelay()
        {
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
            dialoguePanel.SetActive(true);
            
            if (customerPortrait != null) customerPortrait.sprite = CookingSession.CurrentCustomer.portrait;
            if (customerNameText != null) customerNameText.text = CookingSession.CurrentCustomer.customerName;

            DisplayDialogue();
            
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextDialogue);
        }

        private void DisplayDialogue()
        {
            var customer = CookingSession.CurrentCustomer;
            if (customer.openingDialogues != null && currentDialogueIndex < customer.openingDialogues.Length)
            {
                dialogueText.text = customer.openingDialogues[currentDialogueIndex];
            }
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
            
            dialoguePanel.SetActive(true);
            var customer = CookingSession.CurrentCustomer;
            
            if (customerPortrait != null) customerPortrait.sprite = customer.portrait;
            if (customerNameText != null) customerNameText.text = customer.customerName;
            
            dialogueText.text = success ? customer.successDialogue : customer.failureDialogue;
            
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => {
                CookingSession.CurrentCustomerIndex++;
                StartCoroutine(LoadCustomerWithDelay());
            });
        }
    }
}
