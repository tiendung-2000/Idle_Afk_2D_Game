using System.Collections.Generic;
using UnityEngine;

namespace PixelImmortal.Enemies
{
    /// <summary>Live enemy list used for targeting. Dead enemies are removed on death.</summary>
    public class EnemyRegistry
    {
        readonly List<EnemyUnit> active = new List<EnemyUnit>(32);

        public int Count => active.Count;

        public void Add(EnemyUnit e) { if (!active.Contains(e)) active.Add(e); }
        public void Remove(EnemyUnit e) => active.Remove(e);
        public void CopyTo(List<EnemyUnit> buffer) { buffer.Clear(); buffer.AddRange(active); }

        /// <summary>Fills buffer with live enemies within radius (no allocation if buffer has capacity).</summary>
        public void Within(Vector2 center, float radius, System.Collections.Generic.List<EnemyUnit> buffer)
        {
            buffer.Clear();
            float sqr = radius * radius;
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || !e.IsAlive) continue;
                if (((Vector2)e.transform.position - center).sqrMagnitude <= sqr) buffer.Add(e);
            }
        }

        public EnemyUnit Nearest(Vector2 from, float maxRange)
        {
            EnemyUnit best = null;
            float bestSqr = maxRange * maxRange;
            for (int i = 0; i < active.Count; i++)
            {
                var e = active[i];
                if (e == null || !e.IsAlive) continue;
                float d = ((Vector2)e.transform.position - from).sqrMagnitude;
                if (d <= bestSqr) { bestSqr = d; best = e; }
            }
            return best;
        }
    }
}
