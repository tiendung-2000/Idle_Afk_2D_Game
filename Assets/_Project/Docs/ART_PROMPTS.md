# Pixel Immortal - prompts and import for real art

The Unity image generator is not available here (NoSubscription), so the sprites in the game are procedural placeholders.
Generate real art with any external tool, then drop it in `Assets/_Project/ArtIncoming/` and run **PixelImmortal > Import External Art**.
Imported frames are listed in `Art/final_art.txt`, and *Generate Placeholder Pixel Art* will never overwrite them.

## File format (what the importer accepts)
- One PNG per character, **one horizontal strip of square frames**: frame size = image height, frame count = width / height.
  Example: a boss strip with 6 frames of 256 px is 1536 x 256.
- File name = `<kind>_<id>.png`: `player.png`, `enemy_<id>.png`, `elite_<id>.png`, `boss_<mapId>.png`.
- Transparent background is best. A flat single-colour background (no alpha) is keyed out using the top-left pixel.
- Frames are scaled with nearest-neighbour to the game size: player 28 px, enemy 24 px, elite 32 px, boss 56 px. Draw large and chunky; fine detail is lost.
- Character faces **right**. Feet at the bottom edge, same position in every frame (stable pivot).
- No text, UI, shadows or backgrounds baked in.

## Frame order
| Kind | Frames |
|---|---|
| `player` | 0 idle, 1 idle, 2 sword attack, 3 fallen (death), 4 hit |
| `enemy_<id>` | 0 walk, 1 walk, 2 attack, 3 hit, 4 death |
| `elite_<id>` | same as enemy (5 frames), bigger, gold crown/accessory and aura |
| `boss_<mapId>` | 0 walk/hover, 1 walk/hover, 2 attack, 3 hit, 4 death, 5 skill wind-up (arms raised, glowing: shown while the red ring warns the player) |

## Shared prompt prefix
"2D fantasy cultivation (xianxia) pixel art character sprite sheet, single horizontal row of N frames, equal square frames, side view facing right,
transparent background, 1 px dark outline, limited palette, light from the top, consistent pixel density, no text, no UI."

## Hero
- `player.png` (5 frames): Kiếm Tu, young sword cultivator, flowing long black hair with a blue hairband, layered robe in cyan, navy and silver with a jade sash,
  a straight sword with a cyan edge. Frames: idle, idle (breathing bob), sword slash with a cyan arc, lying fallen, flinching hit pose.

## Monsters (24 px frames, 5 each)
| id | Name | Prompt detail |
|---|---|---|
| `linh_slime` | Linh Slime | jade-green blob spirit, glowing core, two dot eyes |
| `yeu_lang` | Hắc Lang | low charcoal wolf, violet glowing eyes |
| `yeu_trung` | Ma Côn | insect with translucent wings, amber and dark brown |
| `tinh_linh_rung` | Tinh Linh Rừng | teal forest sprite with a leaf hat |
| `hoa_linh` | Hỏa Linh | fire spirit, orange flame body, yellow core |
| `thach_giap_thu` | Huyết Thiết | armoured iron brute, dark red cracks |
| `quy_nham` | Quỷ Nham | horned crimson and black rock fiend |
| `hoa_dieu` | Hỏa Điểu | small fire bird, wings up and down |
| `bang_lang` | Băng Lang | frost wolf, white-blue fur, cyan eyes |
| `tuyet_yeu` | Cương Thi | hopping jiangshi, official's hat with a yellow talisman, muted blue robe, arms out |
| `bang_giap_thu` | Băng Giáp Thú | ice-plated beast, pale blue plates |
| `han_hon` | Ngư Hồn | floating aquatic ghost, ice blue and cyan |

## Elites (32 px frames, 5 each; same ids as monsters)
Same design as the monster at 1.3x detail, plus a gold crown or one signature accessory, heavier build and a soft gold aura.
Guide ideas: Huyết Ma Sứ (red scythe, crimson aura), Cự Lang (armoured wolf), Ngọc Linh (jade crystals), Hồn Thú (violet spirit flames, big horns).

## Bosses (56 px frames, 6 each)
| mapId | Boss (GDD) | Visual (guide) |
|---|---|---|
| `thanh_truc_lam` | Thanh Trúc Yêu Vương | Thanh Long: azure serpentine dragon, cyan and turquoise, water and wind |
| `hoa_son_coc` | Viêm Sơn Ma Quân | Tử Ma Đế: black and crimson demon emperor with a crown, dark energy |
| `han_bang_bi_canh` | Hàn Băng Cổ Tôn | Bạch Hổ: white tiger with ice-blue stripes and frost aura |

## Not covered by the importer yet
Backdrops (256 x 320), UI chrome, resource icons, skill VFX, equipment / rarity / realm icons and the HUD portraits are single images; replace those PNGs
under `Assets/_Project/Art` directly (keep the file name) and add their paths to `Art/final_art.txt` so regeneration keeps them.
