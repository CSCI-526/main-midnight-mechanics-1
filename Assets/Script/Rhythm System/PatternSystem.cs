using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatternSystem : MonoBehaviour
{
    public enum Dir { Up, Down, Left, Right }

    [Header("Refs")]
    [SerializeField] private RhythmSystem rhythm;
    [SerializeField] private RectTransform patternRow;
    [SerializeField] private PatternCell cellTemplate; // 请保持 Inactive 的模板，位于 patternRow 下

    [Header("Settings")]
    [SerializeField, Tooltip("每回合指令长度")]
    private int sequenceLength = 3;

    [Header("Feedback")]
    [SerializeField, Tooltip("按错后全体显示错误图的停留秒数，然后重刷新序列")]
    private float wrongFlashSeconds = 0.25f;

    private readonly List<Dir> _seq = new();
    private readonly List<PatternCell> _cells = new();
    private int  _inputIndex = 0;
    private bool _completed  = false;

    // 防并发/跨回合的标识
    private Coroutine _wrongRoutine;
    private int _roundSerial = 0;

    public bool IsSequenceCompleted => _completed;

    void OnEnable()
    {
        if (rhythm)
        {
            rhythm.OnRoundStart += HandleRoundStart;
            rhythm.OnRoundEnd   += HandleRoundEnd;
        }
        HitJudge.OnBasicHit += HandleBasicHit;
    }

    void OnDisable()
    {
        if (rhythm)
        {
            rhythm.OnRoundStart -= HandleRoundStart;
            rhythm.OnRoundEnd   -= HandleRoundEnd;
        }
        HitJudge.OnBasicHit -= HandleBasicHit;
        StopWrongRoutineIfAny();
    }

    void Update()
    {
        if (_seq.Count == 0 || _completed) return;

        if      (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W)) Consume(Dir.Up);
        else if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S)) Consume(Dir.Down);
        else if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) Consume(Dir.Left);
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) Consume(Dir.Right);
    }

    // —— 回合控制 ——
    void HandleRoundStart()
    {
        _roundSerial++;               // 新回合标记
        StopWrongRoutineIfAny();      // 终止可能仍在执行的错误闪烁

        BuildNewSequence();
        BuildUI();

        _inputIndex = 0;
        _completed  = false;
    }

    void HandleRoundEnd()
    {
        // 留空：真正的刷新在下一次 OnRoundStart 里完成
    }

    void HandleBasicHit()
    {
        if (_completed)
        {
            Debug.Log("<color=#2EEA55>[PATTERN] BASIC + SKILL</color>");
            // 这里后续接技能触发
        }
        else
        {
            Debug.Log("<color=#55AAFF>[PATTERN] BASIC only</color>");
        }
    }

    // —— 生成 —— 
    void BuildNewSequence()
    {
        _seq.Clear();
        int len = Mathf.Max(1, sequenceLength);
        for (int i = 0; i < len; i++)
            _seq.Add((Dir)Random.Range(0, 4)); // 0..3
    }

    // —— UI 构建 —— 
    void BuildUI()
    {
        // 清理旧格子（保留模板本体）
        for (int i = patternRow.childCount - 1; i >= 0; i--)
        {
            var child = patternRow.GetChild(i);
            if (child.gameObject == cellTemplate.gameObject) continue; // 不删模板
            Destroy(child.gameObject);
        }
        _cells.Clear();

        // 生成新格子
        foreach (var dir in _seq)
        {
            var cell = Instantiate(cellTemplate, patternRow);
            cell.gameObject.SetActive(true);
            cell.SetSymbol((PatternCell.Dir)dir);
            cell.SetDefault();
            _cells.Add(cell);
        }
    }

    // —— 输入消费 —— 
    void Consume(Dir input)
    {
        if (_completed) return;
        if (_inputIndex < 0 || _inputIndex >= _seq.Count) return;

        bool ok = (input == _seq[_inputIndex]);
        if (ok)
        {
            _cells[_inputIndex].SetOk();
            _inputIndex++;
            if (_inputIndex >= _seq.Count)
                _completed = true;
        }
        else
        {
            // 按错：所有格子切到 Wrong，短暂停留后直接“随机新序列 + 重建UI + 进度清零”
            FlashAllWrongThenRefresh();
        }
    }

    // —— 错误闪烁与重刷 —— 
    void FlashAllWrongThenRefresh()
    {
        StopWrongRoutineIfAny();
        _wrongRoutine = StartCoroutine(WrongAndRefreshCoro(_roundSerial));
    }

    void StopWrongRoutineIfAny()
    {
        if (_wrongRoutine != null)
        {
            StopCoroutine(_wrongRoutine);
            _wrongRoutine = null;
        }
    }

    IEnumerator WrongAndRefreshCoro(int roundSerialAtStart)
    {
        // 1) 全部显示 Wrong
        for (int i = 0; i < _cells.Count; i++)
            _cells[i].SetWrong();

        // 2) 停留一段时间；若期间回合被切换（空格触发 ForceNextRound）则放弃
        float t = 0f;
        while (t < wrongFlashSeconds)
        {
            if (roundSerialAtStart != _roundSerial) yield break; // 已进入新回合，协程终止
            t += Time.deltaTime;
            yield return null;
        }

        // 3) 同一回合内，直接重刷新序列与UI
        if (roundSerialAtStart == _roundSerial)
        {
            BuildNewSequence();
            BuildUI();
            _inputIndex = 0;
            _completed  = false;
        }

        _wrongRoutine = null;
    }
    public void SetSequenceLength(int len)
    {
        sequenceLength = Mathf.Max(1, len);
    }
}
