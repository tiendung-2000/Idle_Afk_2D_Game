using System;
using System.Collections;
using PixelImmortal.Core;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Enemies;
using UnityEngine;

namespace PixelImmortal.Combat
{
    public class PlayerUnit : MonoBehaviour
    {
        const float RetargetInterval = 0.15f;
        const float TracerSeconds = 0.06f;
        static readonly Color PlaceholderColor = new Color(0.35f, 0.7f, 1f);
        const float AttackPoseSeconds = 0.14f;
        const float HitPoseSeconds = 0.12f;

        public Health Health { get; } = new Health();
        public PlayerStats Stats { get; private set; }
        public bool IsAlive => Health.IsAlive;
        public float ShieldRemaining => Mathf.Max(0f, shieldUntil - Time.time);

        public event Action Died;
        public event Action Revived;

        PrototypeConfig config;
        PlayerProgress progress;
        EnemyRegistry registry;
        SpriteRenderer body;
        SpriteRenderer tracer;
        SpriteRenderer shieldRing;
        EnemyUnit target;
        Sprite[] frames;                 // idle0, idle1, attack, dead, hit (from the art catalog; hit is optional)
        Color aliveColor = PlaceholderColor;
        float attackPoseUntil, hitPoseUntil;
        float nextAttack, nextRetarget, invulnerableUntil, tracerOff, shieldUntil, shieldReduction;
        Coroutine respawn;

        public void Init(PrototypeConfig cfg, EnemyRegistry reg, PlayerProgress prog)
        {
            config = cfg;
            registry = reg;
            progress = prog;

            body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(transform, false);
            frames = config.art != null ? config.art.Frames("player") : null;
            if (frames != null && frames.Length >= 1)
            {
                body.sprite = frames[0];
                aliveColor = Color.white;
                body.transform.localScale = Vector3.one;
            }
            else
            {
                frames = null;
                body.sprite = PlaceholderSprites.Circle;
                body.transform.localScale = Vector3.one * 0.9f;
            }
            body.color = aliveColor;
            body.sortingOrder = 100;

            shieldRing = new GameObject("Shield").AddComponent<SpriteRenderer>();
            shieldRing.transform.SetParent(transform, false);
            shieldRing.sprite = PlaceholderSprites.Circle;
            shieldRing.color = new Color(0.6f, 0.85f, 1f, 0.35f);
            shieldRing.transform.localScale = Vector3.one * 1.4f;
            shieldRing.sortingOrder = 99;
            shieldRing.enabled = false;

            tracer = new GameObject("Tracer").AddComponent<SpriteRenderer>();
            tracer.transform.SetParent(transform, false);
            tracer.sprite = PlaceholderSprites.Bar;
            tracer.color = new Color(1f, 0.95f, 0.5f);
            tracer.sortingOrder = 200;
            tracer.enabled = false;

            var bar = new GameObject("HpBar").AddComponent<HealthBarView>();
            bar.transform.SetParent(transform, false);
            bar.Build(1.1f, 0.12f, -0.65f, new Color(0.3f, 0.9f, 0.4f));

            RecomputeStats();
            Health.Died += OnDied;
            Health.Reset(Stats.MaxHp);
            bar.Bind(Health);
            progress.StatsChanged += OnStatsChanged;
        }

        void OnDestroy()
        {
            if (progress != null) progress.StatsChanged -= OnStatsChanged;
        }

        void OnStatsChanged()
        {
            RecomputeStats();
            Health.SetMax(Stats.MaxHp);
        }

        void RecomputeStats() => Stats = PlayerStats.Compute(config, progress.Economy, progress);

        void Update()
        {
            if (config == null) return;
            if (tracer.enabled && Time.time >= tracerOff) tracer.enabled = false;
            shieldRing.enabled = IsAlive && Time.time < shieldUntil;
            Animate();
            if (!IsAlive) return;

            if (Time.time >= nextRetarget || target == null || !target.IsAlive)
            {
                nextRetarget = Time.time + RetargetInterval;
                target = registry.Nearest(transform.position, config.playerAttackRange);
            }

            if (target != null && Time.time >= nextAttack)
            {
                nextAttack = Time.time + Stats.AttackInterval;
                bool crit = Strike(target, 1f);
                AttackFired?.Invoke(crit);
                ShowTracer(target.transform.position);
                attackPoseUntil = Time.time + AttackPoseSeconds;
            }
        }

        /// <summary>Idle bob, attack pose, dead pose, and facing the current target. Placeholder circle has no frames.</summary>
        void Animate()
        {
            if (frames == null) return;
            Sprite s;
            if (!IsAlive) s = frames.Length > 3 ? frames[3] : frames[0];
            else if (Time.time < hitPoseUntil && frames.Length > 4) s = frames[4];
            else if (Time.time < attackPoseUntil && frames.Length > 2) s = frames[2];
            else s = frames[frames.Length > 1 ? (int)(Time.time * 2f) % 2 : 0];
            if (body.sprite != s) body.sprite = s;
            if (IsAlive && target != null) body.flipX = target.transform.position.x < transform.position.x;
        }

        /// <summary>Raised for each basic attack (argument: critical hit).</summary>
        public event Action<bool> AttackFired;

        /// <summary>Raised when the player actually loses HP (after reductions); argument is the amount.</summary>
        public event Action<float> Hurt;

        /// <summary>Attack x multiplier x crit (rolled once per hit). Used by basic attack and skills.</summary>
        public float RollHit(float multiplier, out bool critical)
        {
            critical = CombatMath.IsCrit(Stats.CritChance, UnityEngine.Random.value);
            return CombatMath.Damage(Stats.Attack, multiplier, 1f, critical ? Stats.CritMultiplier : 1f);
        }

        public float RollDamage(float multiplier) => RollHit(multiplier, out _);

        /// <summary>One landed hit on an enemy: roll, damage, hit flash. Returns whether it was a critical.</summary>
        public bool Strike(EnemyUnit enemy, float multiplier)
        {
            float damage = RollHit(multiplier, out bool critical);
            enemy.Health.TakeDamage(damage, critical);
            enemy.FlashHit();
            return critical;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || Time.time < invulnerableUntil) return;
            float reduction = Time.time < shieldUntil
                ? 1f - (1f - Stats.DamageReduction) * (1f - shieldReduction)
                : Stats.DamageReduction;
            float applied = amount * (1f - Mathf.Clamp01(reduction));
            Health.TakeDamage(applied);
            hitPoseUntil = Time.time + HitPoseSeconds;
            Hurt?.Invoke(applied);
        }

        public void ApplyShield(float reduction, float seconds)
        {
            shieldReduction = reduction;
            shieldUntil = Time.time + seconds;
        }

        /// <summary>Full heal and immediate revive; also cancels a pending timed respawn.</summary>
        public void ReviveNow()
        {
            if (respawn != null) { StopCoroutine(respawn); respawn = null; }
            bool wasDead = !IsAlive;
            Health.Reset(Stats.MaxHp);
            body.color = aliveColor;
            target = null;
            if (wasDead) Revived?.Invoke();
        }

        void OnDied()
        {
            body.color = new Color(0.25f, 0.25f, 0.3f);
            tracer.enabled = false;
            shieldUntil = 0f;
            Died?.Invoke();
            respawn = StartCoroutine(RespawnRoutine());
        }

        IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(config.respawnSeconds);
            respawn = null;
            ReviveNow();
            invulnerableUntil = Time.time + config.respawnInvulnerableSeconds;
        }

        void ShowTracer(Vector3 to)
        {
            Vector2 d = to - transform.position;
            tracer.transform.localPosition = Vector3.zero;
            tracer.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            tracer.transform.localScale = new Vector3(d.magnitude, 0.07f, 1f);
            tracer.enabled = true;
            tracerOff = Time.time + TracerSeconds;
        }
    }
}
