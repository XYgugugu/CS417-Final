using System;

namespace PVZ3D.Core
{
    public static class GameEvents
    {
        public static event Action<int> OnSunChanged;
        public static event Action<int> OnCoinsChanged;
        public static event Action<int, int> OnWaveChanged;
        public static event Action<int, int> OnBaseHealthChanged;
        public static event Action OnGameWon;
        public static event Action OnGameLost;
        public static event Action OnGameStarted;
        public static event Action<int, int> OnPlantPlaced;
        public static event Action<int, int> OnPlantRemoved;
        public static event Action<int> OnZombieSpawned;
        public static event Action<int> OnZombieKilled;
        public static event Action<int> OnPlantFired;
        public static event Action<int> OnWaveStarted;
        public static event Action<int> OnWaveCompleted;
        public static event Action OnAllWavesCompleted;
        public static event Action<int> OnBaseDamaged;
        public static event Action<string, int> OnResourceCollected;
        public static event Action<string, int> OnResourceSpent;
        public static event Action<bool, string> OnPurchaseResult;
        public static event Action<bool> OnCheatModeChanged;
        public static event Action<GamePhase> OnGamePhaseChanged;
        public static event Action<RunStats, bool> OnRunEnded;
        public static event Action<IdleProgressResult> OnIdleProgressApplied;

        public static void RaiseSunChanged(int value) => OnSunChanged?.Invoke(value);
        public static void RaiseCoinsChanged(int value) => OnCoinsChanged?.Invoke(value);
        public static void RaiseWaveChanged(int current, int total) => OnWaveChanged?.Invoke(current, total);
        public static void RaiseBaseHealthChanged(int current, int max) => OnBaseHealthChanged?.Invoke(current, max);
        public static void RaiseGameWon() => OnGameWon?.Invoke();
        public static void RaiseGameLost() => OnGameLost?.Invoke();
        public static void RaiseGameStarted() => OnGameStarted?.Invoke();
        public static void RaisePlantPlaced(int lane, int col) => OnPlantPlaced?.Invoke(lane, col);
        public static void RaisePlantRemoved(int lane, int col) => OnPlantRemoved?.Invoke(lane, col);
        public static void RaiseZombieSpawned(int lane) => OnZombieSpawned?.Invoke(lane);
        public static void RaiseZombieKilled(int lane) => OnZombieKilled?.Invoke(lane);
        public static void RaisePlantFired(int lane) => OnPlantFired?.Invoke(lane);
        public static void RaiseWaveStarted(int wave) => OnWaveStarted?.Invoke(wave);
        public static void RaiseWaveCompleted(int wave) => OnWaveCompleted?.Invoke(wave);
        public static void RaiseAllWavesCompleted() => OnAllWavesCompleted?.Invoke();
        public static void RaiseBaseDamaged(int amount) => OnBaseDamaged?.Invoke(amount);
        public static void RaiseResourceCollected(string type, int amount) => OnResourceCollected?.Invoke(type, amount);
        public static void RaiseResourceSpent(string type, int amount) => OnResourceSpent?.Invoke(type, amount);
        public static void RaisePurchaseResult(bool success, string reason) => OnPurchaseResult?.Invoke(success, reason);
        public static void RaiseCheatModeChanged(bool enabled) => OnCheatModeChanged?.Invoke(enabled);
        public static void RaiseGamePhaseChanged(GamePhase phase) => OnGamePhaseChanged?.Invoke(phase);
        public static void RaiseRunEnded(RunStats stats, bool won) => OnRunEnded?.Invoke(stats, won);
        public static void RaiseIdleProgressApplied(IdleProgressResult result) => OnIdleProgressApplied?.Invoke(result);
    }
}
