# PIXEL IMMORTAL
## Game Design Document (GDD) — Claude Code Implementation Specification

**Phiên bản:** 2.0 — Tổng hợp sau 6 nhóm khảo sát  
**Thể loại:** 2D Idle / AFK RPG, tu tiên, chiến đấu tự động  
**Nền tảng mục tiêu:** Android trước, kiến trúc sẵn sàng mở rộng iOS  
**Engine:** Unity 2D — phải xác minh phiên bản thực tế của project  
**Định hướng:** Offline-first, single-player, pixel art chi tiết  
**Tên dự án:** Pixel Immortal — tên tạm thời

---

# 1. DECISION LOG — CÁC QUYẾT ĐỊNH ĐÃ CHỐT

## Nhóm 1 — Gameplay và tiến trình
- Wave thường tự động chuyển ngay khi hoàn thành.
- Mỗi Wave gồm nhiều đợt quái; phải tiêu diệt hết đợt hiện tại mới spawn đợt tiếp theo.
- Đợt cuối có quái tinh anh hoặc điều kiện đặc biệt.
- Mỗi Map có đúng 10 Wave thường và 1 Boss Wave.
- Khi nhân vật chết, hồi sinh và tiếp tục Wave hiện tại.
- Thua Boss: quay về farm Wave 10; người chơi chủ động bấm để thử Boss lại.
- Sang Cycle mới giữ toàn bộ sức mạnh và tài nguyên.
- Chuyển Map bằng hiệu ứng chuyển cảnh ngắn.

## Nhóm 2 — Chiến đấu, quái vật và kỹ năng
- Kỹ năng tự động kích hoạt hoàn toàn.
- Ưu tiên tấn công quái gần nhất.
- Quái xuất hiện theo nhóm nhỏ, cách nhau một khoảng thời gian.
- Chiến đấu thời gian thực trong Unity.

## Nhóm 3 — Tiến trình, Cycle và Boss
- Độ khó tăng theo đường cong có kiểm soát.
- Theo Cycle tăng: HP quái/Boss, sát thương quái/Boss, số lượng/mật độ quái và phần thưởng.
- Cycle vô hạn.
- Giữ Boss gốc của mỗi Map; tăng sức mạnh theo Cycle, không tự sinh biến thể.
- Thắng Boss: tự động chuyển Map/Cycle tiếp theo.
- Thua Boss: farm Wave 10, bấm nút để thử lại.

## Nhóm 4 — Tài nguyên, nâng cấp, tu luyện và kỹ năng
Ba tài nguyên:
1. **Linh Thạch:** tiền tệ chính cho nâng cấp.
2. **Linh Khí:** tích lũy tu luyện và đột phá.
3. **Tinh Hoa:** nâng cấp kỹ năng.

Sáu nhóm nâng cấp:
- Công kích cơ bản.
- HP và sinh tồn.
- Tốc độ đánh.
- Sát thương kỹ năng.
- Hiệu suất AFK/offline.
- Chí mạng.

Quy tắc:
- Giá nâng cấp tăng theo đường cong.
- Đột phá cần Linh Khí và tài nguyên; người chơi bấm xác nhận.
- Kỹ năng mở theo tiến trình và dùng tài nguyên để nâng cấp.
- Có trang bị và gacha.
- Tái sinh/Prestige là tùy chọn, không bắt buộc.

## Nhóm 5 — AFK, nhiệm vụ và lưu dữ liệu
- Offline tối đa 12 giờ; hiệu suất thưởng cơ sở 80%.
- Chỉ farm tại Wave thường đã lưu.
- Offline không tự chuyển Wave/Map, không thắng Boss.
- Nhiệm vụ không tăng tiến độ khi offline.
- Nhiệm vụ ngày làm mới lúc 00:00 theo giờ thiết bị.
- Thưởng nhiệm vụ chưa nhận hết hạn theo ngày.
- Lưu cục bộ, tự động lưu, có backup và phục hồi.

## Nhóm 6 — Đồ họa, UI và asset
- Pixel art chi tiết, nhiều lớp màu và hiệu ứng.
- Nhân vật/quái theo tỷ lệ RPG pixel cân đối.
- UI fantasy pixel, khung gỗ/đá và biểu tượng tiên hiệp.
- Tiếng Việt và tiếng Anh; localization ngay từ đầu.
- Làm prototype bằng placeholder trước, thay asset hoàn chỉnh sau.
- Đấu trường cố định; HUD ngoài khu vực chiến đấu.
- Giữ Pixel Immortal làm tên tạm.

## Thông số mặc định — chưa phải yêu cầu cứng
Các hệ số cân bằng, cooldown, chi phí, tỷ lệ rơi/gacha, thời gian hồi sinh, số quái tối đa, kích thước sprite và giới hạn cấp phải cấu hình được, không hard-code. Có thể dùng UI reference 270×480 làm điểm bắt đầu nhưng phải kiểm tra các tỷ lệ màn hình dọc khác.

**Các điểm cần giữ nhất quán**
1. Cycle không phải Prestige. Cycle tăng tự động sau Boss Map 3 và không reset tài nguyên; Prestige là cơ chế riêng, tự nguyện.
2. Offline không làm tăng tiến trình Wave, thắng Boss hoặc mở Map.
3. Wave chỉ hoàn thành khi toàn bộ nhóm quái cần thiết đã bị tiêu diệt và điều kiện đặc biệt đã đạt.
4. Trang bị và gacha thuộc thiết kế tổng thể, nhưng có thể triển khai ở phase sau.
5. Prestige phải có màn xác nhận và thông báo rõ dữ liệu nào reset/được giữ.
6. Lỗi tải asset không được làm mất save.

---

# 2. TẦM NHÌN VÀ GAMEPLAY CORE

Pixel Immortal là game 2D Idle/AFK RPG tu tiên. Nhân vật tự động chiến đấu trước các nhóm yêu quái. Người chơi nhận tài nguyên, nâng cấp chỉ số, tu luyện, mở kỹ năng, thu thập trang bị và gacha. Người chơi có thể thoát game và nhận thưởng offline khi quay lại.

Vòng lặp:
1. Nhân vật tự chiến đấu.
2. Quái spawn theo từng nhóm.
3. Hoàn thành Wave và tự động tiến đến Wave kế.
4. Nhận Linh Thạch, Linh Khí, Tinh Hoa và vật phẩm.
5. Nâng cấp, tu luyện và cải thiện trang bị.
6. Thử Boss cuối Map.
7. Nếu thua, farm Wave 10 và tự chọn thời điểm thử lại.
8. Sau Boss Map 3, bắt đầu Cycle mới với độ khó cao hơn.
9. Offline farming được tính theo giới hạn và hiệu suất đã chốt.

MVP không mặc định gồm multiplayer, PvP, backend bắt buộc, guild/chat, quảng cáo/IAP hoặc cloud save bắt buộc.

---

# 3. MAP, WAVE VÀ CYCLE

## Ba Map

### Map 1 — Thanh Trúc Lâm
Bối cảnh rừng trúc, đá rêu, ánh sáng xanh và sương nhẹ. Quái: Linh Slime, Yêu Lang, Yêu Trùng, Tinh Linh Rừng. Boss: **Thanh Trúc Yêu Vương**.

### Map 2 — Hỏa Sơn Cốc
Bối cảnh núi lửa, đá đen, dung nham và tro bụi. Quái: Hỏa Linh, Thạch Giáp Thú, Quỷ Nham, Hỏa Điểu. Boss: **Viêm Sơn Ma Quân**.

### Map 3 — Hàn Băng Bí Cảnh
Bối cảnh băng tuyết, tinh thể và sương lạnh. Quái: Băng Lang, Tuyết Yêu, Băng Giáp Thú, Hàn Hồn. Boss: **Hàn Băng Cổ Tôn**.

Mỗi Map có 10 Wave thường và một Boss Wave. Roster và tỉ lệ xuất hiện phải cấu hình bằng dữ liệu.

## Đánh số tiến trình
Tách biệt:
- `mapId`: chủ đề môi trường và roster quái.
- `chapterIndex`: thứ tự tiến trình tổng thể.
- `cycleIndex`: vòng lặp ba Map.

Công thức:
- `cycleIndex = floor((chapterIndex - 1) / 3) + 1`
- `mapIndex = ((chapterIndex - 1) % 3) + 1`

Ví dụ: Chapter 1–3 là Cycle 1; Chapter 4–6 là Cycle 2; Chapter 7–9 là Cycle 3. Map nội dung lần lượt lặp lại Thanh Trúc Lâm, Hỏa Sơn Cốc, Hàn Băng Bí Cảnh. Tiến trình vô hạn.

## State machine
Các trạng thái đề xuất:
- `NormalWave`
- `WaveTransition`
- `BossReady`
- `BossCombat`
- `BossResult`
- `MapTransition`

Luồng:
1. Vào Wave thường và spawn nhóm đầu.
2. Chỉ spawn nhóm kế tiếp khi nhóm hiện tại bị tiêu diệt hết.
3. Kiểm tra điều kiện đặc biệt của nhóm cuối.
4. Hoàn thành Wave thì chuyển ngay sang Wave thường kế tiếp.
5. Sau Wave 10, chuyển sang `BossReady`.
6. Boss chỉ bắt đầu khi người chơi bấm nút.
7. Thắng Boss: hiệu ứng chuyển Map, cập nhật Chapter/Cycle.
8. Thua Boss: trở về Wave 10 để farm; không tự retry.
9. Thắng Boss Map 3: tăng Cycle và quay lại Map 1.

Lưu và tải game phải khôi phục trạng thái chính xác; thời gian offline không được tự thắng Boss.

## Định nghĩa Wave
Mỗi Wave data-driven cần có:
- Danh sách nhóm quái và thành phần mỗi nhóm.
- Spawn delay.
- Điều kiện hoàn thành.
- Elite/special condition.
- Reward table.
- Hệ số độ khó.
- Giới hạn thời gian nếu Wave có điều kiện sinh tồn.

Wave mặc định hoàn thành khi mọi nhóm cần thiết đã spawn, toàn bộ quái mục tiêu đã chết và điều kiện đặc biệt đạt.

---

# 4. CHIẾN ĐẤU VÀ KỸ NĂNG

## Arena
- Đấu trường cố định, nhân vật ở trung tâm hoặc gần trung tâm.
- Quái spawn quanh khu vực chiến đấu và di chuyển tiếp cận.
- HUD nằm ngoài arena.
- Camera không cần cuộn trong MVP.
- Tránh spawn quái trong vùng va chạm nhân vật.
- Tọa độ spawn phải thích ứng theo bounds và safe area, không hard-code cho một độ phân giải.

## Vòng đời quái
`Spawn → MoveToTarget → Attack → Hit → Death → Despawn`

Quái cần cooldown tấn công, tránh hit nhiều lần do va chạm, loại khỏi target list khi chết, không giữ reference đã bị hủy và có thể dùng object pooling.

## Target selection
Mục tiêu mặc định là quái gần nhất. Bỏ qua mục tiêu chết/despawn/không hợp lệ. Chỉ tìm lại mục tiêu khi cần; tránh quét toàn bộ quái mỗi frame.

## Chỉ số
Nhân vật: Max HP, Current HP, Attack, Attack Speed, Critical Chance, Critical Damage Multiplier, Skill Damage Multiplier, Damage Reduction, Movement Speed, Respawn Duration và Offline Efficiency.

Quái: HP, Attack, Defense nếu dùng, Attack Interval, Movement Speed, Reward Table, Elite Modifier và resistance nếu cần mở rộng.

Boss: HP cao, bộ kỹ năng/attack pattern riêng, cooldown và reward table riêng. Boss giữ thiết kế gốc qua các Cycle.

## Công thức sát thương khởi tạo
`RawDamage = Attack × SkillMultiplier × CycleModifier × CritModifier`

Nếu dùng phòng thủ:
`FinalDamage = max(MinDamage, RawDamage - Defense × DefenseFactor)`

Chỉ áp dụng mỗi hệ số đúng một lần. Đưa công thức vào hàm có thể test và kiểm tra overflow/NaN.

## Chết và hồi sinh
Khi HP về 0, ngừng chiến đấu, chạy hiệu ứng chết, chờ thời gian cấu hình, hồi sinh với HP cấu hình, có thể có thời gian bất tử ngắn và tiếp tục Wave hiện tại. Không reset tiến trình đợt quái hoặc mất tài nguyên đã nhận.

## Năm kỹ năng MVP
1. **Kiếm Khí:** sát thương đơn mục tiêu, thường nhắm quái gần nhất.
2. **Hỏa Cầu:** projectile bay đến mục tiêu và gây sát thương khi va chạm.
3. **Băng Hoàn:** AoE, gây sát thương và có thể làm chậm.
4. **Hộ Thể Linh Khí:** tạo khiên hoặc giảm sát thương trong thời gian nhất định.
5. **Vạn Kiếm Quy Tông:** AoE diện rộng, sát thương mạnh.

Tất cả tự động kích hoạt. Mỗi skill có cooldown, targeting rule, multiplier, range/AoE, VFX, điều kiện mở khóa và cấu hình nâng cấp. Không tạo projectile/VFX vô hạn; dùng pooling. Tách logic skill khỏi animation.

---

# 5. TU LUYỆN, NÂNG CẤP, TRANG BỊ VÀ GACHA

## Cảnh giới khởi đầu
Mặc định cấu hình: Luyện Khí 1–3, Trúc Cơ 1–3, Kim Đan 1–3. Phải mở rộng được bằng data.

## Đột phá
Điều kiện có thể gồm đủ Linh Khí, Linh Thạch/vật liệu và yêu cầu tiến trình. UI hiển thị cảnh giới hiện tại, cảnh giới kế, tài nguyên cần và điều kiện thiếu. Khi bấm:
1. Kiểm tra điều kiện trong logic nghiệp vụ.
2. Trừ tài nguyên.
3. Cập nhật cảnh giới.
4. Áp dụng chỉ số/mở khóa.
5. Lưu dữ liệu.

Không dựa riêng vào trạng thái nút UI để bảo vệ tài nguyên.

## Sáu nhóm nâng cấp
- Công kích cơ bản.
- HP và sinh tồn.
- Tốc độ đánh.
- Sát thương kỹ năng.
- Hiệu suất AFK/offline.
- Chí mạng.

Mỗi upgrade có stable ID, localization, level, effect formula, cost curve, loại tài nguyên, điều kiện mở khóa và giới hạn cấp nếu có.

Công thức giá mẫu:
`Cost(level) = baseCost × growthRate ^ level`

Đây chỉ là công thức khởi tạo. Kiểm tra overflow, số âm, NaN và cấp quá cao; cân nhắc `double` hoặc mô hình số lớn hơn.

## Trang bị
MVP có thể bắt đầu với vũ khí, giáp và phụ kiện. Mỗi item cần ID, rarity, slot, base stats, affix nếu có, drop source, yêu cầu cấp nếu dùng, localization và icon. Hỗ trợ inventory, trang bị/tháo và so sánh chỉ số. Lưu item bằng ID/dữ liệu, không lưu GameObject reference.

## Gacha
Dùng `GachaPoolDefinition` chứa pool ID, entries, weights/probabilities, cost, rewards và quy tắc đã cấu hình. Kiểm tra đủ tài nguyên, trừ phí, tạo kết quả và ghi inventory một cách an toàn. Không tự thêm giao dịch tiền thật, quảng cáo thưởng, backend, pity hoặc banner thời gian nếu chưa được xác nhận.

## Prestige tùy chọn
Prestige tách biệt Cycle. Không tự động kích hoạt. Trước khi reset phải hiển thị rõ dữ liệu giữ/reset và phần thưởng, yêu cầu xác nhận, tạo backup, áp dụng reset an toàn và lưu Prestige count. Quy tắc reset chi tiết phải cấu hình; không tự xóa tài nguyên hoặc trang bị nếu chưa định nghĩa.

---

# 6. ĐỘ KHÓ CYCLE

Cycle tăng:
- HP quái/Boss.
- Sát thương quái/Boss.
- Số lượng/mật độ quái.
- Phần thưởng.

Độ khó dùng đường cong có kiểm soát; không mặc định nhân cấp số nhân thuần túy. Ví dụ để thử nghiệm:
`cycleFactor(c) = 1 + a × c + b × log(1 + c)`

Các hệ số phải cấu hình và kiểm thử qua nhiều Cycle. Không tự tăng defense hoặc elite chance theo Cycle vì chưa được chốt. Tránh giảm độ khó bất ngờ, overflow, phần thưởng mất cân bằng hoặc áp dụng multiplier nhiều lần.

---

# 7. AFK/OFFLINE

- Giới hạn thời gian tính thưởng: 12 giờ.
- Hiệu suất cơ sở: 80%.
- Chỉ tính tại Wave thường đã lưu.
- Không chuyển Wave/Map, không thắng Boss, không tăng quest progress.
- Hiển thị màn Offline Rewards trước khi claim.

Công thức:
`effectiveOfflineSeconds = min(validElapsedSeconds, 12 × 3600)`
`offlineReward = baseRewardPerSecond × effectiveOfflineSeconds × 0.80`

Tính riêng mỗi loại tài nguyên dựa trên reward data của Wave. Nâng cấp offline efficiency có thể tăng hiệu suất nhưng phải có giới hạn cấu hình.

Chống lỗi:
- Không chấp nhận elapsed âm.
- Không vượt 12 giờ.
- Không claim hai lần cho cùng một khoảng.
- Cập nhật timestamp claim an toàn.
- Khi save/timestamp lỗi, phục hồi an toàn.
- Không yêu cầu backend trong MVP.
- Có thể lưu UTC timestamp; nhiệm vụ ngày vẫn reset theo giờ thiết bị.
- Ghi nhận rõ rằng không có server thì không thể chống gian lận đồng hồ hệ thống tuyệt đối.

---

# 8. NHIỆM VỤ

Ví dụ: tiêu diệt N quái, thu N Linh Thạch, nhận N Linh Khí, nâng cấp N lần, hoàn thành N Wave, đánh bại Boss.

Mỗi quest có ID, objective type, target count, current progress, reward, ngày reset, trạng thái hoàn thành/claim và localization keys.

- Quest chỉ tiến triển khi chơi trực tiếp.
- Reset lúc 00:00 theo giờ thiết bị.
- Quest chưa hoàn thành hết hạn theo ngày.
- Thưởng chưa nhận cũng hết hạn theo ngày.
- Cảnh báo trước khi rời màn hình nếu có thưởng sắp mất.
- Claim phải idempotent, không nhận hai lần.
- Lưu ngày reset gần nhất để tránh reset lặp.
- Vì dùng giờ thiết bị, không thể đảm bảo chống gian lận thời gian tuyệt đối khi không có server.

---

# 9. SAVE, BACKUP VÀ PHỤC HỒI

Save cần chứa:
- Schema version.
- Chapter/Cycle/Map/Wave và progression state.
- Boss status và highest progression.
- Resources.
- Character stats, upgrade levels, cultivation.
- Skills unlocked/levels.
- Inventory/equipment.
- Gacha state nếu đã triển khai.
- Quest progress/claim state.
- Prestige data nếu có.
- Offline timestamp.
- Gameplay settings cần lưu.

Không serialize trực tiếp Scene, Transform, GameObject hoặc ScriptableObject reference thành progression data. ScriptableObject lưu cấu hình tĩnh; Save DTO lưu dữ liệu người chơi.

Yêu cầu:
- Tự động lưu tại các mốc an toàn.
- Có file chính và backup.
- Ghi file theo cách giảm rủi ro file ghi dở.
- Validate trước khi áp dụng.
- Nếu file chính lỗi, thử backup.
- Nếu cả hai lỗi, thông báo và đưa lựa chọn khởi tạo save mới; không âm thầm ghi đè.
- Có schema version migration.
- Test file thiếu, JSON lỗi, field thiếu, version cũ, tài nguyên âm và index không hợp lệ.

---

# 10. UI/UX VÀ LOCALIZATION

## Battle Screen
- Arena cố định.
- Nhân vật/quái trong vùng chiến đấu.
- HUD ở ngoài arena.
- Chapter, Cycle, Map, Wave.
- HP nhân vật và HP Boss khi đánh Boss.
- Ba tài nguyên.
- Nút nâng cấp, tu luyện, trang bị, gacha, nhiệm vụ.
- Nút thử Boss chỉ hiện khi Boss sẵn sàng.
- Hiệu ứng chuyển Map ngắn.

## Các màn hình khác
- Upgrade: cấp, chỉ số hiện tại/sau nâng cấp, chi phí, nút nâng.
- Cultivation: cảnh giới, Linh Khí, chi phí/điều kiện đột phá.
- Inventory: trang bị, slot, rarity, chỉ số và so sánh.
- Gacha: pool, chi phí, tỷ lệ cấu hình, kết quả.
- Quest: tiến độ, phần thưởng, trạng thái claim/hết hạn.
- Offline Rewards: thời gian được tính, tài nguyên, hiệu suất và nút claim; không có hành động vượt Wave/Boss.

## Responsive UI
- Không phụ thuộc duy nhất vào 270×480.
- Dùng Canvas Scaler/layout phù hợp.
- Kiểm tra safe area và nhiều tỷ lệ màn hình dọc.
- Chữ dễ đọc, HUD không che combat.
- Giữ pixel edges sắc nét.
- Không vẽ text cố định vào UI sprite cần localization.

## Localization
- Tiếng Việt và tiếng Anh.
- Không hard-code text trong gameplay code.
- Dùng localization keys ổn định cho UI, quái, skill, cảnh giới, item và quest.
- Hỗ trợ text dài khác nhau.
- Font phải hỗ trợ đầy đủ dấu tiếng Việt.

---

# 11. ART DIRECTION VÀ ASSET PIPELINE

## Art direction
Detailed pixel art, nhiều lớp màu, silhouette rõ, tỷ lệ RPG pixel cân đối, môi trường có chiều sâu, VFX tương phản nhưng không che thông tin chiến đấu. Giữ pixel density nhất quán.

## Asset checklist

**Nhân vật:** Idle, Attack, Cast Skill, Hit, Death, Revive; có thể bổ sung animation đột phá sau MVP.

**Quái thường:** mỗi Map tối thiểu bốn loại; animation Move/Idle, Attack, Hit, Death tùy loại.

**Boss:** một Boss mỗi Map; Idle, Attack, Skill Attack, Hit, Death và Move nếu cần.

**Môi trường:** Thanh Trúc Lâm, Hỏa Sơn Cốc, Hàn Băng Bí Cảnh; background, tile/arena, chi tiết trang trí, parallax nếu phù hợp.

**UI:** panels, buttons normal/pressed/disabled, progress bars, HP bars, resource icons, upgrade icons, skill icons, realm icons, item/rarity icons, quest icons, popups, Boss frame và offline reward elements.

**VFX:** hit, critical, năm kỹ năng, spawn, death, chuyển Map, đột phá.

## Pipeline đã chốt
1. Prototype bằng placeholder.
2. Hoàn thiện gameplay/save/progression/offline.
3. Thay placeholder từng nhóm bằng asset hoàn chỉnh.
4. Giữ nguyên interface giữa logic và visual.
5. Validate sorting layer, pivot, collider, animation, alpha, pixel density, build size và texture memory.

Claude Code phải kiểm tra công cụ tạo ảnh thực tế. Nếu có, tạo asset theo nhóm và xác minh sprite sheet/frame layout. Nếu không, không tuyên bố đã tạo art hoàn chỉnh; tiếp tục placeholder/procedural art và tạo asset manifest/prompt. Không giả định ảnh AI đơn lẻ tự động trở thành sprite sheet animation.

---

# 12. KIẾN TRÚC UNITY

## Nguyên tắc
- Data-driven, tách gameplay logic khỏi UI/rendering.
- Không hard-code nội dung Map/Wave.
- Stable IDs cho mọi data entity.
- ScriptableObject cho cấu hình tĩnh; Save DTO riêng.
- Pool enemy/projectile/VFX khi phù hợp.
- Hạn chế logic trong `Update()` khi không cần.
- Tránh singleton toàn cục cho mọi hệ thống.
- Event/callback rõ ràng, quản lý subscription đúng vòng đời.
- Code đơn giản, dễ debug, dễ test.

## Cấu trúc thư mục đề xuất

```text
Assets/
  _Project/
    Art/
      Characters/
      Enemies/
      Bosses/
      Environments/
      UI/
      VFX/
    Audio/
    Animations/
    Prefabs/
      Characters/
      Enemies/
      Bosses/
      UI/
      VFX/
    Scenes/
      Bootstrap.unity
      MainGame.unity
    Scripts/
      Core/
      Data/
      Combat/
      Enemies/
      Skills/
      Progression/
      Cultivation/
      Upgrades/
      Equipment/
      Gacha/
      Quests/
      Offline/
      Save/
      UI/
      Localization/
      Utilities/
    ScriptableObjects/
      World/
      Maps/
      Waves/
      Enemies/
      Bosses/
      Skills/
      Upgrades/
      Items/
      Gacha/
      Quests/
      Economy/
      Scaling/
    Localization/
    Tests/
      EditMode/
      PlayMode/
```

Trước khi tạo thư mục, audit project hiện tại; không di chuyển/xóa asset chưa kiểm tra reference.

## Data definitions
- `WorldDefinition`: world ID, Map list, scaling config, progression rules.
- `MapDefinition`: Map ID, localization, environment, enemy roster, waves, Boss, transition, reward modifiers.
- `StageDefinition`: stage ID, Wave index, groups, spawn interval, completion condition, elite/special condition, rewards.
- `EnemyDefinition`: ID, base stats, AI, movement/attack parameters, visual refs, reward table.
- `BossDefinition`: ID, stats, attack patterns, skills, rewards.
- `SkillDefinition`: ID, damage type, targeting, cooldown, multiplier, range/AoE, VFX, unlock and upgrade config.
- `UpgradeDefinition`: ID, cost, curve, effect, per-level effect, unlock, max level.
- `ItemDefinition`: ID, rarity, slot, stats, drop source, localization, icon.
- `GachaPoolDefinition`: pool, entries, weights/probabilities, cost and rewards.
- `QuestDefinition`: ID, objective, target, reward, reset rule, localization.
- `CycleScalingConfig`: HP, attack, density, reward curves and clamps.

## Runtime systems đề xuất
`GameBootstrap`, `WorldDatabase`, `MapManager`, `StageProgressionSystem`, `WaveManager`, `BossWaveManager`, `CombatSystem`, `TargetSelectionSystem`, `EnemySystem`, `SkillSystem`, `UpgradeSystem`, `CultivationSystem`, `EquipmentSystem`, `InventorySystem`, `GachaSystem`, `QuestSystem`, `OfflineProgressSystem`, `SaveSystem`, `LocalizationService`, `AudioService`, `ObjectPoolService` và UI presenters.

Đây là trách nhiệm logic, không bắt buộc mỗi tên thành một MonoBehaviour. Tránh phân mảnh quá mức.

---

# 13. QUALITY, PERFORMANCE VÀ TESTING

## Code quality
- Tránh logic trùng lặp và magic numbers.
- Công thức tính toán phải test được.
- Xử lý null/invalid state.
- Tránh đăng ký event lặp.
- Hủy subscription đúng vòng đời.
- Hạn chế GC allocations trong combat.
- Không để event hoặc reference giữ GameObject đã bị hủy.
- Không mất state khi chuyển scene/reload.

## Performance
- Giới hạn quái hoạt động theo config.
- Pool enemy/projectile/VFX.
- Dùng Sprite Atlas khi phù hợp.
- Kiểm soát overdraw VFX.
- Test trên build mobile thật.
- Không tối ưu quá mức khiến kiến trúc phức tạp không cần thiết.

## Unit tests
- Damage formula.
- Upgrade cost formula.
- Cycle scaling.
- Chapter/Cycle/Map indexing.
- Offline reward.
- Breakthrough requirements.
- Idempotent quest reward claim.
- Gacha table validity.
- Save migration.

## PlayMode tests
- Spawn/kill groups.
- Wave transition chỉ khi đủ điều kiện.
- Không chuyển Wave khi còn quái.
- Respawn tiếp tục Wave.
- Thua Boss về Wave 10.
- Thắng Boss chuyển Map/Cycle.
- Offline không tiến triển Wave/Boss/quest.
- Save/load khôi phục đúng trạng thái.

---

# 14. KẾ HOẠCH TRIỂN KHAI CHO CLAUDE CODE

## Phase 0 — Audit project
1. Đọc project Unity hiện có.
2. Kiểm tra Unity version, render pipeline, input system và packages.
3. Kiểm tra scene, prefab, script, asset.
4. Kiểm tra build.
5. Báo cáo hiện trạng và kế hoạch.
6. Không xóa/ghi đè asset chưa hiểu.

**Gate:** project được hiểu rõ và có kế hoạch thực tế.

## Phase 1 — Playable prototype
1. Arena cố định.
2. Player/enemy placeholder.
3. Auto attack và nearest target.
4. HP, damage, death/respawn.
5. Group spawning và Wave transition.
6. Wave 10 và BossReady.
7. Boss placeholder.
8. Map transition.

**Gate:** chơi được vòng lặp Wave 1 đến Boss bằng placeholder.

## Phase 2 — Progression/Cycle
1. Ba Map, 10 Wave mỗi Map.
2. Ba Boss.
3. Chapter/Cycle indexing.
4. Scaling cấu hình.
5. Boss win/loss.
6. Endless loop.
7. Test giữ nguyên sức mạnh qua Cycle.

**Gate:** chạy đúng ít nhất hai Cycle.

## Phase 3 — Skill/economy/cultivation
1. Ba tài nguyên.
2. Năm kỹ năng.
3. Sáu nhóm nâng cấp.
4. Cost curve.
5. Cảnh giới khởi đầu.
6. Manual breakthrough.
7. Save data cho các hệ thống.

**Gate:** chiến đấu, thu tài nguyên, nâng cấp, đột phá hoạt động.

## Phase 4 — Save/offline
1. Local save.
2. Backup/restore.
3. Schema version.
4. Offline reward 12 giờ, hiệu suất cơ sở 80%.
5. Chặn offline progression.
6. Offline rewards UI.
7. Test save lỗi.

**Gate:** offline không tiến Wave/Boss và save có thể phục hồi.

## Phase 5 — Quests/localization
1. Quest definitions.
2. Daily quests.
3. Reset giờ thiết bị.
4. Idempotent claim.
5. Expiry.
6. Vietnamese/English.
7. Test text overflow/font.

**Gate:** quest/localization không hard-code text trong gameplay.

## Phase 6 — Equipment/gacha
1. Item definitions.
2. Inventory.
3. Equip/unequip.
4. Stat modifiers.
5. Drop tables.
6. Gacha pool.
7. Safe resource deduction/reward grant.
8. Save/restore.
9. Validate probabilities.

**Gate:** không mất item/tài nguyên và tất cả được cấu hình.

## Phase 7 — Polish/art
1. Thay placeholder theo nhóm.
2. Animation/VFX.
3. Fantasy pixel UI.
4. Map transitions.
5. Tối ưu pooling/texture/performance.
6. Test build mobile.
7. Kiểm tra save/progression.

**Gate:** game build và chơi được; thay art không cần viết lại gameplay.

---

# 15. ACCEPTANCE CRITERIA

- [ ] Project mở và build được với Unity version phù hợp.
- [ ] Ba Map, mỗi Map 10 Wave + Boss.
- [ ] Chapter/Cycle/Map indexing đúng.
- [ ] Wave tự chuyển sau khi hoàn thành hợp lệ.
- [ ] Quái spawn theo nhóm; không spawn nhóm mới khi nhóm trước còn quái.
- [ ] Nhóm cuối có elite hoặc special condition.
- [ ] Nhân vật tự tấn công quái gần nhất.
- [ ] Năm kỹ năng tự động hoạt động theo config.
- [ ] Nhân vật hồi sinh và tiếp tục Wave.
- [ ] Thua Boss farm Wave 10, thử lại bằng nút.
- [ ] Thắng Boss tự chuyển Map/Cycle.
- [ ] Cycle mới không reset tài nguyên/sức mạnh.
- [ ] Scaling theo đường cong cấu hình được.
- [ ] Ba tài nguyên, sáu nâng cấp, tu luyện và breakthrough hoạt động.
- [ ] Trang bị/gacha hoạt động hoặc phần chưa triển khai được ghi rõ TODO.
- [ ] Offline tối đa 12 giờ, hiệu suất cơ sở 80%.
- [ ] Offline không tăng Wave/Boss/quest progress.
- [ ] Daily quests reset theo giờ thiết bị; thưởng chưa nhận hết hạn theo ngày.
- [ ] Save tự động, backup và restore hoạt động.
- [ ] Tiếng Việt và tiếng Anh hoạt động.
- [ ] UI thích ứng màn hình dọc.
- [ ] Có thể chơi bằng placeholder trước khi thay art.
- [ ] Save lỗi không gây crash hoặc mất dữ liệu âm thầm.
- [ ] MVP không bắt buộc backend.

---

# 16. CHỈ THỊ CUỐI CÙNG CHO CLAUDE CODE

Coi tài liệu này là nguồn yêu cầu chính của Pixel Immortal.

1. Trước khi sửa code, audit project và báo cáo cấu trúc hiện tại.
2. Không tạo lại project nếu đã có project Unity hợp lệ.
3. Không xóa asset/scene/script chưa xác minh.
4. Triển khai theo phase, ưu tiên prototype chơi được.
5. Sau mỗi phase, chạy kiểm tra/build nếu môi trường hỗ trợ.
6. Không chỉ tạo file code rồi tuyên bố hoàn thành; kiểm tra compile, scene và prefab references.
7. Dùng placeholder trước, thay art sau.
8. Không tuyên bố đã tạo ảnh AI nếu công cụ không khả dụng.
9. Không hard-code dữ liệu Map/Wave/skill/upgrade/gacha/reward.
10. Tách Save DTO khỏi ScriptableObject.
11. Viết test cho công thức và quy tắc tiến trình quan trọng.
12. Không tự thay đổi các quyết định đã chốt trong Decision Log.
13. Nếu thiếu dữ liệu quan trọng, dùng giải pháp mặc định có thể cấu hình và ghi rõ giả định.
14. Nếu không thể hoàn thành hết trong một lượt, hoàn thành phase hiện tại và báo cáo trạng thái thực tế, lỗi còn lại, placeholder và bước tiếp theo.

**Mục tiêu cuối:** game Idle RPG có vòng lặp thực sự hoạt động, Cycle vô hạn, nâng cấp/tu luyện, offline reward an toàn, save backup/restore, localization Việt/Anh, kiến trúc data-driven và pipeline thay asset không phá vỡ gameplay.
