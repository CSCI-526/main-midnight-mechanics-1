// AspectRatioLetterbox.cs
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AspectRatioLetterbox : MonoBehaviour
{
    [SerializeField] private int targetWidth = 16;
    [SerializeField] private int targetHeight = 9;
    [SerializeField] private Color barColor = Color.black;

    Camera cam;
    void Awake()
    {
        cam = GetComponent<Camera>();
        cam.backgroundColor = barColor;   // 黑边颜色
    }

    void OnEnable()  => Apply();
    void OnPreCull() => Apply();          // 分辨率改变时也能适配（含 WebGL 伸缩）
#if UNITY_EDITOR
    void OnValidate() { if (cam) Apply(); }
#endif

    void Apply()
    {
        if (!cam) cam = GetComponent<Camera>();

        float target = (float)targetWidth / targetHeight;
        float window = (float)Screen.width / Screen.height;

        if (window > target)
        {
            // 更“宽” → 左右加边（pillarbox）
            float w = target / window;
            float x = (1f - w) * 0.5f;
            cam.rect = new Rect(x, 0f, w, 1f);
        }
        else
        {
            // 更“高” → 上下加边（letterbox）
            float h = window / target;
            float y = (1f - h) * 0.5f;
            cam.rect = new Rect(0f, y, 1f, h);
        }
    }
}