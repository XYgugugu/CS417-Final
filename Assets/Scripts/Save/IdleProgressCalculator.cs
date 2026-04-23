using System;
using PVZ3D.Core;

namespace PVZ3D.Core
{
    public struct IdleProgressResult
    {
        public double UsedMinutes;
        public int AwardedSun;
        public int AwardedCoins;

        public bool HasRewards => AwardedSun > 0 || AwardedCoins > 0;
    }
}

namespace PVZ3D.Save
{
    public static class IdleProgressCalculator
    {
        public static IdleProgressResult Calculate(
            string lastSessionUtc,
            DateTime nowUtc,
            int sunPerMinute = 4,
            int coinsPerMinute = 1,
            int maxMinutes = 30)
        {
            if (string.IsNullOrWhiteSpace(lastSessionUtc))
            {
                return default;
            }

            if (!DateTime.TryParse(lastSessionUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime last))
            {
                return default;
            }

            double elapsedMinutes = (nowUtc - last.ToUniversalTime()).TotalMinutes;
            if (elapsedMinutes <= 0d)
            {
                return default;
            }

            double used = Math.Min(elapsedMinutes, Math.Max(0, maxMinutes));
            int sun = (int)Math.Floor(used * Math.Max(0, sunPerMinute));
            int coins = (int)Math.Floor(used * Math.Max(0, coinsPerMinute));

            return new IdleProgressResult
            {
                UsedMinutes = used,
                AwardedSun = sun,
                AwardedCoins = coins
            };
        }
    }
}
