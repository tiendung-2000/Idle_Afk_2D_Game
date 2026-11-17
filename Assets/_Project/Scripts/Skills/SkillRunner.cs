using System.Collections.Generic;
using PixelImmortal.Combat;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Enemies;
using PixelImmortal.Upgrades;
using UnityEngine;

namespace PixelImmortal.Skills
{
    /// <summary>Auto-casts every unlocked skill on cooldown. Pure logic; visuals are delegated to SkillVfxPool.</summary>
    public class SkillRunner : MonoBehaviour
    {
        const float ShieldTriggerRange = 3f;

        PlayerUnit player;
        EnemyRegistry registry;
        PlayerProgress progress;
        UpgradeSystem upgrades;
        SkillVfxPool vfx;
        float[] nextCast;

        /// <summary>Raised when a skill fires (sound effects listen to this).</summary>
        public event System.Action<SkillDefinition> Cast;
        ArtCatalog art;
        readonly List<EnemyUnit> buffer = new List<EnemyUnit>(32);

        public void Init(PlayerUnit p, EnemyRegistry reg, PlayerProgress prog, UpgradeSystem up, SkillVfxPool pool, ArtCatalog artCatalog = null)
        {
            art = artCatalog;
            player = p;
            registry = reg;
            progress = prog;
            upgrades = up;
            vfx = pool;
            nextCast = new float[prog.Economy.skills.Length];
        }

        void Update()
        {
            if (player == null || !player.IsAlive) return;
            var skills = progress.Economy.skills;
            for (int i = 0; i < skills.Length; i++)
            {
                var s = skills[i];
                if (s == null || !progress.IsSkillUnlocked(s) || Time.time < nextCast[i]) continue;
                if (TryCast(s))
                {
                    nextCast[i] = Time.time + s.cooldown;
                    Cast?.Invoke(s);
                }
            }
        }

        float Multiplier(SkillDefinition s) => upgrades.SkillMultiplier(s) * player.Stats.SkillDamageMultiplier;

        bool TryCast(SkillDefinition s)
        {
            Vector2 origin = player.transform.position;
            switch (s.kind)
            {
                case SkillKind.SingleTarget:
                {
                    var t = registry.Nearest(origin, s.range);
                    if (t == null) return false;
                    player.Strike(t, Multiplier(s));
                    Disc(s, t.transform.position, 0.2f, 0.7f);
                    return true;
                }
                case SkillKind.Projectile:
                {
                    var t = registry.Nearest(origin, s.range);
                    if (t == null) return false;
                    var v = vfx.Rent();
                    if (v == null) return false;
                    float dmg = player.RollHit(Multiplier(s), out bool crit);
                    v.PlayProjectile(Tint(s), origin, t, s.projectileSpeed, dmg, 0.3f, Art(s), crit);
                    return true;
                }
                case SkillKind.AreaAtTarget:
                {
                    var t = registry.Nearest(origin, s.range);
                    if (t == null) return false;
                    Vector2 center = t.transform.position;
                    HitArea(s, center, s.radius);
                    Disc(s, center, 0.3f, s.radius);
                    return true;
                }
                case SkillKind.AreaAroundPlayer:
                {
                    if (registry.Nearest(origin, s.range) == null) return false;
                    HitArea(s, origin, s.radius);
                    Disc(s, origin, 0.5f, s.radius);
                    return true;
                }
                case SkillKind.SelfShield:
                {
                    if (player.ShieldRemaining > 0f || registry.Nearest(origin, ShieldTriggerRange) == null) return false;
                    player.ApplyShield(s.shieldReduction, s.shieldSeconds);
                    Disc(s, origin, 0.6f, 1.2f);
                    return true;
                }
            }
            return false;
        }

        void HitArea(SkillDefinition s, Vector2 center, float radius)
        {
            registry.Within(center, radius, buffer);
            for (int i = 0; i < buffer.Count; i++)
            {
                var e = buffer[i];
                player.Strike(e, Multiplier(s));
                if (s.slowAmount > 0f && e.IsAlive) e.ApplySlow(s.slowAmount, s.slowSeconds);
            }
            buffer.Clear();
        }

        Sprite Art(SkillDefinition s) => art != null ? art.First("vfx." + s.id) : null;

        /// <summary>Real coloured effect art is shown as drawn; placeholder art takes the skill colour.</summary>
        Color Tint(SkillDefinition s) => art != null && art.IsFullColor("vfx." + s.id) ? Color.white : s.vfxColor;

        void Disc(SkillDefinition s, Vector3 pos, float from, float to)
        {
            var v = vfx.Rent();
            if (v != null) v.PlayDisc(Tint(s), pos, from, to, s.vfxSeconds, Art(s));
        }
    }
}
