using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Level Pack", fileName = "LevelPack")]
public class LevelPack : ScriptableObject
{
    public string packName = "Remix";
    public List<LevelConfig> levels = new();
}