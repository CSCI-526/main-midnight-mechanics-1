using UnityEngine;
using System.Collections.Generic;

public sealed class DrumProjectile : ProjectileBase
{
    [Header("Simple Flash Circle")]
    [Tooltip("一张圆形的 sprite（中间白/亮色，外圈透明）。")]
    [SerializeField] private Sprite flashSprite;

    [SerializeField] private Color flashColor = new(1f, 1f, 1f, 0.9f);

    public void Explode(Vector2 center, float radius, int damage, float vfxLife)
    {
        // 1) AoE 伤害（保持不变）
        var victims = new List<Enemy>(32);
        float r2 = radius * radius;
        foreach (var e in Enemy.All)
        {
            if (e && ((Vector2)e.transform.position - center).sqrMagnitude <= r2)
                victims.Add(e);
        }

        foreach (var v in victims)
        {
            var hp = v.GetComponent<EnemyHealth>();
            if (hp) hp.TakeDamage(damage);
            else    v.Kill();
        }

        // 2) 扩张+渐隐的白色圆闪一下
        DrawFlashCircle(center, radius, vfxLife);

        // 3) 弹体自己延时销毁
        Destroy(gameObject, Mathf.Max(0.01f, vfxLife));
    }

    void DrawFlashCircle(Vector2 c, float r, float life)
    {
        if (!flashSprite)
        {
            Debug.LogWarning("[DrumProjectile] flashSprite 未设置，跳过爆炸特效。");
            return;
        }

        var go = new GameObject("DrumFlashCircle");
        go.transform.position = c;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = flashSprite;
        sr.color  = flashColor;

        // 计算 sprite 在 scale=1 时的世界半径（x 方向一半宽度）
        float baseRadius = sr.bounds.extents.x;
        if (baseRadius <= 0f)
            baseRadius = 0.5f; // 极端兜底，防止除 0

        // 目标：显示出来的半径 = r
        float targetScale = r / baseRadius;

        // 初始从一个更小的比例开始，看起来像“从中心炸开”
        float startScale = targetScale * 0.3f;

        // 挂一个内部动画组件，负责扩张+渐隐
        var inst = go.AddComponent<FlashCircleInstance>();
        inst.Init(sr, startScale, targetScale, life);
    }

    /// <summary>
    /// 内部类：负责把圆圈从小变大、同时渐隐，然后销毁自己。
    /// 写在同一个脚本里，不需要额外 .cs 文件。
    /// </summary>
    private sealed class FlashCircleInstance : MonoBehaviour
    {
        SpriteRenderer _sr;
        float _startScale;
        float _targetScale;
        float _duration;
        float _time;
        Color _baseColor;

        public void Init(SpriteRenderer sr, float startScale, float targetScale, float duration)
        {
            _sr          = sr;
            _startScale  = startScale;
            _targetScale = targetScale;
            _duration    = Mathf.Max(0.05f, duration);
            _time        = 0f;
            _baseColor   = sr.color;

            // 初始缩放设成小圈
            transform.localScale = Vector3.one * _startScale;
        }

        void Update()
        {
            if (_sr == null)
            {
                Destroy(gameObject);
                return;
            }

            _time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_time / _duration);

            // 扩张：用 sqrt 做一点“先快后慢”的缓出效果
            float eased = Mathf.Sqrt(t);
            float s = Mathf.Lerp(_startScale, _targetScale, eased);
            transform.localScale = new Vector3(s, s, 1f);

            // 渐隐：alpha 线性从 base → 0
            var c = _baseColor;
            c.a = Mathf.Lerp(_baseColor.a, 0f, t);
            _sr.color = c;

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
