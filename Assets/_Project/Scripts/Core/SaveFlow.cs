using System;
using PixelImmortal.Economy;
using PixelImmortal.Localization;
using PixelImmortal.Offline;
using PixelImmortal.Quests;
using PixelImmortal.Save;
using PixelImmortal.UI;
using UnityEngine;

namespace PixelImmortal.Core
{
    /// <summary>Startup save handling: load, corrupt-save decision, backup notice, offline reward popup. Text comes from the localization table.</summary>
    public static class SaveFlow
    {
        public static string DefaultDirectory => System.IO.Path.Combine(Application.persistentDataPath, "PixelImmortal");

        /// <summary>Loads and applies progression and quests. The caller wires the world state and coordinator afterwards.</summary>
        public static LoadResult LoadAndApply(SaveSystem system, PlayerProgress progress, QuestSystem quests, out SaveData data)
        {
            var result = system.Load();
            data = result.Data ?? new SaveData();
            if (result.Status == LoadStatus.Loaded || result.Status == LoadStatus.LoadedFromBackup)
            {
                progress.ApplyData(data.progress);
                quests.ApplyData(data.quests);
            }
            return result;
        }

        public static void ShowStartupMessages(LoadResult result, SaveCoordinator coordinator, OfflineService offline,
            PlayerProgress progress, PopupPanel popup, LocalizationService loc, long nowUtcTicks)
        {
            var data = coordinator.Data;

            if (result.Status == LoadStatus.Corrupt)
            {
                coordinator.AutosaveEnabled = false;
                Debug.LogError("Save unreadable: " + result.Error);
                popup.Show(loc.Get("save.corrupt"),
                    loc.Get("save.start_new"), () =>
                    {
                        coordinator.System.MoveCorruptAside();
                        coordinator.AutosaveEnabled = true;
                        coordinator.SaveNow();
                    },
                    loc.Get("save.play_without"), null);
                return;
            }

            if (result.Status == LoadStatus.NewGame)
            {
                data.savedAtUtcTicks = nowUtcTicks;
                coordinator.SaveNow();
                if (!data.settings.introSeen)
                    popup.Show(loc.Get("intro.text"), loc.Get("btn.ok"), () =>
                    {
                        data.settings.introSeen = true;
                        coordinator.SaveNow();
                    });
                return;
            }

            string notice = result.Status == LoadStatus.LoadedFromBackup ? loc.Get("save.backup_notice") + "\n\n" : "";

            bool hasOffline = offline.Prepare(data, nowUtcTicks, progress);
            coordinator.SaveNow(); // persist the pending reward and the consumed time gap immediately

            if (hasOffline) ShowOfflinePopup(coordinator, offline, progress, popup, loc, notice);
            else if (notice.Length > 0)
            {
                popup.Show(notice.TrimEnd(), loc.Get("btn.ok"), null);
            }
        }

        /// <summary>
        /// The app was in the background (process alive) and has come back. Counts the time away exactly like a
        /// cold start would, merging into any reward that is still unclaimed.
        /// </summary>
        public static void ShowReturnMessage(SaveCoordinator coordinator, OfflineService offline, PlayerProgress progress,
            PopupPanel popup, LocalizationService loc)
        {
            if (!coordinator.AutosaveEnabled) return; // a damaged-save decision is pending; do not touch the data
            bool has = offline.Prepare(coordinator.Data, DateTime.UtcNow.Ticks, progress);
            coordinator.SaveNow();
            if (has) ShowOfflinePopup(coordinator, offline, progress, popup, loc, "");
        }

        static void ShowOfflinePopup(SaveCoordinator coordinator, OfflineService offline, PlayerProgress progress,
            PopupPanel popup, LocalizationService loc, string notice)
        {
            var data = coordinator.Data;
            var p = data.offline;
            popup.Show(notice + loc.Format("offline.welcome", FormatDuration(loc, p.effectiveSeconds), offline.MaxHours.ToString("0.#"),
                    (p.efficiency * 100f).ToString("0"), UiKit.Short(p.linhThach), UiKit.Short(p.linhKhi), UiKit.Short(p.tinhHoa)),
                loc.Get("btn.claim"), () =>
                {
                    offline.Claim(data, progress);
                    coordinator.SaveNow();
                });
        }

        public static string FormatDuration(LocalizationService loc, double seconds)
        {
            var t = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return loc.Format("time.hm", (int)t.TotalHours, t.Minutes);
        }
    }
}
