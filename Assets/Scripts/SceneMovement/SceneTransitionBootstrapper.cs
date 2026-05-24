using UnityEngine;

/// <summary>
/// 게임 시작 시 SceneTransitionManager를 자동으로 생성합니다.
/// 어떤 씬에도 별도 설정 없이 즉시 동작합니다.
/// </summary>
public static class SceneTransitionBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (SceneTransitionManager.Instance != null) return;

        var go = new GameObject("[SceneTransitionManager]");
        go.AddComponent<SceneTransitionManager>();
    }
}
