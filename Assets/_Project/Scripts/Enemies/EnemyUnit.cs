using System;
using System.Collections;
using PixelImmortal.Combat;
using PixelImmortal.Core;
using UnityEngine;

namespace PixelImmortal.Enemies
{
    public struct EnemySpawnParams
    {
        public float hp, attack, attackInterval, moveSpeed, attackRange, scale;
        public Color color;
        public bool isBoss, isElite;
        /// <summary>Animation frames from the art catalog; null = placeholder square.</summary>
        public Sprite[] frames;
        /// <summary>Size multiplier applied to art (1 = the sprite's natural size).</summary>
        public float relativeScale;
        /// <summary>Seconds of warning before a strike lands (bosses). 0 = the strike lands at once.</summary>
        public float telegraphSeconds;
        /// <summary>Optional combat feedback sprites: hit spark, death puff, spawn ring, telegraph ring.</summary>
        public Sprite fxHit, fxDeath, fxSpawn, fxTelegraph;
    }

    /// <summary>Lifecycle: Spawn -> MoveToTarget -> Attack -> Hit -> Death -> Despawn (pooled).</summary>
    public class EnemyUnit : MonoBehaviour
    {
        const float FlashSeconds = 0.12f;
        const float AttackPoseSeconds = 0.15f;
        const int WalkFrames = 2, AttackFrameIndex = 2, HitFrameIndex = 3, DeathFrameIndex = 4, SkillFrameIndex = 5;
        const float FxSeconds = 0.25f;
        static readonly Color TelegraphColor = new Color(1f, 0.25f, 0.2f);
        const float DeathFadeSeconds = 0.2f;
        const float AnimFps = 4f;
        const float PopSeconds = 0.18f;

        public Health Health { get; } = new Health();
        public bool IsAlive { get; private set; }
        public bool IsBoss { get; private set; }
        public bool IsElite { get; private set; }

        public event Action<EnemyUnit> Died;
        /// <summary>Every hit that lands on this enemy: (enemy, amount, critical).</summary>
        public event Action<EnemyUnit, float, bool> DamageTaken;
        public event Action<EnemyUnit> Released;

        SpriteRenderer body;
        HealthBarView bar;
        Combat.PlayerUnit target;
        EnemyRegistry registry;
        Color baseColor;
        float attack, attackInterval, moveSpeed, attackRange, nextAttack, flashUntil, attackPoseUntil, slowAmount, slowUntil;
        Sprite[] frames;
        bool hasArt;
        float baseScale, animOffset, spawnedAt;
        int frameIndex = -1;

        // Combat feedback: a transient effect (spawn ring / hit spark / death puff) and the boss telegraph ring.
        SpriteRenderer fx, telegraph;
        Sprite fxHit, fxDeath, fxSpawn;
        float fxStart, fxDuration, fxScale;
        float telegraphSeconds, windupEnd;   // windupEnd > 0 while a boss strike is being telegraphed

        void Awake()
        {
            body = new GameObject("Body").AddComponent<SpriteRenderer>();
            body.transform.SetParent(transform, false);
            body.sprite = PlaceholderSprites.Square;

            bar = new GameObject("HpBar").AddComponent<HealthBarView>();
            bar.transform.SetParent(transform, false);
            bar.Build(0.9f, 0.1f, 0f, new Color(0.95f, 0.3f, 0.3f));

            fx = new GameObject("Fx").AddComponent<SpriteRenderer>();
            fx.transform.SetParent(transform, false);
            fx.sortingOrder = 260;
            fx.enabled = false;
            telegraph = new GameObject("Telegraph").AddComponent<SpriteRenderer>();
            telegraph.transform.SetParent(transform, false);
            telegraph.sortingOrder = 1;
            telegraph.enabled = false;

            Health.Died += OnDied;
            Health.Damaged += (amount, crit) =>
            {
                DamageTaken?.Invoke(this, amount, crit);
                PlayFx(fxHit, Color.white, 0.5f, crit ? 1.1f : 0.8f);
            };
        }

        /// <summary>Starts the transient effect (it grows and fades). Does nothing without a sprite.</summary>
        void PlayFx(Sprite sprite, Color color, float fromScale, float toScale)
        {
            if (sprite == null) return;
            fx.sprite = sprite;
            fx.color = color;
            fx.enabled = true;
            fxStart = Time.time;
            fxDuration = FxSeconds;
            fxScale = toScale;
            fx.transform.localScale = Vector3.one * fromScale;
            fx.transform.localPosition = Vector3.zero;
            fxFrom = fromScale;
        }
        float fxFrom;

        void UpdateFx()
        {
            if (!fx.enabled) return;
            float k = (Time.time - fxStart) / fxDuration;
            if (k >= 1f) { fx.enabled = false; return; }
            fx.transform.localScale = Vector3.one * Mathf.Lerp(fxFrom, fxScale, k);
            var c = fx.color;
            c.a = 1f - k;
            fx.color = c;
        }

        void StartWindup()
        {
            windupEnd = Time.time + telegraphSeconds;
            if (telegraph.sprite != null) telegraph.enabled = true;
        }

        void EndWindup()
        {
            windupEnd = 0f;
            telegraph.enabled = false;
        }

        /// <summary>Ring on the ground that fills in over the wind-up, so the strike is readable before it lands.</summary>
        void UpdateTelegraph()
        {
            if (!telegraph.enabled) return;
            float k = Mathf.Clamp01(1f - (windupEnd - Time.time) / telegraphSeconds);
            float radius = attackRange * (1.1f - 0.1f * k);
            telegraph.transform.localScale = Vector3.one * radius * 2f / Mathf.Max(0.01f, telegraph.sprite.bounds.size.x) * (1f / Mathf.Max(0.01f, transform.lossyScale.x));
            var c = TelegraphColor;
            c.a = 0.35f + 0.55f * k + 0.1f * Mathf.Sin(Time.time * 30f);
            telegraph.color = c;
        }

        public void Spawn(Vector2 position, EnemySpawnParams p, Combat.PlayerUnit player, EnemyRegistry reg)
        {
            transform.position = position;
            target = player;
            registry = reg;
            attack = p.attack;
            attackInterval = p.attackInterval;
            moveSpeed = p.moveSpeed;
            attackRange = p.attackRange;
            IsBoss = p.isBoss;
            IsElite = p.isElite;
            baseColor = p.color;
            body.color = baseColor;
            frames = p.frames;
            hasArt = frames != null && frames.Length > 0;
            frameIndex = -1;
            body.flipX = false;
            if (hasArt) { body.sprite = frames[0]; baseScale = p.relativeScale > 0f ? p.relativeScale : 1f; }
            else { body.sprite = PlaceholderSprites.Square; baseScale = p.scale; }
            animOffset = UnityEngine.Random.value * 2f;
            spawnedAt = Time.time;
            body.transform.localScale = Vector3.one * baseScale * 0.6f;
            float height = hasArt ? body.sprite.bounds.size.y * baseScale : baseScale;
            bar.transform.localPosition = new Vector3(0f, height * 0.5f + 0.15f, 0f);
            bar.transform.localScale = Vector3.one * (p.isBoss ? 2f : 1f);
            Health.Reset(p.hp);
            bar.Bind(Health);
            nextAttack = Time.time + attackInterval * UnityEngine.Random.value;
            flashUntil = 0f;
            attackPoseUntil = 0f;
            slowUntil = 0f;
            slowAmount = 0f;
            telegraphSeconds = p.telegraphSeconds;
            EndWindup();
            telegraph.sprite = p.fxTelegraph;
            fxHit = p.fxHit; fxDeath = p.fxDeath; fxSpawn = p.fxSpawn;
            fx.enabled = false;
            PlayFx(fxSpawn, Color.white, 0.4f, 1.3f);
            IsAlive = true;
            registry.Add(this);
        }

        void Update()
        {
            UpdateFx();
            if (!IsAlive) return;

            body.color = Time.time < flashUntil ? Color.white : baseColor;
            body.sortingOrder = 50 - Mathf.RoundToInt(transform.position.y * 10f);
            Animate();

            if (target == null || !target.IsAlive)
            {
                if (windupEnd > 0f) EndWindup();
                return;
            }

            Vector2 to = (Vector2)target.transform.position - (Vector2)transform.position;

            // Boss strike: stand still and show the warning, then hit if the player is still close enough.
            if (windupEnd > 0f)
            {
                UpdateTelegraph();
                if (Time.time >= windupEnd)
                {
                    EndWindup();
                    nextAttack = Time.time + attackInterval;
                    attackPoseUntil = Time.time + AttackPoseSeconds;
                    if (to.magnitude <= attackRange * 1.25f) target.TakeDamage(attack);
                }
                return;
            }

            if (to.magnitude > attackRange)
            {
                float speed = Time.time < slowUntil ? moveSpeed * (1f - slowAmount) : moveSpeed;
                transform.position += (Vector3)(to.normalized * (speed * Time.deltaTime));
            }
            else if (Time.time >= nextAttack)
            {
                if (telegraphSeconds > 0f) { StartWindup(); return; }
                nextAttack = Time.time + attackInterval;
                attackPoseUntil = Time.time + AttackPoseSeconds;
                target.TakeDamage(attack);
            }
        }

        /// <summary>Walk-cycle frames, facing the player, and a short pop-in when spawned.</summary>
        void Animate()
        {
            float k = (Time.time - spawnedAt) / PopSeconds;
            if (k < 1f) body.transform.localScale = Vector3.one * baseScale * Mathf.Lerp(0.6f, 1f, k);
            else if (k < 1.5f) body.transform.localScale = Vector3.one * baseScale;

            if (!hasArt) return;
            if (frames.Length > 1)
            {
                // Frames: 0/1 walk cycle, 2 attack, 3 hit. Art with fewer frames just keeps walking.
                int walk = Mathf.Min(WalkFrames, frames.Length);
                int i;
                if (windupEnd > 0f && frames.Length > SkillFrameIndex) i = SkillFrameIndex;
                else if (frames.Length > HitFrameIndex && Time.time < flashUntil) i = HitFrameIndex;
                else if (frames.Length > AttackFrameIndex && Time.time < attackPoseUntil) i = AttackFrameIndex;
                else i = (int)((Time.time + animOffset) * AnimFps) % walk;
                if (i != frameIndex) { frameIndex = i; body.sprite = frames[i]; }
            }
            if (target != null) body.flipX = target.transform.position.x < transform.position.x;
        }

        public void FlashHit() => flashUntil = Time.time + FlashSeconds;

        /// <summary>Slows movement by the given fraction (0..1) until the time elapses; the stronger slow wins.</summary>
        public void ApplySlow(float amount, float seconds)
        {
            if (amount >= slowAmount || Time.time >= slowUntil) slowAmount = amount;
            slowUntil = Mathf.Max(slowUntil, Time.time + seconds);
        }

        void OnDied()
        {
            IsAlive = false;
            EndWindup();
            registry.Remove(this);
            Died?.Invoke(this);
            PlayFx(fxDeath, new Color(1f, 1f, 1f, 0.9f), 0.5f, 1.6f);
            StartCoroutine(DeathRoutine());
        }

        IEnumerator DeathRoutine()
        {
            if (hasArt && frames.Length > DeathFrameIndex) body.sprite = frames[DeathFrameIndex];
            float t = 0f;
            while (t < DeathFadeSeconds)
            {
                t += Time.deltaTime;
                var c = baseColor;
                c.a = 1f - t / DeathFadeSeconds;
                body.color = c;
                body.transform.localScale = Vector3.one * baseScale * (1f - 0.4f * t / DeathFadeSeconds);
                yield return null;
            }
            Released?.Invoke(this);
        }

        /// <summary>Remove without reward/death events (map change, boss reset).</summary>
        public void Despawn()
        {
            StopAllCoroutines();
            EndWindup();
            fx.enabled = false;
            IsAlive = false;
            registry.Remove(this);
            Released?.Invoke(this);
        }
    }
}
