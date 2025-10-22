using UnityEngine;
using UnityEngine.UI;

public sealed class TutorialOverlay : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;      // 覆盖层Panel（初始Inactive）
    [SerializeField] private Image      picture;   // 显示图片的Image
    [SerializeField] private Button     nextButton;
    [SerializeField] private Button     exitButton;

    [Header("Open Triggers (optional)")]
    [SerializeField] private Button[] openButtons; // 直接把“Tutorial”按钮拖进来

    [Header("Pages")]
    [SerializeField] private Sprite[] pages;       // 教程图片

    [Header("Layout")]
    [SerializeField, Tooltip("按屏幕尺寸缩放比例")]
    private float viewportScale = 0.7f;

    [Header("Options")]
    [SerializeField] private bool pauseOnShow = false;

    int _index;

    void Awake()
    {
        if (!root) root = gameObject;

        // 绑定 Next / Exit
        if (nextButton)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnClickNext);
        }
        if (exitButton)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(Hide);
        }

        // 绑定“打开”按钮（在本脚本内部完成，无需额外脚本）
        if (openButtons != null)
        {
            foreach (var btn in openButtons)
            {
                if (!btn) continue;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Show);
            }
        }

        if (picture) picture.preserveAspect = true;
        if (root.activeSelf) root.SetActive(false);
    }

    public void Show()
    {
        _index = 0;
        RefreshPage();
        ResizePictureToViewport();
        root.transform.SetAsLastSibling();
        root.SetActive(true);
        if (pauseOnShow) Time.timeScale = 0f;
    }

    public void Hide()
    {
        if (pauseOnShow) Time.timeScale = 1f;
        root.SetActive(false);
    }

    void OnClickNext()
    {
        if (pages == null || pages.Length == 0) { Hide(); return; }
        _index++;
        if (_index >= pages.Length) Hide();
        else RefreshPage();
    }

    void RefreshPage()
    {
        if (!picture) return;
        Sprite s = null;
        if (pages != null && _index >= 0 && _index < pages.Length) s = pages[_index];
        picture.sprite = s;
    }

    void ResizePictureToViewport()
    {
        if (!picture) return;
        var rt = picture.rectTransform;

        var canvas = rt.GetComponentInParent<Canvas>();
        var rootRT = canvas ? canvas.GetComponent<RectTransform>() : rt.root as RectTransform;
        if (!rootRT) return;

        float w = rootRT.rect.width  * viewportScale;
        float h = rootRT.rect.height * viewportScale;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   h);
        // preserveAspect=true 会自动按比例适配这个矩形
    }

    void OnRectTransformDimensionsChange()
    {
        // 分辨率变化时保持0.7缩放
        if (root && root.activeSelf) ResizePictureToViewport();
    }

    void OnDisable()
    {
        if (pauseOnShow) Time.timeScale = 1f; // 防止外部禁用时卡住时间
    }
}
