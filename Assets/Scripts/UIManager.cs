using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public string nextStage;
    public GameObject StagePannel;

    public void MoveToNextStage()
    {
        SceneManager.LoadScene(nextStage);
    }

    public void GameStart()
    {
        SceneManager.LoadScene("Stage_1");
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
