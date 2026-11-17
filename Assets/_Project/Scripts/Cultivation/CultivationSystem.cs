using PixelImmortal.Data;
using PixelImmortal.Economy;

namespace PixelImmortal.Cultivation
{
    public enum BreakthroughBlock
    {
        None,
        MaxRealm,
        NeedLinhKhi,
        NeedLinhThach,
        NeedChapter
    }

    /// <summary>Manual breakthrough: validate, spend, advance realm. The player must trigger it.</summary>
    public class CultivationSystem
    {
        readonly PlayerProgress progress;

        /// <summary>Raised after a successful breakthrough.</summary>
        public event System.Action BrokeThrough;

        public CultivationSystem(PlayerProgress progress) { this.progress = progress; }

        public RealmDefinition Current => progress.Economy.realms[progress.RealmIndex];

        public bool HasNext => progress.RealmIndex + 1 < progress.Economy.realms.Length;

        public RealmDefinition Next => HasNext ? progress.Economy.realms[progress.RealmIndex + 1] : null;

        public BreakthroughBlock Check()
        {
            var next = Next;
            if (next == null) return BreakthroughBlock.MaxRealm;
            if (progress.HighestChapter < next.minChapter) return BreakthroughBlock.NeedChapter;
            if (!progress.Wallet.CanAfford(ResourceType.LinhKhi, next.linhKhiCost)) return BreakthroughBlock.NeedLinhKhi;
            if (!progress.Wallet.CanAfford(ResourceType.LinhThach, next.linhThachCost)) return BreakthroughBlock.NeedLinhThach;
            return BreakthroughBlock.None;
        }

        public bool TryBreakthrough()
        {
            if (Check() != BreakthroughBlock.None) return false;
            var next = Next;
            // Both affordable per Check(); spend atomically (refund first if the second fails).
            if (!progress.Wallet.TrySpend(ResourceType.LinhKhi, next.linhKhiCost)) return false;
            if (!progress.Wallet.TrySpend(ResourceType.LinhThach, next.linhThachCost))
            {
                progress.Wallet.Add(ResourceType.LinhKhi, next.linhKhiCost);
                return false;
            }
            progress.SetRealm(progress.RealmIndex + 1);
            BrokeThrough?.Invoke();
            return true;
        }
    }
}
