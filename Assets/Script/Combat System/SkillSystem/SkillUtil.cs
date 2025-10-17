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
    }
}