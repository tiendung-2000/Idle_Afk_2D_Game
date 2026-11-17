using System.Collections;
using System.Collections.Generic;
using PixelImmortal.Combat;
using PixelImmortal.Core;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Enemies;
using PixelImmortal.Save;
using UnityEngine;

namespace PixelImmortal.Progression
{
    /// <summary>
    /// Wave/boss/map state machine plus group spawning.
    /// Offline progression never touches this; it only runs during live play.
    /// </summary>
    public class BattleDirector : MonoBehaviour
    {
        PrototypeConfig config;
        PlayerUnit player;
        EnemyRegistry registry;
        ArenaView arena;
        IScreenFader fader;
        PlayerProgress progress;

        readonly Stack<EnemyUnit> pool = new Stack<EnemyUnit>();
        readonly List<EnemyUnit> scratch = new List<EnemyUnit>(32);
        Transform enemyRoot;
        Coroutine flow;
        bool bossFarming;
        bool bossDefeated;
        bool playerDownInBoss;
        bool pendingAdvance;

        public BattleState State { get; private set; }
        public int Chapter { get; private set; } = 1;
        public int Wave { get; private set; } = 1;
        public int Kills { get; private set; }
        public int EnemiesAlive => registry.Count;
        public EnemyUnit Boss { get; private set; }
        public int Cycle => ProgressionMath.CycleIndex(Chapter);
        public int MapNumber => ProgressionMath.MapIndex(Chapter);
        public int WavesPerMap => Map.waves.Length;
        public int BossesDefeated { get; private set; }
        public float RewardMultiplier => config.scaling.Reward(Chapter);
        WaveDefinition CurrentWave => Map.waves[Mathf.Clamp(Wave, 1, WavesPerMap) - 1];
        public MapDefinition[] Maps => config.maps;

        public MapDefinition Map => config.maps[(MapNumber - 1) % config.maps.Length];

        /// <summary>Raised at safe points (wave cleared, boss resolved, map changed). Used for autosave.</summary>
        public event System.Action Milestone;

        /// <summary>Live-play events that quests listen to. Never raised by offline processing.</summary>
        public event System.Action<EnemyUnit> EnemyKilled;
        public event System.Action WaveCleared;
        public event System.Action BossDefeatedEvent;
        public event System.Action BossLostEvent;
        public event System.Action MapChangedEvent;
        public event System.Action<EnemyUnit, float, bool> EnemyDamaged;

        public void Init(PrototypeConfig cfg, PlayerUnit p, EnemyRegistry reg, ArenaView arenaView, IScreenFader screenFader,
            PlayerProgress playerProgress, WorldData resume = null)
        {
            config = cfg;
            player = p;
            registry = reg;
            arena = arenaView;
            fader = screenFader;
            progress = playerProgress;
            enemyRoot = new GameObject("Enemies").transform;

            if (resume != null) ApplyWorld(resume);
            progress.NoteChapter(Chapter);

            player.Died += OnPlayerDied;
            arena.ApplyMap(Map);
            flow = StartCoroutine(MainLoop());
        }

        /// <summary>Restores a saved position. A saved boss fight resumes as BossReady (farming the last wave); never auto-starts the boss.</summary>
        void ApplyWorld(WorldData w)
        {
            Chapter = Mathf.Max(1, w.currentChapter);
            Wave = Mathf.Clamp(w.currentWave, 1, WavesPerMap);
            bossFarming = w.bossReady;
            if (bossFarming) Wave = WavesPerMap;
            Kills = Mathf.Max(0, w.kills);
            BossesDefeated = Mathf.Max(0, w.bossesDefeated);
        }

        /// <summary>Snapshot of where the player is. A mid-fight boss is saved as BossReady; a won boss as the next map's wave 1.</summary>
        public WorldData CaptureWorld()
        {
            bool advance = pendingAdvance;
            return new WorldData
            {
                currentChapter = advance ? Chapter + 1 : Chapter,
                currentWave = advance ? 1 : Wave,
                bossReady = !advance && (bossFarming || State == BattleState.BossCombat || State == BattleState.BossResult),
                kills = Kills,
                bossesDefeated = BossesDefeated
            };
        }

        void OnDestroy()
        {
            if (player != null) player.Died -= OnPlayerDied;
        }

        public void RequestBoss()
        {
            if (State != BattleState.BossReady) return;
            StopCoroutine(flow);
            ClearEnemies();
            flow = StartCoroutine(BossRoutine());
        }

        // ---- flow -------------------------------------------------------

        IEnumerator MainLoop()
        {
            while (true)
            {
                State = bossFarming ? BattleState.BossReady : BattleState.NormalWave;
                yield return WaveRoutine();
                WaveCleared?.Invoke();
                Milestone?.Invoke();

                if (Wave < WavesPerMap)
                {
                    State = BattleState.WaveTransition;
                    yield return new WaitForSeconds(config.waveTransitionSeconds);
                    Wave++;
                }
                else
                {
                    // Wave 10 done: BossReady. Keep farming wave 10 until the player starts the boss.
                    bossFarming = true;
                    State = BattleState.BossReady;
                    yield return new WaitForSeconds(config.waveTransitionSeconds);
                }
            }
        }

        IEnumerator WaveRoutine()
        {
            var def = CurrentWave;
            int groups = Mathf.Max(1, def.groupCount);
            int perGroup = Mathf.Max(1, Mathf.RoundToInt(def.enemiesPerGroup * config.scaling.Density(Chapter)));

            for (int g = 0; g < groups; g++)
            {
                bool finalGroup = g == groups - 1;
                for (int i = 0; i < perGroup; i++)
                {
                    while (registry.Count >= config.maxActiveEnemies) yield return null;
                    bool elite = finalGroup && i < def.eliteCountInFinalGroup;
                    SpawnEnemy(elite);
                    yield return new WaitForSeconds(config.spawnStaggerSeconds);
                }

                // Next group (or wave completion) only once every enemy of this group is dead.
                while (registry.Count > 0) yield return null;
                if (!finalGroup) yield return new WaitForSeconds(config.groupIntervalSeconds);
            }
        }

        IEnumerator BossRoutine()
        {
            State = BattleState.BossCombat;
            bossDefeated = false;
            playerDownInBoss = false;
            player.ReviveNow();
            Boss = SpawnBoss();
            Boss.Died += OnBossDied;

            while (!bossDefeated && !playerDownInBoss) yield return null;

            State = BattleState.BossResult;
            Boss.Died -= OnBossDied;
            yield return new WaitForSeconds(config.bossResultSeconds);

            if (bossDefeated)
            {
                BossesDefeated++;
                BossDefeatedEvent?.Invoke();
                pendingAdvance = true;
                Milestone?.Invoke();
                yield return MapTransition();
                bossFarming = false;
            }
            else
            {
                // Lost: back to farming wave 10, player decides when to retry.
                ClearEnemies();
                player.ReviveNow();
                bossFarming = true;
                Wave = WavesPerMap;
                BossLostEvent?.Invoke();
            }

            Boss = null;
            Milestone?.Invoke();
            flow = StartCoroutine(MainLoop());
        }

        IEnumerator MapTransition()
        {
            State = BattleState.MapTransition;
            yield return fader.FadeTo(1f, config.mapFadeSeconds);
            ClearEnemies();
            Chapter++;
            pendingAdvance = false;
            progress.NoteChapter(Chapter);
            Wave = 1;
            arena.ApplyMap(Map);
            fader.ShowMapTitle();
            MapChangedEvent?.Invoke();
            player.ReviveNow();
            yield return new WaitForSeconds(0.6f);
            yield return fader.FadeTo(0f, config.mapFadeSeconds);
        }

        void OnPlayerDied()
        {
            if (State == BattleState.BossCombat) playerDownInBoss = true;
        }

        void OnBossDied(EnemyUnit e) => bossDefeated = true;

        // ---- spawning -----------------------------------------------------

        EnemyVariant PickVariant()
        {
            var roster = Map.roster;
            if (roster == null || roster.Length == 0) return null;
            float total = 0f;
            for (int i = 0; i < roster.Length; i++) total += Mathf.Max(0f, roster[i].spawnWeight);
            if (total <= 0f) return roster[0];
            float r = Random.value * total;
            for (int i = 0; i < roster.Length; i++)
            {
                r -= Mathf.Max(0f, roster[i].spawnWeight);
                if (r <= 0f) return roster[i];
            }
            return roster[roster.Length - 1];
        }

        void SpawnEnemy(bool elite)
        {
            var v = PickVariant();
            float hpMul = v != null ? v.hpMultiplier : 1f;
            float atkMul = v != null ? v.attackMultiplier : 1f;
            float spdMul = v != null ? v.speedMultiplier : 1f;
            float offset = CurrentWave.difficultyOffset;
            // Elites use their own art when it exists (own 32 px sprite, so no gold tint or upscaling is needed).
            var eliteFrames = elite && v != null && config.art != null ? config.art.Frames("elite." + v.id) : null;
            var frames = eliteFrames ?? (v != null && config.art != null ? config.art.Frames("enemy." + v.id) : null);
            bool ownEliteArt = eliteFrames != null;
            Color baseColor = frames != null ? Color.white : (v != null ? v.color : Color.white);
            var p = new EnemySpawnParams
            {
                hp = config.enemyHp * hpMul * config.scaling.EnemyHp(Chapter, offset) * (elite ? config.eliteHpMultiplier : 1f),
                attack = config.enemyAttack * atkMul * config.scaling.EnemyAttack(Chapter, offset) * (elite ? config.eliteAttackMultiplier : 1f),
                attackInterval = config.enemyAttackInterval,
                moveSpeed = config.enemyMoveSpeed * spdMul,
                attackRange = config.enemyAttackRange,
                scale = elite ? config.eliteScale : (v != null ? v.scale : config.enemyScale),
                color = elite && !ownEliteArt ? Color.Lerp(baseColor, new Color(1f, 0.8f, 0.2f), frames != null ? 0.45f : 0.6f) : baseColor,
                isElite = elite,
                frames = frames,
                // The elite sprite is 32 px against 24 px for a normal monster, so shrink the multiplier to keep the same on-screen size.
                relativeScale = (elite ? config.eliteScale : (v != null ? v.scale : config.enemyScale)) / Mathf.Max(0.01f, config.enemyScale) * (ownEliteArt ? 0.75f : 1f)
            };
            AddFx(ref p);
            Acquire().Spawn(arena.RandomEdgePoint(player.transform.position, config.minSpawnDistanceFromPlayer), p, player, registry);
        }

        /// <summary>Combat feedback sprites (hit spark, death puff, spawn ring, boss telegraph ring); all optional.</summary>
        void AddFx(ref EnemySpawnParams p)
        {
            if (config.art == null) return;
            p.fxHit = config.art.First("fx.hit");
            p.fxDeath = config.art.First("fx.death");
            p.fxSpawn = config.art.First("fx.spawn");
            p.fxTelegraph = config.art.First("fx.telegraph");
        }

        EnemyUnit SpawnBoss()
        {
            // Boss keeps its base design per map; only cycle/map factors scale it.
            var bossFrames = config.art != null ? config.art.Frames("boss." + Map.mapId) : null;
            var p = new EnemySpawnParams
            {
                hp = config.bossHp * Map.bossHpMultiplier * config.scaling.BossHp(Chapter),
                attack = config.bossAttack * Map.bossAttackMultiplier * config.scaling.BossAttack(Chapter),
                attackInterval = config.bossAttackInterval,
                moveSpeed = config.bossMoveSpeed,
                attackRange = config.bossAttackRange,
                scale = config.bossScale,
                color = bossFrames != null ? Color.white : Map.bossColor,
                isBoss = true,
                frames = bossFrames,
                relativeScale = 1f,
                telegraphSeconds = config.bossTelegraphSeconds
            };
            AddFx(ref p);
            var e = Acquire();
            e.Spawn(arena.RandomEdgePoint(player.transform.position, config.minSpawnDistanceFromPlayer), p, player, registry);
            return e;
        }

        EnemyUnit Acquire()
        {
            if (pool.Count > 0)
            {
                var e = pool.Pop();
                e.gameObject.SetActive(true);
                return e;
            }
            var go = new GameObject("Enemy");
            go.transform.SetParent(enemyRoot, false);
            var unit = go.AddComponent<EnemyUnit>();
            unit.Died += OnEnemyDied;
            unit.Released += Release;
            unit.DamageTaken += (e, amount, crit) => EnemyDamaged?.Invoke(e, amount, crit);
            return unit;
        }

        void OnEnemyDied(EnemyUnit e)
        {
            Kills++;
            var eco = config.economy;
            var entry = e.IsBoss ? eco.bossKill : (e.IsElite ? eco.eliteKill : eco.normalKill);
            RewardMath.Grant(progress.Wallet, entry, RewardMultiplier);
            EnemyKilled?.Invoke(e);
        }

        void Release(EnemyUnit e)
        {
            e.gameObject.SetActive(false);
            pool.Push(e);
        }

        void ClearEnemies()
        {
            registry.CopyTo(scratch);
            for (int i = 0; i < scratch.Count; i++) scratch[i].Despawn();
            scratch.Clear();
        }
    }
}
