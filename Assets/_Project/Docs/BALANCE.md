# Pixel Immortal - Balance notes (prototype)

All numbers are data in ScriptableObjects (`Assets/_Project/ScriptableObjects`). Nothing here is a hard requirement from the GDD except the offline rule (12 h cap, 80% efficiency).

## How it was measured
A scripted "sensible player" bot played the real game in Unity play mode, one `EditorApplication.Step()` per frame:
buys the cheapest affordable upgrade, saves for a breakthrough when its Linh Khi and chapter requirements are met,
equips the best owned items, claims quests, and challenges the boss after 20 policy ticks of farming Wave 10.
It does not use the gacha.

**Time base (corrected in Phase 7):** each stepped frame advances **0.02 s** of game time, not the 1/30 s that was assumed when the first
balance table was written. All times below are real game minutes of active play = frames x 0.02 / 60. (The earlier table was 1.67x too slow.)

## Targets (assumptions - change freely)
- Early maps take a few minutes each; the first bosses are winnable on the first try (onboarding).
- From cycle 3-4 on, bosses and normal waves push back (boss losses, player deaths) and maps slow down.
- A new realm every few minutes early, slower later; skills unlock as realms open.
- Offline pays about 80% of active income at the saved wave (GDD).

## Measured with the saved values (bot, real game minutes)
| Milestone | Time |
|---|---|
| Realm Luyen Khi 2 / 3 | 0.7 / 2.5 min |
| Realm Truc Co 1 / 2 / 3 | 6.4 / 11.1 / 18.2 min |
| Cycle 2 / 3 / 4 start | 7.3 / 16.1 / 25.3 min |
| Time per map | about 2.4 min early, rising to about 4 min by cycle 4 |
| Bosses beaten / tries | 9 / 9 (losses start to appear around cycle 3-4 with this bot) |
| Player deaths over 25 minutes | 5 |

Real wave clear time is about 16 s on average (including group gaps).

**This is faster than the 4-5 minutes per map first reported.** If a slower early game is wanted, raise `enemyHp` / `bossHp` in
`PrototypeConfig` or the upgrade `growthRate` values; re-run the simulation described above to check the pacing.

## Current values
- Enemy base: 40 HP, 6 attack. Boss base: 1200 HP, 20 attack (x per-map multipliers 1.0 / 1.4 / 1.8 HP).
- Scaling (`CycleScalingConfig`): HP and attack factor = 1 + 1.5(c-1) + 0.8 ln(c) per cycle, map step +0.35, wave offset +0.035 per wave.
  Invariants checked by tests: factor never drops between waves, maps or cycles (needs map step >= max wave offset, cycle a >= 2 x map step + max wave offset).
- Upgrades (base cost, growth): attack 25 / 1.20, HP 30 / 1.20, speed 50 / 1.26, skill 80 / 1.22, offline 120 / 1.30, crit 60 / 1.22.
- Realm costs (Linh Khi / Linh Thach): 100/200, 200/450, 400/1000, 800/2000, 1500/4000, 2800/8000, 5000/16000, 9000/32000.
- Drops: normal 1%, elite 10%, boss 100%. Gacha: 400 Linh Thach per pull, rates 55 / 28 / 12 / 4 / 1%.
- Offline: `estimatedClearSeconds` = 10.4 on every wave. This is **calibrated to measured income** (active income at chapters 7-10 is about
  22 Linh Thach/s, and the density-free offline model at 100% gives about 21-27), not to wall-clock clear time (about 16 s), because the offline model
  deliberately ignores the extra enemies per group at higher cycles. With it, 12 h at 80% pays about 276k Linh Thach at chapter 1,
  520k at chapter 4, 730k at chapter 7 and 925k at chapter 10. Exponential upgrade costs turn that into a bounded number of extra levels
  rather than skipping content, and chapter-gated realms and bosses still require live play. It is still a large jump early on; if it feels too strong,
  lower the offline efficiency upgrades or raise upgrade `growthRate` rather than breaking the 80% rule.

## Known limits
- The bot never uses the gacha, never rests, and retries bosses on a fixed timer, so a human will differ.
- Gacha and drop value (items vs upgrades) was reasoned about, not simulated.
- Quest rewards (about 670 Linh Thach per day) are small next to income and untuned.
- Pacing past cycle 4 and any long-term (hours) curve are unmeasured.
