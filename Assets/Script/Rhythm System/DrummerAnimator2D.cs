using UnityEngine;

public class DrummerAnimator2D : MonoBehaviour
{
    [Header("渲染器")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("左右两帧")]
    [SerializeField] private Sprite leftSprite;   // 左手击打帧
    [SerializeField] private Sprite rightSprite;  // 右手击打帧

    [Header("起始状态")]
    [SerializeField] private bool startWithLeft = true;  // 一开始用左还是右

    private bool isLeft;  // 当前是不是 Left 帧

    private void Reset()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        HitJudge.OnPerfect += OnHitSuccess;
        HitJudge.OnGood    += OnHitSuccess;
        // 如果你想 Miss 也切换，就顺便加一行：
        // HitJudge.OnMiss    += OnHitSuccess;
    }

    private void OnDisable()
    {
        HitJudge.OnPerfect -= OnHitSuccess;
        HitJudge.OnGood    -= OnHitSuccess;
        // HitJudge.OnMiss    -= OnHitSuccess;
    }

    private void Start()
    {
        if (!spriteRenderer) return;
        if (!leftSprite || !rightSprite) return;

        isLeft = startWithLeft;

        // 初始化一帧
        spriteRenderer.sprite = isLeft ? leftSprite : rightSprite;
    }

    private void OnHitSuccess()
    {
        if (!spriteRenderer) return;
        if (!leftSprite || !rightSprite) return;

        // 每次命中直接左右互换
        isLeft = !isLeft;
        spriteRenderer.sprite = isLeft ? leftSprite : rightSprite;
    }
}