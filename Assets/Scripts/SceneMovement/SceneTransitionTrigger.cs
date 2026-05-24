using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 버튼이나 오브젝트에 붙여서 Sweet Wipe 씬 전환을 트리거합니다.
/// </summary>
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("씬 설정")]
    [Tooltip("이동할 씬 이름 (비어있으면 sceneIndex 사용)")]
    public string targetSceneName = "";

    [Tooltip("이동할 씬 인덱스")]
    public int targetSceneIndex = -1;

    [Header("자동 연결")]
    [Tooltip("같은 오브젝트의 Button에 자동으로 연결합니다")]
    public bool autoConnectButton = true;

    private void Start()
    {
        if (!autoConnectButton) return;
        var btn = GetComponent<Button>();
        if (btn != null) btn.onClick.AddListener(Trigger);
    }

    public void Trigger()
    {
        var mgr = SceneTransitionManager.Instance;
        if (mgr == null) { Debug.LogWarning("[SceneTransition] Manager not found!"); return; }

        if (!string.IsNullOrEmpty(targetSceneName))
            mgr.LoadScene(targetSceneName);
        else if (targetSceneIndex >= 0)
            mgr.LoadScene(targetSceneIndex);
        else
            Debug.LogWarning("[SceneTransitionTrigger] 씬 이름 또는 인덱스를 설정해주세요.");
    }

    public void TriggerNext()     => SceneTransitionManager.Instance?.LoadNextScene();
    public void TriggerPrevious() => SceneTransitionManager.Instance?.LoadPreviousScene();
}
