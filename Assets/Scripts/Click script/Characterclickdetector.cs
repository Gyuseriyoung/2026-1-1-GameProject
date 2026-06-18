using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 오브젝트에 Button 컴포넌트 추가 후 이 스크립트 연결
/// Button의 OnClick()에 PanelSlider.Toggle 연결하거나
/// 아래처럼 자동 연결 방식 사용 가능
/// </summary>
public class CharacterClickDetector : MonoBehaviour
{
    [Tooltip("클릭 시 토글할 PanelSlider 연결")]
    public PanelSlider panelSlider;

    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(panelSlider.Toggle);
    }
}