// LevelSelectorUI.cs
using UnityEngine;

public class LevelSelectorUI : MonoBehaviour
{
    [SerializeField] private SceneFlow sceneFlow;
    
    public void SelectPackAndPlay(LevelPack pack)
    {
        if (!pack) { Debug.LogError("[LevelSelectorUI] Pack is null"); return; }

        var session = GameSession.Instance ?? FindObjectOfType<GameSession>();
        if (!session) { Debug.LogError("[LevelSelectorUI] GameSession not found"); return; }

        session.BeginPack(pack);                  // 选定专辑，从第0关开始
        if (!sceneFlow) sceneFlow = FindObjectOfType<SceneFlow>(true);
        sceneFlow.LoadGameplay();                 // 进入 Gameplay
    }
}