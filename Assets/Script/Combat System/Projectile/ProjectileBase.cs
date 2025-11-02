// ProjectileBase.cs
using UnityEngine;

/// <summary>
/// 纯标记基类：所有弹体类都应继承它，LevelRunner 能统一清场。
/// 不包含任何逻辑，避免干扰你现有的弹体实现。
/// </summary>
public abstract class ProjectileBase : MonoBehaviour {}