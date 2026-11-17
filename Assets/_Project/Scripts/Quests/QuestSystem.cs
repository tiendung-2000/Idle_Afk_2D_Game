using System;
using System.Collections.Generic;
using System.Globalization;
using PixelImmortal.Data;
using PixelImmortal.Economy;
using PixelImmortal.Save;

namespace PixelImmortal.Quests
{
    /// <summary>
    /// Daily quests. Reset at 00:00 device-local time; unclaimed rewards expire with the day.
    /// Progress only comes from live-play events (callers), never from offline processing.
    /// </summary>
    public class QuestSystem
    {
        const string DayFormat = "yyyy-MM-dd";

        class State { public double progress; public bool claimed; }

        readonly EconomyConfig eco;
        readonly PlayerProgress progress;
        readonly Func<DateTime> localNow;
        readonly Dictionary<string, State> states = new Dictionary<string, State>();
        string dayKey = "";

        /// <summary>Raised when progress, claim state or the day changes.</summary>
        public event Action Changed;

        /// <summary>Raised after a reward is paid out.</summary>
        public event Action Claimed;

        public QuestSystem(EconomyConfig economy, PlayerProgress playerProgress, Func<DateTime> localNow)
        {
            eco = economy;
            progress = playerProgress;
            this.localNow = localNow;
            ResetAll(DayKeyOf(localNow()));
        }

        public QuestDefinition[] Quests => eco.dailyQuests ?? new QuestDefinition[0];

        public static string DayKeyOf(DateTime local) => local.ToString(DayFormat, CultureInfo.InvariantCulture);

        State Get(string id)
        {
            if (!states.TryGetValue(id, out var s)) states[id] = s = new State();
            return s;
        }

        public double GetProgress(QuestDefinition q) => Math.Min(Get(q.id).progress, q.targetCount);
        public bool IsComplete(QuestDefinition q) => Get(q.id).progress >= q.targetCount;
        public bool IsClaimed(QuestDefinition q) => Get(q.id).claimed;
        public bool CanClaim(QuestDefinition q) => IsComplete(q) && !IsClaimed(q);

        public int ClaimableCount
        {
            get
            {
                int n = 0;
                foreach (var q in Quests) if (q != null && CanClaim(q)) n++;
                return n;
            }
        }

        public TimeSpan TimeUntilReset
        {
            get
            {
                var now = localNow();
                return now.Date.AddDays(1) - now;
            }
        }

        // ---- day rollover -----------------------------------------------------

        /// <summary>
        /// Resets everything if the device date is later than the stored day. A date earlier than the
        /// stored one (clock moved back) never resets, so the same day cannot be farmed twice.
        /// </summary>
        public bool EnsureCurrentDay()
        {
            string today = DayKeyOf(localNow());
            if (today == dayKey) return false;

            if (DateTime.TryParseExact(dayKey, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var stored)
                && DateTime.TryParseExact(today, DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var now)
                && now < stored)
                return false;

            ResetAll(today);
            Changed?.Invoke();
            return true;
        }

        void ResetAll(string day)
        {
            dayKey = day;
            states.Clear();
            foreach (var q in Quests) if (q != null) states[q.id] = new State();
        }

        // ---- progress and claim ----------------------------------------------

        public void AddProgress(QuestObjective objective, double amount)
        {
            if (double.IsNaN(amount) || amount <= 0) return;
            EnsureCurrentDay();
            bool any = false;
            foreach (var q in Quests)
            {
                if (q == null || q.objective != objective) continue;
                var s = Get(q.id);
                if (s.claimed || s.progress >= q.targetCount) continue;
                s.progress = Math.Min(q.targetCount, s.progress + amount);
                any = true;
            }
            if (any) Changed?.Invoke();
        }

        /// <summary>Claims a finished quest once. Re-checks the day first, so an expired reward cannot be claimed.</summary>
        public bool TryClaim(QuestDefinition q)
        {
            if (q == null) return false;
            EnsureCurrentDay();
            if (!CanClaim(q)) return false;

            Get(q.id).claimed = true; // mark first: no path can pay twice
            RewardMath.Grant(progress.Wallet, q.reward, 1f, false);
            Changed?.Invoke();
            Claimed?.Invoke();
            return true;
        }

        // ---- save -----------------------------------------------------------

        public QuestSaveData ToData()
        {
            var d = new QuestSaveData { dayKey = dayKey };
            foreach (var q in Quests)
            {
                if (q == null) continue;
                var s = Get(q.id);
                d.entries.Add(new QuestEntry { id = q.id, progress = s.progress, claimed = s.claimed });
            }
            return d;
        }

        /// <summary>Restores saved state, then applies the day rollover (a new day clears everything).</summary>
        public void ApplyData(QuestSaveData d)
        {
            if (d == null) return;
            string saved = d.dayKey ?? "";
            ResetAll(string.IsNullOrEmpty(saved) ? DayKeyOf(localNow()) : saved);

            if (d.entries != null)
                foreach (var e in d.entries)
                {
                    var q = e != null ? eco.FindQuest(e.id) : null;
                    if (q == null) continue;
                    var s = Get(q.id);
                    double p = double.IsNaN(e.progress) || e.progress < 0 ? 0 : e.progress;
                    s.progress = Math.Min(p, q.targetCount);
                    s.claimed = e.claimed && s.progress >= q.targetCount; // a claim without completion is invalid
                }

            EnsureCurrentDay();
            Changed?.Invoke();
        }
    }
}
