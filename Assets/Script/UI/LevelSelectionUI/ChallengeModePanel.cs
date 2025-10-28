using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChallengeModePanel : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public Button button;   // 关卡按钮
        public LevelPack pack;  // 对应的 LevelPack（建议每包一关）
    }

    [Header("UI")]
    [SerializeField] private GameObject root;       // 面板根（不填则用自身）
    [SerializeField] private Button[] openButtons;  // 打开面板的入口按钮
    [SerializeField] private Button exitButton;     // 关闭面板

    [Header("Entries")]
    [SerializeField] private List<Entry> entries = new List<Entry>();

    [Header("Flow")]
    [SerializeField] private SceneFlow sceneFlow;   // 拖入 SceneFlow

    void Awake()
    {
        if (!root) root = gameObject;

        // 入口：打开
        if (openButtons != null)
        {
            foreach (var btn in openButtons)
            {
                if (!btn) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Show);
            }
        }

        // 退出：关闭
        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(Hide);
        }

        // 按钮直连关卡
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!e.button) continue;
            var packCaptured = e.pack;
            e.button.onClick.RemoveAllListeners();
            e.button.onClick.AddListener(() => StartChallenge(packCaptured));
        }

        // 初始隐藏
        if (root.activeSelf) root.SetActive(false);
    }

    public void Show()
    {
        if (!sceneFlow) { Debug.LogError("[ChallengeModePanel] SceneFlow 未设置。", this); return; }
        root.transform.SetAsLastSibling();
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    void StartChallenge(LevelPack pack)
    {
        if (!sceneFlow) { Debug.LogError("[ChallengeModePanel] SceneFlow 未设置。", this); return; }
        if (!pack || pack.levels == null || pack.levels.Count == 0)
        {
            Debug.LogError("[ChallengeModePanel] 该 LevelPack 为空或未配置关卡。");
            return;
        }

        var session = GameSession.Instance ?? FindFirstObjectByType<GameSession>(FindObjectsInactive.Include);
        if (!session) { Debug.LogError("[ChallengeModePanel] GameSession 未找到。"); return; }

        session.BeginChallenge(pack);
        sceneFlow.LoadGameplay();
    }
}
