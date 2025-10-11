using UnityEngine;
using UnityEngine.UI;

public class PatternCell : MonoBehaviour
{
    public enum Dir { Up = 0, Down = 1, Left = 2, Right = 3 }

    [Header("UI")]
    [SerializeField] private Image icon;

    [Header("Sprites by Direction (Up,Down,Left,Right)")]
    [SerializeField] private Sprite[] normalSprites = new Sprite[4];
    [SerializeField] private Sprite[] okSprites     = new Sprite[4];
    [SerializeField] private Sprite[] wrongSprites  = new Sprite[4];

    private Dir currentDir;

    public void SetSymbol(Dir d)
    {
        currentDir = d;
        SetDefault();          // 初始显示未完成
        if (icon) icon.color = Color.white; // 确保不被旧的 tint 影响
    }

    public void SetDefault() => SetSpriteFrom(normalSprites);
    public void SetOk()      => SetSpriteFrom(okSprites, fallback: normalSprites);
    public void SetWrong()   => SetSpriteFrom(wrongSprites, fallback: normalSprites);

    void SetSpriteFrom(Sprite[] set, Sprite[] fallback = null)
    {
        if (!icon) return;
        var idx = (int)currentDir;

        Sprite choose = null;
        if (set != null && idx < set.Length) choose = set[idx];
        if (!choose && fallback != null && idx < fallback.Length) choose = fallback[idx];

        if (choose) icon.sprite = choose;
    }

    // 预留：未来做VFX时可在这里触发Animator
    // public void PlayOkVFX() { animator?.SetTrigger("Ok"); }
}