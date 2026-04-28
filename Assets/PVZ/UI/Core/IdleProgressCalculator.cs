using System;
using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Pure helper that figures out how much sun/coins to award the player
    /// based on real-world time elapsed since the last save.
    ///
    /// Kept stateless so it's trivially unit-testable. The actual application
    /// of these rewards happens in <see cref="GameSceneBootstrap"/>.
    /// </summary>
    public static class IdleProgressCalculator
    {
        public struct Result
        {
            public double secondsAway;
            public int sunGained;
            public int coinsGained;
            public bool wasCapped;
        }

        public static Result Calculate(SaveData save, GameSettings settings, DateTime nowUtc)
        {
            var result = new Result();
            if (save == null || settings == null) return result;
            if (save.lastSavedUtcTicks <= 0) return result;

            var lastSaved = new DateTime(save.lastSavedUtcTicks, DateTimeKind.Utc);
            var delta = nowUtc - lastSaved;
            var seconds = delta.TotalSeconds;
            if (seconds <= 0) return result; // clock went backwards — treat as no idle

            var capped = seconds > settings.maxIdleSeconds;
            if (capped) seconds = settings.maxIdleSeconds;

            result.secondsAway = seconds;
            result.sunGained = Mathf.FloorToInt((float)(seconds * settings.idleSunPerSecond));
            result.coinsGained = Mathf.FloorToInt((float)(seconds * settings.idleCoinsPerSecond));
            result.wasCapped = capped;
            return result;
        }

        /// <summary>Format a duration for the "welcome back" toast.</summary>
        public static string FormatDuration(double seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{(int)t.TotalMinutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }
    }
}
