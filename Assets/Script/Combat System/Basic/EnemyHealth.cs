using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int  maxHP = 10;
    [SerializeField] private bool destroyOnDeath = true;

    [Header("Hit Flash")]
    [Tooltip("命中时白闪时长")]
    [SerializeField] private float flashDuration = 0.06f;
    [Tooltip("命中时是否对白色闪烁（修改 SpriteRenderer.color）")]
    [SerializeField] private bool flashWhite = true;

    [Header("Notify Enemy (Hit-Stop + Shake)")]
    [Tooltip("命中停顿时长（秒）；≤0 使用 Enemy 的默认值")]
    [SerializeField] private float hitStopSeconds = 0.06f;
    [Tooltip("抖动幅度；<0 使用 Enemy 的默认值")]
    [SerializeField] private float shakeAmplitude = 0.08f;

    public int Current { get; private set; }

    // 缓存 Sprites 用于白闪
    SpriteRenderer[] _sprites;
    Color[]          _baseColors;
    Coroutine        _flashCo;

    void Awake()
    {
        Current = Mathf.Max(1, maxHP);

        if (flashWhite)
        {
            _sprites = GetComponentsInChildren<SpriteRenderer>(true);
            _baseColors = new Color[_sprites.Length];
            for (int i = 0; i < _sprites.Length; i++)
                _baseColors[i] = _sprites[i].color;
        }
    }

    public void TakeDamage(int dmg)
    {
        if (dmg <= 0 || Current <= 0) return;

        // 1) 触发命中停顿 + 抖动（不需要改任何技能/弹体）
        var enemy = GetComponent<Enemy>();
        if (enemy) enemy.HitPauseAndShake(hitStopSeconds, shakeAmplitude);

        // 2) 白闪
        if (flashWhite)
        {
            if (_flashCo != null) StopCoroutine(_flashCo);
            _flashCo = StartCoroutine(CoFlashWhite());
        }

        // 3) 扣血 & 死亡
        Current -= dmg;
        if (Current <= 0) Die();
    }

    IEnumerator CoFlashWhite()
    {
        // 设置为白色
        for (int i = 0; i < _sprites.Length; i++)
            if (_sprites[i]) _sprites[i].color = Color.white;

        yield return new WaitForSeconds(flashDuration);

        // 还原
        for (int i = 0; i < _sprites.Length; i++)
            if (_sprites[i]) _sprites[i].color = _baseColors[i];

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
