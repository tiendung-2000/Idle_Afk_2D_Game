using PixelImmortal.Data;

namespace PixelImmortal.Economy
{
    public static class RewardMath
    {
        /// <param name="earned">True for live-play income (counts toward quests); false for offline/quest payouts.</param>
        public static void Grant(Wallet wallet, RewardEntry entry, float multiplier, bool earned = true)
        {
            if (entry == null || float.IsNaN(multiplier) || multiplier <= 0f) return;
            wallet.Add(ResourceType.LinhThach, entry.linhThach * multiplier, earned);
            wallet.Add(ResourceType.LinhKhi, entry.linhKhi * multiplier, earned);
            wallet.Add(ResourceType.TinhHoa, entry.tinhHoa * multiplier, earned);
        }
    }
}
