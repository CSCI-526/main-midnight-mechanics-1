using UnityEngine;

/// <summary>
/// 统一的弹体视觉脚本：
/// 1) 让 Sprite 朝着运动方向旋转
/// 2) 轻微“呼吸”缩放
/// 3) 生成渐隐的残影拖尾
///
/// 挂到所有需要特效的弹体 prefab 上即可（Drum 除外）。
/// 要求：同一物体上必须有 SpriteRenderer；有 Rigidbody2D 则优先用刚体速度。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class ProjectileVisualEffects : MonoBehaviour
{
    [Header("Face Velocity + Breath")]
    [SerializeField] private bool  faceVelocity     = true;
    [SerializeField] private float minVelForRotate  = 0.01f;

    [SerializeField] private bool  useBreath        = true;
    [SerializeField] private float breathAmplitude  = 0.06f; // ±6%
    [SerializeField] private float breathSpeed      = 3f;    // 次/秒

    [Header("Afterimage Trail")]
    [SerializeField] private bool  useAfterimage        = true;
    [SerializeField] private float spawnInterval        = 0.03f;
    [SerializeField] private float afterimageLife       = 0.25f;
    [SerializeField] private float afterimageStartAlpha = 0.6f;
    [SerializeField] private float afterimageScaleMul   = 1.0f;

    Rigidbody2D     _rb;
    SpriteRenderer  _sr;
    Vector3         _baseScale;
    float           _lastAngle;
    float           _nextSpawnTime;
    Vector3         _lastPos;
    bool            _hasLastPos;

    void Awake()
    {
        TryGetComponent(out _rb);
        _sr        = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
        _lastPos   = transform.position;
        _hasLastPos = true;
    }

    void LateUpdate()
    {
        // --- 1. 计算当前“运动方向” ---
        Vector2 vel = Vector2.zero;
        bool hasVel = false;

        if (_rb != null)
        {
            vel = _rb.linearVelocity;
            if (vel.sqrMagnitude > minVelForRotate * minVelForRotate)
            {
                hasVel = true;
            }
        }

        // 对于 Keyboard / Synth 这类自己改 transform 的，做一个位置差分兜底
        if (!hasVel)
        {
            Vector3 curPos = transform.position;
            if (_hasLastPos)
            {
                Vector3 delta = curPos - _lastPos;
                if (delta.sqrMagnitude > minVelForRotate * minVelForRotate)
                {
                    vel    = new Vector2(delta.x, delta.y) / Mathf.Max(Time.deltaTime, 0.0001f);
                    hasVel = true;
                }
            }
            _lastPos   = curPos;
            _hasLastPos = true;
        }

        // --- 2. 面朝运动方向 + 呼吸缩放 ---
        if (faceVelocity)
        {
            if (hasVel)
            {
                float ang = Mathf.Atan2(vel.y, vel.x) * Mathf.Rad2Deg;
                _lastAngle = ang;
            }
            transform.rotation = Quaternion.AngleAxis(_lastAngle, Vector3.forward);
        }

        if (useBreath)
        {
            float s = 1f + Mathf.Sin(Time.unscaledTime * breathSpeed) * breathAmplitude;
            transform.localScale = _baseScale * s;
        }

        // --- 3. 残影拖尾 ---
        if (useAfterimage) TickAfterimage();
    }

    void TickAfterimage()
    {
        if (_sr == null || _sr.sprite == null) return;
        if (Time.unscaledTime < _nextSpawnTime) return;
        _nextSpawnTime = Time.unscaledTime + spawnInterval;

        var go = new GameObject("Afterimage");
        go.transform.position = transform.position;
        go.transform.rotation = transform.rotation;
        go.transform.localScale = transform.localScale * afterimageScaleMul;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _sr.sprite;
        sr.flipX  = _sr.flipX;
        sr.flipY  = _sr.flipY;
        sr.sortingLayerID = _sr.sortingLayerID;
        sr.sortingOrder   = _sr.sortingOrder - 1; // 压一层到子弹下面

        Color c = _sr.color;
        c.a = afterimageStartAlpha;
        sr.color = c;

        var inst = go.AddComponent<AfterimageInstance>();
        inst.life = afterimageLife;
    }

    /// <summary>
    /// 残影实例：负责自己渐隐并销毁。
    /// 不需要手动挂，代码自动添加。
    /// </summary>
    private class AfterimageInstance : MonoBehaviour
    {
        public float life = 0.25f;

        SpriteRenderer _sr;
        float _age;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            _age += dt;

            if (_sr != null)
            {
                float t = Mathf.Clamp01(_age / Mathf.Max(0.0001f, life));
                var c = _sr.color;
                c.a = Mathf.Lerp(c.a, 0f, t);
                _sr.color = c;
            }

            if (_age >= life)
            {
                Destroy(gameObject);
            }
        }
    }
}
