using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int  maxHP = 10;
    [SerializeField] private bool destroyOnDeath = true;

    [Header("Hit Flash")]
    [Tooltip("命中时闪烁的颜色（例如纯红）")]
    [SerializeField] private Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("命中时闪烁时长")]
    [SerializeField] private float flashDuration = 0.06f;

    [Tooltip("是否启用命中闪烁效果")]
    [SerializeField] private bool enableFlash = true;

    [Header("Notify Enemy (Hit-Stop + Shake)")]
    [Tooltip("命中停顿时长（秒）；≤0 使用 Enemy 的默认值")]
    [SerializeField] private float hitStopSeconds = 0.06f;

    [Tooltip("抖动幅度；<0 使用 Enemy 的默认值")]
    [SerializeField] private float shakeAmplitude = 0.08f;

    public int Current { get; private set; }

    // 缓存 Sprites 用于闪烁
    private SpriteRenderer[] _sprites;
    private Color[]          _baseColors;
    private Coroutine        _flashCo;

    void Awake()
    {
        Current = Mathf.Max(1, maxHP);

        // 缓一份所有子节点 SpriteRenderer，做统一闪烁
        _sprites = GetComponentsInChildren<SpriteRenderer>(true);

        if (_sprites != null && _sprites.Length > 0)
        {
            _baseColors = new Color[_sprites.Length];
            for (int i = 0; i < _sprites.Length; i++)
                _baseColors[i] = _sprites[i].color;
        }
        else
        {
            _baseColors = System.Array.Empty<Color>();
        }
    }

    public void TakeDamage(int dmg)
    {
        if (dmg <= 0 || Current <= 0) return;

        // 1) 触发命中停顿 + 抖动（不需要改任何技能/弹体）
        var enemy = GetComponent<Enemy>();
        if (enemy) enemy.HitPauseAndShake(hitStopSeconds, shakeAmplitude);

        // 2) 命中闪烁（改成闪红 / 自定义颜色）
        if (enableFlash && _sprites != null && _sprites.Length > 0)
        {
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(CoFlashTint());
        }

        // 3) 扣血 & 死亡
        Current -= dmg;
        if (Current <= 0) Die();
    }

    IEnumerator CoFlashTint()
    {
        // 设置为 flashColor
        for (int i = 0; i < _sprites.Length; i++)
        {
            if (_sprites[i])
                _sprites[i].color = flashColor;
        }

        yield return new WaitForSeconds(flashDuration);

        // 还原原始颜色
        for (int i = 0; i < _sprites.Length && i < _baseColors.Length; i++)
        {
            if (_sprites[i])
                _sprites[i].color = _baseColors[i];
        }

        _flashCo = null;
    }

    void Die()
    {
        var enemy = GetComponent<Enemy>();
        if (enemy) enemy.Kill();
        else if (destroyOnDeath) Destroy(gameObject);
    }

    public void SetMaxHP(int hp)
    {
        maxHP  = Mathf.Max(1, hp);
        Current = maxHP;
    }
}
