# Pixel Immortal - Art manifest

## Status: placeholder art, generated procedurally
**No AI image tool was available (Unity's generator returned "no subscription"), so none of this art is AI-generated and none of it is final.**
Everything under `Assets/_Project/Art` was drawn by code (`Assets/_Project/Editor/PixelArtGenerator.cs`, menu
*PixelImmortal > Generate Placeholder Pixel Art*). It is deterministic: running it again reproduces the same PNGs and rebuilds the catalog.
It exists so the game is playable and readable while real art is made.

## How art is wired (the swap contract)
Gameplay asks the `ArtCatalog` asset (`ScriptableObjects/Economy/ArtCatalog.asset`, referenced by `PrototypeConfig.art`) for an id and gets sprites back.
A missing id is not an error: the game falls back to the original flat placeholder shapes (verified: with `art = null` the game runs unchanged).
So final art is dropped in by replacing PNGs (keep the file name) or by editing catalog entries. No gameplay or UI code changes.

Import rules are applied automatically to everything under `Assets/_Project/Art` (`PixelArtImportSettings`): Sprite, point filtering, no mipmaps,
no compression; **world art 32 px per unit, UI art 16 px per unit**; sliced UI sprites get their borders by file name.

## Catalog ids and sizes
| Id | Frames | Size (px) | Notes |
|---|---|---|---|
| `player` | 5: idle, idle, attack, dead, hit | 28 x 28 | faces right, flipped toward the target in code; hit shows for 0.12 s after taking damage (frame 4 is optional) |
| `enemy.<id>` (12) | 5: walk, walk, attack, hit, death | 24 x 24 | attack = forward lunge + white streak (0.15 s after each strike); hit = recoil + white flash (with the hit flash); death = collapsed, shown while it fades. Art with only 2 frames keeps walking |
| `elite.<id>` (12) | 5: same as enemy | 32 x 32 | own sprite (gold crown, side spikes, gold aura), used instead of the tinted/upscaled monster; falls back to `enemy.<id>` when missing |
| `fx.hit`, `fx.death`, `fx.spawn`, `fx.telegraph` | 1 each | 16 / 32 / 32 / 64 | near-white, tinted in code; played by `EnemyUnit` (hit spark, death puff, spawn ring, boss warning ring) |
| `icon.<skillId>` (5) | 1 | 64 x 64 | skill icon tile in the Skills screen | `linh_slime, yeu_lang, yeu_trung, tinh_linh_rung, hoa_linh, thach_giap_thu, quy_nham, hoa_dieu, bang_lang, tuyet_yeu, bang_giap_thu, han_hon` |
| `boss.<mapId>` (3) | 6: walk, walk, attack, hit, death, skill | 56 x 56 | `thanh_truc_lam, hoa_son_coc, han_bang_bi_canh`; frame 5 (arms raised, aura) is shown during the boss strike telegraph |
| `bg.<mapId>` (3) | 1 | 256 x 320 | exactly the 8 x 10 unit arena at 32 px per unit |
| `vfx.<skillId>` (5) | 1 | 16-64 | drawn near-white so `SkillDefinition.vfxColor` tints them |
| `ui.panel`, `ui.frame` | 1 | 24 x 24 | 9-slice, border 8; panel is tinted by the HUD colours, frame is an overlay ring |
| `ui.button`, `_pressed`, `_disabled` | 1 each | 16 x 16 | 9-slice, border 4 |
| `icon.linh_thach`, `icon.linh_khi`, `icon.tinh_hoa` | 1 | 16 x 16 | resource icons in the top bar |

Elites have their own 32 px sprite (the same pixel density as normal monsters), scaled by `eliteScale` x 0.75 so the on-screen size is unchanged.

**Boss telegraph:** a boss strike is no longer instant. When the boss is in range and its cooldown is ready it stops, shows frame 5 and a pulsing red ground ring for
`PrototypeConfig.bossTelegraphSeconds` (0.5 s, 0 disables it), then hits if the player is still within 1.25 x its attack range. Damage and cooldown values are unchanged.

**Imported from the user's concept sheets** (`Downloads/enemy_monster.png`, `boss.png`, `player.png`; cut automatically, background keyed out, then box-averaged down to game size):
- hero: idle, idle, attack (with sword-arc effect), fallen, hurt
- monsters: linh_slime = Yêu Linh, yeu_lang = Hắc Lang, yeu_trung = Ma Côn, thach_giap_thu = Huyết Thiết, quy_nham = Yêu Nữ, tuyet_yeu = Cương Thi,
  hoa_linh = Thiên Cẩu, bang_lang = the blue "special" wolf, han_hon = Ngư Hồn. Frames: Run[0], Run[3], Attack[3], Hurt[1], Die[last].
- bosses: thanh_truc_lam = Thanh Long, hoa_son_coc = Tử Ma Đế (skill frame = last attack frame).
Still placeholder (code-drawn): tinh_linh_rung, hoa_dieu, bang_giap_thu, boss han_bang_bi_canh (Bạch Hổ is white on the sheet's light card, so it cannot be keyed out), all elites, all VFX, icons and backdrops.
These are cut from concept art (not production sprites): frames are not pivot-aligned beyond bottom-centre and the walk cycle is two picked run frames.

**Skill effects and icons** (cut from `Downloads/skill_effect.png`, glow converted to alpha, drawn untinted: `ArtCatalog.Entry.fullColor`):
`skill_kiem_khi` = Kiếm Quang crescent, `skill_hoa_cau` = Hỏa Long fire crescent, `skill_bang_hoan` = Băng Hà crystal burst, `skill_ho_the` = Khiên bubble (status row),
`skill_van_kiem` = Lôi Kiếm lightning pillar. The sheet's other skills (Bão Phong, Hoa Vô Tận, Thần Hồn, Thiên Lôi, Phong Thần, Thiên Mệnh) have no matching GDD skill yet.
Each is one still frame, not an animation. The skill icons are these sprites on the generated 64 px tile.

**Real art:** see `ART_PROMPTS.md` and the *PixelImmortal > Import External Art* menu (`Assets/_Project/ArtIncoming`).

## Art guide pass (Pixel_Immortal_Character_Monster_Boss_Art_Guide.md)
Roster stays the GDD one; only palettes and themes were taken from the guide. Sprites are still the procedural placeholders, now recoloured:
| GDD id | Guide counterpart / palette |
|---|---|
| `player` | Kiếm Tu: cyan robe, navy shade, jade sash, long hair |
| `yeu_lang` | Hắc Lang: charcoal, violet eyes |
| `yeu_trung` | Ma Côn: amber, dark brown |
| `thach_giap_thu` | Huyết Thiết: iron, dark red |
| `quy_nham` | Yêu Nữ colours: crimson, black |
| `tuyet_yeu` | Cương Thi: muted blue, pale skin |
| `han_hon` | Ngư Hồn: ice blue, cyan |
| `boss.thanh_truc_lam` | Thanh Long: cyan, turquoise |
| `boss.hoa_son_coc` | Tử Ma Đế: black, crimson, gold crown |
| `boss.han_bang_bi_canh` | Bạch Hổ theme: white, ice blue (unchanged colours) |

**Portraits** (`portrait.player`, `portrait.boss.<mapId>`, 128 x 128, `Art/Portraits/`) are **cropped from the concept sheet**, not redrawn, and are scaled to 128 px with smoothing, so they are not pixel-exact.
The hero portrait still has the sheet's cream card background. They are shown on the Hero and Stage screens only. Source mapping: hero = Kiếm Tu,
Thanh Trúc Yêu Vương = Thanh Long, Viêm Sơn Ma Quân = Tử Ma Đế, Hàn Băng Cổ Tôn = Bạch Hổ. Mapping the guide's bosses onto the GDD's is a visual choice; revisit it if the GDD bosses get their own art.
Also done: hero given a sash tail, collar and glowing blade; Cương Thi (`tuyet_yeu`) has its own jiangshi silhouette (hat, talisman, outstretched arms).
Not done from the guide: the guide-only monsters (Cự Lang, Huyết Ma Sứ, Thiên Cẩu, ...), the other playable archetypes, equipment/rarity/realm icons and a breakthrough effect.
None of the above is real AI/hand-drawn art; it is placeholder pixel art generated by code.

## Not made yet (still placeholder or absent)
- Equipment and skill **icons** (items show text only), rarity icons, quest icons, realm icons.
- Hit / critical / spawn / death / breakthrough / map-transition effects beyond the generic flash, shrink-fade and title banner.
- Parallax, decorative arena props, animated backgrounds.
- Proper animation: attack and hit frames are derived from the walk frame (shift, streak, tint), not hand-drawn; no death frames (shrink-fade), no boss skill-attack frames.
- Music, and real sound effects: the ~16 effects are procedurally synthesised placeholders (`ProceduralSfx`); swap in recorded clips later.
- A pixel font for the UI (it uses Unity's built-in font); floating damage numbers already use a generated pixel digit font (`digit.*` in the ArtCatalog).

## Prompts for the real art (for an artist or an image tool)
Keep every sprite on a transparent background, no anti-aliasing, a 1 px dark outline, light from the top, and the same pixel density
(about 32 px per world unit; a normal enemy is about 24 px tall, a boss about 56 px).
- **Player**: young xianxia cultivator in a blue robe with a white sash, short black hair, holding a steel sword; 4 frames (idle A, idle B, sword-swing, fallen).
- **Enemies**, 2-frame walk each, 24 px: bamboo-forest set (jade slime, grey wolf, yellow-green beetle, leaf-hatted forest sprite); volcano set
  (fire spirit, stone-armoured beast, horned rock fiend, flame bird); ice set (frost wolf, snow demon, ice-plated beast, violet wraith).
- **Bosses**, 2 frames, 56 px: Bamboo Demon King (towering green humanoid with a bamboo crown); Flame Mountain Demon Lord (red-black horned
  fiend wreathed in fire); Ancient Frost Sovereign (spectral ice lord with a crystal crown).
- **Backdrops**, 256 x 320, top-down fixed arena, calm centre with decoration only near the edges: bamboo forest with moss and mist; black basalt with glowing
  lava cracks; frozen crystal floor with snow.
- **UI**: carved dark-wood / stone panel and gold-trimmed frame (9-slice), three-state beveled buttons, small resource icons (spirit stone, spirit qi, essence).
- **Effects**, near-white so they can be tinted: sword-qi slash arc, fireball, ice ring with shards, protective bubble, ring of ten thousand swords.
