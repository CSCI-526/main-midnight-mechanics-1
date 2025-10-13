using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFlow : MonoBehaviour
{
    [Header("Scene Names (Build Settings)")]
    [SerializeField] private string startMenuScene = "StartMenu";
    [SerializeField] private string levelSelectorScene = "LevelSelector";
    [SerializeField] private string gameplayScene = "Gameplay";

    public void LoadStartMenu()     => Load(startMenuScene);
    public void LoadLevelSelector() => Load(levelSelectorScene);
    public void LoadGameplay()      => Load(gameplayScene);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static void Load(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        Time.timeScale = 1f;
        SceneManager.LoadScene(name, LoadSceneMode.Single);
    }
}