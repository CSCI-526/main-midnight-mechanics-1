using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static SkillLibrary;

public class SkillSlotWidget : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Image frame;               
    [SerializeField] private Color  emptyTint  = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Sprite emptySprite;        

    public void SetEmpty()
    {
        if (!icon) return;

        if (emptySprite != null)
        {
            icon.enabled = true;
            icon.sprite  = emptySprite;
            icon.color   = emptyTint;
        }
        else
        {
            icon.sprite  = null;
            icon.enabled = false;  
        }

        if (levelText) levelText.text = string.Empty;
        // 不触碰 frame
    }

    public void SetActive(SkillLibrary lib, ActiveSkillId id, int level)
    {
        var def = (lib != null) ? lib.GetActive(id) : null;

        if (icon)
        {
            icon.enabled = true;
            icon.sprite  = (def != null && def.icon != null) ? def.icon : emptySprite;
            icon.color   = Color.white;
        }

        if (levelText)
            levelText.text = (def != null) ? $"Lv.{Mathf.Clamp(level, 1, PlayerSkills.MAX_LEVEL)}" : string.Empty;
    }

    public void SetPassive(SkillLibrary lib, PassiveSkillId id, int level)
    {
        var def = (lib != null) ? lib.GetPassive(id) : null;

        if (icon)
        {
            icon.enabled = true;
            icon.sprite  = (def != null && def.icon != null) ? def.icon : emptySprite;
            icon.color   = Color.white;
        }

        if (levelText)
            levelText.text = (def != null) ? $"Lv.{Mathf.Clamp(level, 1, PlayerSkills.MAX_LEVEL)}" : string.Empty;
    }
}