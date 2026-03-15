// GameOverManager.cs
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public GameObject gameOverPanel;

    public void ShowGameOver(PlayerAnimationController animController = null)
    {
        StartCoroutine(GameOverRoutine(animController));
    }

    private IEnumerator GameOverRoutine(PlayerAnimationController animController)
    {
        if (animController != null)
        {
            animController.PlayDeath();
            yield return new WaitForSeconds(1f); // ölüm animasyonu süresi
        }

        Time.timeScale = 0f;
        gameOverPanel.SetActive(true);
    }

    public void HideGameOver()
    {
        Time.timeScale = 1f;
        gameOverPanel.SetActive(false);
    }

    public void TryAgain()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}