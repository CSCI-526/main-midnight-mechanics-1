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
    public void SetSequenceLength(int len) => sequenceLength = Mathf.Max(1, len); // ← 只保留这一份

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
        _roundSerial++;
        StopWrongRoutineIfAny();

        BuildNewSequence();
        BuildUI();

        _inputIndex = 0;
        _completed  = false;
    }

    void HandleRoundEnd() { }

    void HandleBasicHit()
    {
        if (_completed)
            Debug.Log("<color=#2EEA55>[PATTERN] BASIC + SKILL</color>");
        else
            Debug.Log("<color=#55AAFF>[PATTERN] BASIC only</color>");
    }

    // —— 生成 —— 
    void BuildNewSequence()
    {
        _seq.Clear();
        int len = Mathf.Max(1, sequenceLength);
        for (int i = 0; i < len; i++)
            _seq.Add((Dir)Random.Range(0, 4));
    }

    // —— UI 构建 —— 
    void BuildUI()
    {
        for (int i = patternRow.childCount - 1; i >= 0; i--)
        {
            var child = patternRow.GetChild(i);
            if (child.gameObject == cellTemplate.gameObject) continue;
            Destroy(child.gameObject);
        }
        _cells.Clear();

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
            if (_inputIndex >= _seq.Count) _completed = true;
        }
        else
        {
            FlashAllWrongThenRefresh();
        }
    }

    // —— 错误闪烁与重刷 —— 
    void FlashAllWrongThenRefresh()
    {
        StopWrongRoutineIfAny();
        _wrongRoutine = StartCoroutine(WrongAndRefreshCoro(_roundSerial));
    }

    public void StopWrongRoutineIfAny()
    {
        if (_wrongRoutine != null)
        {
            StopCoroutine(_wrongRoutine);
            _wrongRoutine = null;
        }
    }

    IEnumerator WrongAndRefreshCoro(int roundSerialAtStart)
    {
        for (int i = 0; i < _cells.Count; i++) _cells[i].SetWrong();

        float t = 0f;
        while (t < wrongFlashSeconds)
        {
            if (roundSerialAtStart != _roundSerial) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (roundSerialAtStart == _roundSerial)
        {
            BuildNewSequence();
            BuildUI();
            _inputIndex = 0;
            _completed  = false;
        }

        _wrongRoutine = null;
    }
    
    
    public void ResetForNewLevel()
    {
        StopWrongRoutineIfAny();
        
        for (int i = patternRow.childCount - 1; i >= 0; i--)
        {
            var child = patternRow.GetChild(i);
            if (child.gameObject == cellTemplate.gameObject) continue;
            Destroy(child.gameObject);
        }
        _cells.Clear();

        _seq.Clear();
        _inputIndex = 0;
        _completed  = false;
    }
}
