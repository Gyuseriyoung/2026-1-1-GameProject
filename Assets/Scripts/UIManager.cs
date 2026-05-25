using Unity.VectorGraphics;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public string nextStage;
    public GameObject StagePannel;

    public void MoveToNextStage()
    {
        SceneTransitionManager.Instance.LoadScene(nextStage);
    }

    public void GameStart()
    {
        SceneTransitionManager.Instance.LoadScene(nextStage);
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenStage()
    {
        StagePannel.SetActive(true);
    }

    public void CloseStage()
    {
        StagePannel.SetActive(false);
    }
}
