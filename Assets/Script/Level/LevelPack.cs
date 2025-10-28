using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Level Pack", fileName = "LevelPack")]
public class LevelPack : ScriptableObject
{
    public string packName = "Challenge Pack";
    public List<LevelConfig> levels = new();

    [Header("Challenge Settings")]
    [Min(0)] public int challengeStartGold = 120; // 每个包的起始金币
}