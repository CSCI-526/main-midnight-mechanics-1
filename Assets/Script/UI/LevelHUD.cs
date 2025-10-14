using UnityEngine;
using TMPro;

public class LevelHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private LevelRunner runner;

    // 可自定义显示样式
    [SerializeField] private string format = "{pack}  {index}/{count}  —  {name}";

    void Awake()
    {
        if (!label) label = GetComponent<TextMeshProUGUI>();
        if (!runner) runner = FindObjectOfType<LevelRunner>();
    }

    void OnEnable()
    {
        if (runner) runner.OnLevelApplied += Refresh;
        Refresh(); // 进场先刷一次
    }

    void OnDisable()
    {
        if (runner) runner.OnLevelApplied -= Refresh;
    }

    void Refresh()
    {
        if (!label) return;

        var session = GameSession.Instance ?? FindObjectOfType<GameSession>();
        var pack    = session ? session.SelectedPack : null;
        int idx     = session ? session.CurrentLevelIndex : 0;
        int count   = (pack && pack.levels != null) ? pack.levels.Count : 0;

        // 关卡名可以直接从 runner.Current 拿到
        var level   = runner ? runner.Current : null;

        string txt = format
            .Replace("{pack}",  pack ? pack.packName : "—")
            .Replace("{index}", (idx + 1).ToString())
            .Replace("{count}", count.ToString())
            .Replace("{name}",  level ? level.levelName : "—");

        label.text = txt;
    }
}