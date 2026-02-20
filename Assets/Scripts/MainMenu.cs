using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField seedInput;
    [Header("Scenes")]
    public string gameSceneName = "SampleScene";

    public void OnClickStart()
    {
        string text = seedInput ? seedInput.text : "";
        int seed = SeedUtil.FromInput(text);

        if (!GameSession.I)
            new GameObject("GameSession").AddComponent<GameSession>();

        GameSession.I.SetSeed(seed);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OnClickQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
