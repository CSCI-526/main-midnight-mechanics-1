using System.Collections.Generic;
using UnityEngine;
using static SkillLibrary;

public class SkillsHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerSkills playerSkills;
    [SerializeField] private SkillLibrary library;

    [Header("Mode")]
    [SerializeField] private bool useManualSlots = true;  // 手工槽位模式

    [Header("Manual Slots (drag your slots here)")]
    [SerializeField] private Transform activeRow;          // 仅用于“从子节点收集”
    [SerializeField] private Transform passiveRow;         // 仅用于“从子节点收集”
    [SerializeField] private List<SkillSlotWidget> activeSlots  = new();   // 手工拖 4 个
    [SerializeField] private List<SkillSlotWidget> passiveSlots = new();   // 手工拖 4 个

    [Header("Auto Instantiate (optional, legacy)")]
    [SerializeField] private SkillSlotWidget slotPrefab;   // 仅当 useManualSlots=false
    [SerializeField] private int slotsPerRow = 4;

    void OnEnable()
    {
        if (!playerSkills) playerSkills = FindObjectOfType<PlayerSkills>(true);

        if (useManualSlots)
        {
            if (activeSlots.Count == 0 && activeRow)
                activeSlots.AddRange(activeRow.GetComponentsInChildren<SkillSlotWidget>(true));
            if (passiveSlots.Count == 0 && passiveRow)
                passiveSlots.AddRange(passiveRow.GetComponentsInChildren<SkillSlotWidget>(true));
            
            foreach (var s in activeSlots)  if (s) s.SetEmpty();
            foreach (var s in passiveSlots) if (s) s.SetEmpty();
        }
        else
        {
            BuildIfNeeded_Auto();
        }

        if (playerSkills) playerSkills.OnChanged += Refresh;
        Refresh();
    }

    void OnDisable()
    {
        if (playerSkills) playerSkills.OnChanged -= Refresh;
    }

    // 
    void BuildIfNeeded_Auto()
    {
        if (slotPrefab == null) return;

        if (activeSlots.Count == 0 && activeRow)
        {
            for (int i = 0; i < slotsPerRow; i++)
            {
                var w = Instantiate(slotPrefab, activeRow);
                w.SetEmpty();
                activeSlots.Add(w);
            }
        }
        if (passiveSlots.Count == 0 && passiveRow)
        {
            for (int i = 0; i < slotsPerRow; i++)
            {
                var w = Instantiate(slotPrefab, passiveRow);
                w.SetEmpty();
                passiveSlots.Add(w);
            }
        }
    }

    // 
    void Refresh()
    {
        if (!playerSkills)
        {
            Debug.LogWarning("[SkillsHUD] PlayerSkills not found.");
            return;
        }
        
        for (int i = 0; i < activeSlots.Count; i++)
        {
            var slot = activeSlots[i];
            if (!slot) continue;

            if (i < playerSkills.Actives.Count)
            {
                var id = playerSkills.Actives[i];
                int lv = playerSkills.GetLevel(id);
                slot.SetActive(library, id, lv);
            }
            else
            {
                slot.SetEmpty();
            }
        }
        
        for (int i = 0; i < passiveSlots.Count; i++)
        {
            var slot = passiveSlots[i];
            if (!slot) continue;

            if (i < playerSkills.Passives.Count)
            {
                var id = playerSkills.Passives[i];
                int lv = playerSkills.GetLevel(id);
                slot.SetPassive(library, id, lv);
            }
            else
            {
                slot.SetEmpty();
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Collect Slots From Children")]
    void CollectSlotsFromChildren()
    {
        activeSlots.Clear();
        passiveSlots.Clear();
        if (activeRow)  activeSlots.AddRange(activeRow.GetComponentsInChildren<SkillSlotWidget>(true));
        if (passiveRow) passiveSlots.AddRange(passiveRow.GetComponentsInChildren<SkillSlotWidget>(true));
        Debug.Log($"[SkillsHUD] Collected: actives={activeSlots.Count}, passives={passiveSlots.Count}");
    }
#endif
}
