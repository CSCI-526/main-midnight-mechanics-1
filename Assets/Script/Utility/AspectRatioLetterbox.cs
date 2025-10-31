// AspectRatioLetterbox.cs
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioLetterbox : MonoBehaviour
{
    [SerializeField] private int targetWidth  = 16;
    [SerializeField] private int targetHeight = 9;

    [Tooltip("WebGL/窗口尺寸变化时，实时检查并应用")]
    [SerializeField] private bool liveUpdate = true;

    [Tooltip("把近似 16:9 的窗口当作完全匹配，避免细缝黑边")]
    [SerializeField] private float epsilon = 0.01f;

    private Camera cam;
    private int lastW = -1, lastH = -1;

    void Awake()
    {
        cam = GetComponent<Camera>();
        // 不再强制改背景色；黑边会自然是黑/背景默认色
        // cam.clearFlags 可保持你的设置（Skybox / Solid Color）
    }

    void OnEnable() { Apply(); }

    void Update()
    {
        if (!liveUpdate) return;
        if (Screen.width != lastW || Screen.height != lastH)
            Apply();
    }

    void Apply()
    {
        lastW = Screen.width; lastH = Screen.height;

        float target = (float)targetWidth / Mathf.Max(1, targetHeight);
        float window = (float)Screen.width / Mathf.Max(1, Screen.height);

        if (Mathf.Abs(window - target) <= epsilon)
        {
            cam.rect = new Rect(0f, 0f, 1f, 1f); // 视口填满（无黑边）
            return;
        }

        if (window > target)
        {
            // 更“宽” → 左右加边
            float w = target / window;
            float x = (1f - w) * 0.5f;
            cam.rect = new Rect(x, 0f, w, 1f);
        }
        else
        {
            // 更“高” → 上下加边
            float h = window / target;
            float y = (1f - h) * 0.5f;
            cam.rect = new Rect(0f, y, 1f, h);
        }
    }
}