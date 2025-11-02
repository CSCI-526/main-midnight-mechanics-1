using UnityEngine;

namespace Game.Skills
{
    /// <summary>Utility helpers for skills.</summary>
    public static class SkillUtil
    {
        public static Enemy FindNearestEnemy(Vector3 origin)
        {
            Enemy best = null;
            float bestSqr = float.PositiveInfinity;
            foreach (var e in Enemy.All)
            {
                if (!e) continue;
                float d = (e.transform.position - origin).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = e; }
            }
            return best;
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float s = Mathf.Sin(rad);
            float c = Mathf.Cos(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// <summary>返回“朝上半圆（-90°~+90°，以 Vector2.up 为中轴）”的随机方向。</summary>
        public static Vector2 RandomUpwardDir()
        {
            float angle = Random.Range(-90f, 90f);
            return Rotate(Vector2.up, angle).normalized;
        }

        /// <summary>如果场上有敌人→朝最近敌人；否则→RandomUpwardDir。</summary>
        public static Vector2 AimDirOrRandomUp(Vector2 origin)
        {
            var n = FindNearestEnemy(origin);
            if (n)
            {
                Vector2 d = (Vector2)n.transform.position - origin;
                if (d.sqrMagnitude > 1e-6f) return d.normalized;
            }
            return RandomUpwardDir();
        }
    }
}