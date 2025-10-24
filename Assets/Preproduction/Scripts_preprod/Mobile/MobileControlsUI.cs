using UnityEngine;
using UnityEngine.UI;

public class MobileControlsUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button upButton;
    public Button downButton;
    public Button leftButton;
    public Button rightButton;
    public Button hitButton;

    [Header("References")]
    public PatternSystem patternSystem;
    public HitJudge hitJudge;

    void Awake()
    {
        if (!patternSystem) patternSystem = FindObjectOfType<PatternSystem>();
        if (!hitJudge) hitJudge = FindObjectOfType<HitJudge>();
    }

    void Start()
    {
        if (!patternSystem || !hitJudge) return;

        upButton.onClick.AddListener(() => patternSystem.Consume(PatternSystem.Dir.Up));
        downButton.onClick.AddListener(() => patternSystem.Consume(PatternSystem.Dir.Down));
        leftButton.onClick.AddListener(() => patternSystem.Consume(PatternSystem.Dir.Left));
        rightButton.onClick.AddListener(() => patternSystem.Consume(PatternSystem.Dir.Right));

        hitButton.onClick.AddListener(() => hitJudge.OnHitButtonPressed());
    }
}
