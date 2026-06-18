using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingGame
{
    public class OrderManager : MonoBehaviour
    {
        [Header("Scene Names")]
        public string gameplaySceneName = "Game Debug Scene";
        public string dialogueSceneName = "Customer Debug Scene";

        [Header("Initial Stage (For Debug)")]
        public StageData debugStage;

        [Header("References")]
        public DialogueManager dialogueManager;

        [Header("UI References")]
        public Image customerPortrait;
        public Image backgroundImage;
        public Animator customerAnimator;
        public TextMeshProUGUI customerNameText;

        private Vector2 originalPortraitPosition;

        [Header("Timing")]
        public float arrivalDelay = 1.5f;

        private void Start()
        {
            EnsureSession();

            if (CookingSession.CurrentStage == null) return;

            if (dialogueManager == null) dialogueManager = GetComponent<DialogueManager>();

            if (customerPortrait != null)
            {
                originalPortraitPosition = customerPortrait.rectTransform.anchoredPosition;
            }

            StartSequence();
        }

        private void StartSequence()
        {
            if (CookingSession.IsReturningFromResult)
            {
                ShowResultDialogue(CookingSession.LastGameSuccess);
            }
            else if (CookingSession.CurrentCustomerIndex == 0 && 
                     CookingSession.CurrentStage.introDialogues != null && 
                     CookingSession.CurrentStage.introDialogues.Length > 0)
            {
                ShowStageIntroDialogue();
            }
            else
            {
                StartCoroutine(LoadCustomerWithDelay());
            }
        }

        private void EnsureSession()
        {
            if (CookingSession.CurrentStage == null && debugStage != null)
            {
                CookingSession.StartSession(debugStage);
            }
        }

        private void ShowStageIntroDialogue()
        {
            SetPortraitVisible(false); // Player is text only
            if (customerNameText != null) customerNameText.text = "나";
            dialogueManager.PlayDialogue(CookingSession.CurrentStage.introDialogues, () => {
                StartCoroutine(LoadCustomerWithDelay());
            });
        }

        private IEnumerator LoadCustomerWithDelay()
        {
            SetPortraitVisible(false);
            if (dialogueManager != null) dialogueManager.SetVisible(false);

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
            SetPortraitVisible(true);
            BindCustomerView(CookingSession.CurrentCustomer, "Customer_Idle");

            StartCoroutine(SlideInPortrait(() => {
                dialogueManager.PlayDialogue(CookingSession.CurrentCustomer.openingDialogues, () => {
                    StartGameplay();
                });
            }));
        }

        private void ShowResultDialogue(bool success)
        {
            CookingSession.IsReturningFromResult = false;
            SetPortraitVisible(true);

            var customer = CookingSession.CurrentCustomer;
            BindCustomerView(customer, success ? "Customer_Success" : "Customer_Fail");
            
            string line = success ? customer.successDialogue : customer.failureDialogue;
            
            StartCoroutine(SlideInPortrait(() => {
                dialogueManager.PlayDialogue(line, () => {
                    CookingSession.CurrentCustomerIndex++;
                    
                    if (customer != null && !IsDialogueEmpty(customer.soliloquies))
                    {
                        SetPortraitVisible(false);
                        if (customerNameText != null) customerNameText.text = "나";
                        
                        dialogueManager.PlayDialogue(customer.soliloquies, () => {
                            StartCoroutine(LoadCustomerWithDelay());
                        });
                    }
                    else
                    {
                        StartCoroutine(LoadCustomerWithDelay());
                    }
                });
            }));
        }

        private IEnumerator SlideInPortrait(System.Action onComplete)
        {
            if (customerPortrait == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            float duration = 0.5f;
            float elapsed = 0f;
            Vector2 startPos = new Vector2(originalPortraitPosition.x, originalPortraitPosition.y - 800f);
            Vector2 endPos = originalPortraitPosition;

            customerPortrait.rectTransform.anchoredPosition = startPos;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easeT = 1f - Mathf.Pow(1f - t, 3f); // Cubic Ease Out
                customerPortrait.rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, easeT);
                yield return null;
            }

            customerPortrait.rectTransform.anchoredPosition = endPos;
            onComplete?.Invoke();
        }

        private bool IsDialogueEmpty(string[] lines)
        {
            if (lines == null || lines.Length == 0) return true;
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line)) return false;
            }
            return true;
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
            if (backgroundImage != null) backgroundImage.sprite = customer.backgroundImage;
            if (customerNameText != null) customerNameText.text = customer.customerName;

            if (customerAnimator == null) return;

            if (customer.animatorOverride != null)
            {
                customerAnimator.runtimeAnimatorController = customer.animatorOverride;
            }

            customerAnimator.Play(animationName);
        }

        private void SetPortraitVisible(bool visible)
        {
            if (customerPortrait != null) customerPortrait.gameObject.SetActive(visible);
        }
    }
}
