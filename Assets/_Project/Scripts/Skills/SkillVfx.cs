using System.Collections.Generic;
using PixelImmortal.Core;
using PixelImmortal.Enemies;
using UnityEngine;

namespace PixelImmortal.Skills
{
    /// <summary>Pooled placeholder VFX: an expanding/fading disc, or a projectile that damages its target on arrival.</summary>
    public class SkillVfx : MonoBehaviour
    {
        const float MaxProjectileSeconds = 3f;
        const float HitDistance = 0.25f;

        SpriteRenderer sr;
        Color color;
        float t, duration, startScale, endScale;
        bool projectile;
        EnemyUnit projTarget;
        float projSpeed, projDamage, norm = 2f;
        bool projCrit;

        public bool InUse { get; private set; }

        void Awake()
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = PlaceholderSprites.Circle;
            sr.sortingOrder = 300;
            sr.enabled = false;
        }

        public void PlayDisc(Color c, Vector3 position, float fromScale, float toScale, float seconds, Sprite sprite = null)
        {
            sr.sprite = sprite != null ? sprite : PlaceholderSprites.Circle;
            Begin(c, position, fromScale, toScale, seconds);
            transform.rotation = Quaternion.identity;
            projectile = false;
        }

        public void PlayProjectile(Color c, Vector3 from, EnemyUnit target, float speed, float damage, float scale, Sprite sprite = null, bool critical = false)
        {
            sr.sprite = sprite != null ? sprite : PlaceholderSprites.Circle;
            Begin(c, from, scale, scale, MaxProjectileSeconds);
            projectile = true;
            projTarget = target;
            projSpeed = speed;
            projDamage = damage;
            projCrit = critical;
        }

        void Begin(Color c, Vector3 position, float fromScale, float toScale, float seconds)
        {
            // Normalise so "scale 1" means a disc of radius 1 whatever the sprite size is.
            norm = 2f / Mathf.Max(0.01f, sr.sprite.bounds.size.x);
            color = c;
            transform.position = position;
            startScale = fromScale;
            endScale = toScale;
            duration = Mathf.Max(0.01f, seconds);
            t = 0f;
            InUse = true;
            sr.color = c;
            sr.enabled = true;
            transform.localScale = Vector3.one * fromScale * norm;
        }

        void Update()
        {
            if (!InUse) return;
            t += Time.deltaTime;

            if (projectile)
            {
                if (projTarget == null || !projTarget.IsAlive || t >= duration) { End(); return; }
                Vector3 to = projTarget.transform.position;
                transform.position = Vector3.MoveTowards(transform.position, to, projSpeed * Time.deltaTime);
                if ((transform.position - to).sqrMagnitude <= HitDistance * HitDistance)
                {
                    projTarget.Health.TakeDamage(projDamage, projCrit);
                    projTarget.FlashHit();
                    End();
                }
                return;
            }

            float k = t / duration;
            if (k >= 1f) { End(); return; }
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, k) * norm;
            var c = color;
            c.a = color.a * (1f - k);
            sr.color = c;
        }

        void End()
        {
            InUse = false;
            projTarget = null;
            sr.enabled = false;
        }
    }

    /// <summary>Fixed-size pool. When exhausted, effects are skipped instead of growing without bound.</summary>
    public class SkillVfxPool
    {
        readonly List<SkillVfx> items = new List<SkillVfx>();

        public SkillVfxPool(Transform parent, int capacity)
        {
            for (int i = 0; i < capacity; i++)
            {
                var go = new GameObject("Vfx" + i);
                go.transform.SetParent(parent, false);
                items.Add(go.AddComponent<SkillVfx>());
            }
        }

        public SkillVfx Rent()
        {
            for (int i = 0; i < items.Count; i++) if (!items[i].InUse) return items[i];
            return null;
        }
    }
}
