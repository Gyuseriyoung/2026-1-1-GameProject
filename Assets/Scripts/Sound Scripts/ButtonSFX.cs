using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// UI 요소를 클릭했을 때 효과음을 재생하는 단순한 컴포넌트입니다.
/// </summary>
public class ButtonSFX : MonoBehaviour, IPointerClickHandler
{
    [Header("클릭 효과음")]
    public AudioClip clip;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }
}
