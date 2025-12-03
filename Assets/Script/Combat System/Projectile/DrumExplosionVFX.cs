using UnityEngine;

/// <summary>
/// Drum 的圆形爆炸特效：
/// - 依赖挂在同一物体上的 SpriteRenderer（你自己拖 sprite）
/// - Play() 时从中心点开始：半径从 0 → radius，颜色 Alpha 从 1 → 0
/// - 不负责销毁 GameObject，DrumProjectile 会统一 Destroy
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DrumExplosionVFX : MonoBehaviour
{
    [Header("动画节奏")]
    [Tooltip("半径随时间的变化（0~1），0=起点，1=最大半径。留空就用默认 EaseInOut。")]
    [SerializeField] private AnimationCurve radiusCurve;

    [Tooltip("透明度随时间的变化（0~1），0=完全透明，1=不透明。留空就用默认 1→0。")]
    [SerializeField] private AnimationCurve alphaCurve;

    SpriteRenderer _sr;
    Color _baseColor;

    float _targetRadius;     // 目标爆炸半径（世界单位）
    float _duration;         // 整个动画时长
    float _time;             // 已运行时间
    float _spriteBaseRadius; // sprite 在 scale=1 时的世界半径

    bool _playing;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr.color;

        // 记录 scale=1 时，这张 sprite 在世界里的“半径”（宽度的一半）
        _spriteBaseRadius = _sr.bounds.extents.x;
        if (_spriteBaseRadius <= 0f)
            _spriteBaseRadius = 0.5f; // 防止极端情况

        // 默认曲线兜底
        if (radiusCurve == null || radiusCurve.keys.Length == 0)
            radiusCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        if (alphaCurve == null || alphaCurve.keys.Length == 0)
            alphaCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0f)
            );
    }

    /// <summary>
    /// 从给定 center 开始播放一次爆炸。
    /// </summary>
    public void Play(Vector2 center, float radius, float duration)
    {
        transform.position = center;

        _targetRadius = Mathf.Max(0.01f, radius);
        _duration     = Mathf.Max(0.05f, duration);
        _time         = 0f;
        _playing      = true;

        // 重置初始状态
        transform.localScale = Vector3.zero;
        _sr.color = _baseColor;
    }

    void Update()
    {
        if (!_playing) return;

        _time += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(_time / _duration);

        // 1) 半径插值：0 → targetRadius
        float rNorm = Mathf.Clamp01(radiusCurve.Evaluate(t)); // 0~1
        float radiusNow = _targetRadius * rNorm;

        // 换算成 scale：baseRadius * scale = radiusNow
        float scale = radiusNow / _spriteBaseRadius;
        transform.localScale = new Vector3(scale, scale, 1f);

        // 2) Alpha 插值：baseAlpha → 0
        float aNorm = Mathf.Clamp01(alphaCurve.Evaluate(t));
        var c = _baseColor;
        c.a *= aNorm;
        _sr.color = c;

        if (t >= 1f)
        {
            _playing = false;
            // 不销毁 GO，由 DrumProjectile 统一 Destroy(gameObject)
        }
    }
}
