using UnityEngine;

[DisallowMultipleComponent]
public class TutorialPanelsGate : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LevelRunner levelRunner;          // 可留空，自动查找（含 Inactive）

    [Header("Only show this in tutorial levels")]
    [SerializeField] private GameObject tutorialIngamePanel;   // 挂在 Main UI 下，默认 Inactive

    [Header("Identify Tutorial Level (any keyword matches)")]
    [Tooltip("在 levelName / LevelConfig.name / chart.name 中命中任意关键词即视为教程关")]
    [SerializeField] private string[] tutorialKeywords = new[] { "tutorial", "lp_tutorial" };

    [Header("Debug")]
    [SerializeField] private bool verboseLog = false;

    void OnEnable()
    {
        if (!levelRunner)
            levelRunner = FindFirstObjectByType<LevelRunner>(FindObjectsInactive.Include);

        if (levelRunner)
        {
            levelRunner.OnLevelApplied += Refresh;   // 关卡加载/应用后刷新
            levelRunner.OnLevelEnded   += HidePanel; // 关卡结束即关闭
        }

        // 场景刚打开先尝试一次（若此时还没 Apply，会在 OnLevelApplied 再刷一次）
        Refresh();
    }

    void OnDisable()
    {
        if (levelRunner)
        {
            levelRunner.OnLevelApplied -= Refresh;
            levelRunner.OnLevelEnded   -= HidePanel;
        }
    }

    void Refresh()
    {
        bool isTut = IsTutorialLevel();
        if (verboseLog) Debug.Log($"[TutorialPanelsGate] isTutorial={isTut}");
        SafeSet(tutorialIngamePanel, isTut);
    }

    void HidePanel()
    {
        SafeSet(tutorialIngamePanel, false);
    }

    bool IsTutorialLevel()
    {
        if (!levelRunner || !levelRunner.Current) return false;

        // 候选名：LevelConfig.levelName -> LevelConfig.name -> Chart.name
        string a = levelRunner.Current.levelName ?? string.Empty;
        string b = levelRunner.Current.name      ?? string.Empty;
        string c = levelRunner.Current.chart ? levelRunner.Current.chart.name : string.Empty;

        for (int i = 0; i < tutorialKeywords.Length; i++)
        {
            string kw = tutorialKeywords[i];
            if (string.IsNullOrEmpty(kw)) continue;

            if (a.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (b.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (c.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    static void SafeSet(GameObject go, bool on)
    {
        if (go && go.activeSelf != on) go.SetActive(on);
    }
}
