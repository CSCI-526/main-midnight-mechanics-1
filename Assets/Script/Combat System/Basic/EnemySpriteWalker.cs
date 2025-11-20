using UnityEngine;

public class EnemySpriteWalker : MonoBehaviour
{
    [Header("组件")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [System.Serializable]
    public class AnimSet
    {
        [Tooltip("这一套敌人的行走动画帧（按播放顺序，从第一帧到最后一帧）")]
        public Sprite[] frames;
    }

    [Header("可选的动画套装（手动填三套或更多）")]
    [SerializeField] private AnimSet[] animSets;

    [Header("动画参数")]
    [SerializeField] private float animFPS = 8f;           // 动画帧率
    [SerializeField] private bool randomStartFrame = true; // 是否随机起始帧（让一群怪不同步）

    // —— 运行时状态 —— 
    private Sprite[] _currentFrames;  // 只指向“选中的那一套”
    private float    _frameTimer;
    private int      _frameIndex;
    private int      _chosenSetIndex = -1; // 仅用于调试看看选的是哪套

    private void Reset()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (animSets == null || animSets.Length == 0)
        {
            Debug.LogWarning("[EnemySpriteWalker] animSets 为空，请在 Inspector 里填三套 sprite。", this);
            enabled = false;
            return;
        }

        // —— 只在生成时随机选“一套”，之后不再改变 —— 
        _chosenSetIndex = Random.Range(0, animSets.Length);
        var set = animSets[_chosenSetIndex];

        if (set == null || set.frames == null || set.frames.Length == 0)
        {
            Debug.LogWarning($"[EnemySpriteWalker] 第 {_chosenSetIndex} 套 frames 为空。", this);
            enabled = false;
            return;
        }

        _currentFrames = set.frames;

        // 初始化当前帧
        if (randomStartFrame)
            _frameIndex = Random.Range(0, _currentFrames.Length);
        else
            _frameIndex = 0;

        spriteRenderer.sprite = _currentFrames[_frameIndex];
        _frameTimer = 0f;
    }

    private void Update()
    {
        if (_currentFrames == null || _currentFrames.Length == 0) return;

        float frameDuration = 1f / Mathf.Max(1f, animFPS);
        _frameTimer += Time.deltaTime;

        if (_frameTimer >= frameDuration)
        {
            _frameTimer -= frameDuration;

            _frameIndex++;
            if (_frameIndex >= _currentFrames.Length)
                _frameIndex = 0;

            spriteRenderer.sprite = _currentFrames[_frameIndex];
        }
    }
}
