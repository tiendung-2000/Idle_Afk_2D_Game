using System;
using PixelImmortal.Economy;
using PixelImmortal.Localization;
using PixelImmortal.Quests;
using PixelImmortal.Progression;
using UnityEngine;

namespace PixelImmortal.Save
{
    /// <summary>
    /// Autosaves at safe points: director milestones, stat/upgrade changes (debounced), a periodic timer,
    /// and when the app is paused or quit. Disabled while a corrupt save is awaiting the player's decision.
    /// </summary>
    public class SaveCoordinator : MonoBehaviour
    {
        const float DebounceSeconds = 2f;
        const float PeriodicSeconds = 20f;

        SaveSystem system;
        PlayerProgress progress;
        BattleDirector director;
        QuestSystem quests;
        LocalizationService loc;
        bool dirty;
        float lastSave;

        public SaveData Data { get; private set; }
        public SaveSystem System => system;
        public bool AutosaveEnabled { get; set; } = true;

        /// <summary>Optional: the mute state is written into the save settings.</summary>
        public PixelImmortal.Audio.AudioService Audio { get; set; }

        public void Init(SaveSystem saveSystem, SaveData data, PlayerProgress prog, BattleDirector dir, QuestSystem questSystem, LocalizationService localization)
        {
            quests = questSystem;
            loc = localization;
            system = saveSystem;
            Data = data;
            progress = prog;
            director = dir;
            lastSave = Time.unscaledTime;
            director.Milestone += MarkDirty;
            progress.StatsChanged += MarkDirty;
            progress.InventoryChanged += MarkDirty;
            loc.LanguageChanged += MarkDirty;
        }

        void OnDestroy()
        {
            if (director != null) director.Milestone -= MarkDirty;
            if (progress != null) progress.StatsChanged -= MarkDirty;
            if (progress != null) progress.InventoryChanged -= MarkDirty;
            if (loc != null) loc.LanguageChanged -= MarkDirty;
        }

        public void MarkDirty() => dirty = true;


        void Update()
        {
            if (!AutosaveEnabled || system == null) return;
            float since = Time.unscaledTime - lastSave;
            if ((dirty && since >= DebounceSeconds) || since >= PeriodicSeconds) SaveNow();
        }

        public bool SaveNow()
        {
            if (!AutosaveEnabled || system == null) return false;
            Data.progress = progress.ToData();
            Data.world = director.CaptureWorld();
            Data.quests = quests.ToData();
            Data.settings.language = loc.Language;
            if (Audio != null) Data.settings.muted = Audio.Muted;
            Data.savedAtUtcTicks = DateTime.UtcNow.Ticks;
            bool ok = system.Save(Data);
            lastSave = Time.unscaledTime;
            if (ok) dirty = false;
            return ok;
        }

        /// <summary>
        /// Player-confirmed reset: stops autosave so the old state cannot be written back, moves the save files aside
        /// (kept in the save folder, never deleted) and restarts the scene as a new game.
        /// </summary>
        public void ResetAndReload()
        {
            AutosaveEnabled = false;
            system.MoveCorruptAside();
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Raised when the app comes back to the foreground (the process kept running while away).</summary>
        public event Action Resumed;

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveNow();
            else Resumed?.Invoke();
        }

        void OnApplicationQuit() => SaveNow();
    }
}
