using System;
using System.Collections.Generic;
using UnityEngine;

namespace PVZ3D.UI
{
    /// <summary>
    /// Single source of truth for all gameplay-facing UI data.
    ///
    /// Aaron's UI subscribes to the events declared here. Other team members
    /// (Jiabin/Ellen/Encheng/etc.) push state changes through the public Set*/Add*
    /// methods. No direct cross-module references — pure event bus.
    ///
    /// Lifecycle: initialized by <see cref="GameSceneBootstrap"/> on scene load,
    /// torn down on scene unload. Persisted via <see cref="SaveSystem"/>.
    /// </summary>
    public static class GameState
    {
        // ---------- Resources ----------
        private static int _sun;
        public static int Sun
        {
            get => _sun;
            private set
            {
                if (_sun == value) return;
                _sun = value;
                OnSunChanged?.Invoke(_sun);
            }
        }

        private static int _coins;
        public static int Coins
        {
            get => _coins;
            private set
            {
                if (_coins == value) return;
                _coins = value;
                OnCoinsChanged?.Invoke(_coins);
            }
        }

        // ---------- Player ----------
        private static int _health;
        private static int _maxHealth;
        public static int Health => _health;
        public static int MaxHealth => _maxHealth;

        // ---------- Wave / Scoreboard ----------
        private static int _currentWave;
        private static int _totalWaves;
        private static int _zombiesDefeated;
        private static int _highestWaveReached;

        public static int CurrentWave => _currentWave;
        public static int TotalWaves => _totalWaves;
        public static int ZombiesDefeated => _zombiesDefeated;
        public static int HighestWaveReached => _highestWaveReached;

        // ---------- Plants ----------
        // plantId -> remaining cooldown seconds (0 = ready)
        private static readonly Dictionary<string, float> _plantCooldowns = new();
        // plantId -> total cooldown duration (set when planted)
        private static readonly Dictionary<string, float> _plantCooldownTotals = new();
        private static readonly HashSet<string> _unlockedPlants = new();

        public static IReadOnlyDictionary<string, float> PlantCooldowns => _plantCooldowns;
        public static IReadOnlyDictionary<string, float> PlantCooldownTotals => _plantCooldownTotals;
        public static IReadOnlyCollection<string> UnlockedPlants => _unlockedPlants;

        // ---------- Loss Timer ----------
        public static float LossTimerRemain { get; private set; }
        public static float LossTimerTotal { get; private set; }
        public static bool LossTimerRunning { get; private set; }

        // ---------- Game flow ----------
        public static bool IsGameOver { get; private set; }
        public static bool DidWin { get; private set; }
        public static bool CheatModeEnabled { get; private set; }

        // ---------- Events ----------
        public static event Action<int> OnSunChanged;
        public static event Action<int> OnCoinsChanged;
        public static event Action<int, int> OnHealthChanged;       // (current, max)
        public static event Action<int, int> OnWaveProgressed;      // (current, total)
        public static event Action<int> OnZombieDefeated;           // (totalDefeatedSoFar)
        public static event Action<string, float, float> OnPlantCooldownTick; // (id, remain, total)
        public static event Action<string> OnPlantUsed;             // (id) — fired the moment a card is consumed
        public static event Action<string> OnPlantUnlocked;
        public static event Action OnGameWon;
        public static event Action OnGameLost;
        public static event Action OnStateReset;
        public static event Action<float, float> OnLossTimerTick;     // (remain, total)

        // ============================================================
        //  Initialization
        // ============================================================

        /// <summary>
        /// Reset all runtime state and fire <see cref="OnStateReset"/>. Called
        /// by GameSceneBootstrap before applying loaded save data.
        /// </summary>
        public static void ResetAll()
        {
            _sun = 0;
            _coins = 0;
            _health = 0;
            _maxHealth = 0;
            _currentWave = 0;
            _totalWaves = 0;
            _zombiesDefeated = 0;
            // _highestWaveReached intentionally NOT reset — it's a high-score across runs
            _plantCooldowns.Clear();
            _plantCooldownTotals.Clear();
            _unlockedPlants.Clear();
            LossTimerRemain = 0f;
            LossTimerTotal = 0f;
            LossTimerRunning = false;
            IsGameOver = false;
            DidWin = false;
            CheatModeEnabled = false;
            OnStateReset?.Invoke();
        }

        // ============================================================
        //  Loss timer
        // ============================================================

        /// <summary>Begin (or restart) the loss countdown. Total seconds drives the visual progress bar.</summary>
        public static void StartLossTimer(float durationSeconds)
        {
            LossTimerTotal = Mathf.Max(0.001f, durationSeconds);
            LossTimerRemain = LossTimerTotal;
            LossTimerRunning = LossTimerTotal > 0f;
            OnLossTimerTick?.Invoke(LossTimerRemain, LossTimerTotal);
        }

        public static void StopLossTimer()
        {
            LossTimerRunning = false;
            LossTimerRemain = 0f;
            OnLossTimerTick?.Invoke(LossTimerRemain, LossTimerTotal);
        }

        /// <summary>Push the current remaining seconds. If it hits 0 while running, the player loses.</summary>
        public static void TickLossTimer(float remainSeconds)
        {
            LossTimerRemain = Mathf.Max(0f, remainSeconds);
            OnLossTimerTick?.Invoke(LossTimerRemain, LossTimerTotal);
            if (LossTimerRunning && LossTimerRemain <= 0f)
            {
                LossTimerRunning = false;
                Lose();
            }
        }

        // ============================================================
        //  Public mutators (called by other gameplay systems)
        // ============================================================

        public static void AddSun(int amount)
        {
            if (amount == 0) return;
            Sun = Mathf.Max(0, _sun + amount);
        }

        public static bool TrySpendSun(int amount)
        {
            if (amount <= 0) return true;
            if (CheatModeEnabled) return true; // cheat mode = free
            if (_sun < amount) return false;
            Sun = _sun - amount;
            return true;
        }

        public static void AddCoins(int amount)
        {
            if (amount == 0) return;
            Coins = Mathf.Max(0, _coins + amount);
        }

        public static bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;
            if (CheatModeEnabled) return true;
            if (_coins < amount) return false;
            Coins = _coins - amount;
            return true;
        }

        public static void SetHealth(int current, int max)
        {
            _maxHealth = Mathf.Max(1, max);
            _health = Mathf.Clamp(current, 0, _maxHealth);
            OnHealthChanged?.Invoke(_health, _maxHealth);
            if (_health == 0 && !IsGameOver) Lose();
        }

        public static void DamagePlayer(int amount)
        {
            if (amount <= 0) return;
            SetHealth(_health - amount, _maxHealth);
        }

        public static void HealPlayer(int amount)
        {
            if (amount <= 0) return;
            SetHealth(_health + amount, _maxHealth);
        }

        public static void SetWave(int current, int total)
        {
            _currentWave = Mathf.Max(0, current);
            _totalWaves = Mathf.Max(0, total);
            if (_currentWave > _highestWaveReached) _highestWaveReached = _currentWave;
            OnWaveProgressed?.Invoke(_currentWave, _totalWaves);
        }

        public static void RecordZombieDefeated()
        {
            _zombiesDefeated++;
            OnZombieDefeated?.Invoke(_zombiesDefeated);
        }

        public static void UnlockPlant(string plantId)
        {
            if (string.IsNullOrEmpty(plantId)) return;
            if (_unlockedPlants.Add(plantId))
            {
                OnPlantUnlocked?.Invoke(plantId);
            }
        }

        public static bool IsPlantUnlocked(string plantId) => _unlockedPlants.Contains(plantId);

        public static bool IsPlantReady(string plantId)
        {
            return !_plantCooldowns.TryGetValue(plantId, out var t) || t <= 0f;
        }

        /// <summary>
        /// Mark a plant card as just used. Starts its cooldown timer.
        /// Plant cost should be deducted by the caller via TrySpendSun first.
        /// </summary>
        public static void StartPlantCooldown(string plantId, float cooldownSeconds)
        {
            if (string.IsNullOrEmpty(plantId) || cooldownSeconds <= 0f) return;
            _plantCooldowns[plantId] = cooldownSeconds;
            _plantCooldownTotals[plantId] = cooldownSeconds;
            OnPlantUsed?.Invoke(plantId);
            OnPlantCooldownTick?.Invoke(plantId, cooldownSeconds, cooldownSeconds);
        }

        /// <summary>Tick all active cooldowns. Called by GameSceneBootstrap each frame.</summary>
        public static void TickCooldowns(float deltaTime)
        {
            if (deltaTime <= 0f || _plantCooldowns.Count == 0) return;

            // Iterate over a snapshot since we mutate the dict.
            using var enumerator = _plantCooldowns.GetEnumerator();
            var keys = new List<string>(_plantCooldowns.Keys);
            foreach (var id in keys)
            {
                var remain = _plantCooldowns[id] - deltaTime;
                var total = _plantCooldownTotals.TryGetValue(id, out var t) ? t : 1f;
                if (remain <= 0f)
                {
                    _plantCooldowns[id] = 0f;
                    OnPlantCooldownTick?.Invoke(id, 0f, total);
                }
                else
                {
                    _plantCooldowns[id] = remain;
                    OnPlantCooldownTick?.Invoke(id, remain, total);
                }
            }
        }

        public static void SetCheatMode(bool enabled)
        {
            CheatModeEnabled = enabled;
            if (enabled)
            {
                AddSun(9999);
                AddCoins(9999);
            }
        }

        public static void Win()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            DidWin = true;
            OnGameWon?.Invoke();
        }

        public static void Lose()
        {
            if (IsGameOver) return;
            IsGameOver = true;
            DidWin = false;
            OnGameLost?.Invoke();
        }

        // ============================================================
        //  Serialization helpers (used by SaveSystem)
        // ============================================================

        internal static SaveData CaptureSnapshot()
        {
            var data = new SaveData
            {
                sun = _sun,
                coins = _coins,
                health = _health,
                maxHealth = _maxHealth,
                currentWave = _currentWave,
                totalWaves = _totalWaves,
                zombiesDefeated = _zombiesDefeated,
                highestWaveReached = _highestWaveReached,
                cheatModeEnabled = CheatModeEnabled,
                lastSavedUtcTicks = DateTime.UtcNow.Ticks,
                unlockedPlants = new List<string>(_unlockedPlants),
                plantCooldownIds = new List<string>(),
                plantCooldownRemain = new List<float>(),
                plantCooldownTotal = new List<float>(),
            };
            foreach (var kv in _plantCooldowns)
            {
                data.plantCooldownIds.Add(kv.Key);
                data.plantCooldownRemain.Add(kv.Value);
                data.plantCooldownTotal.Add(_plantCooldownTotals.TryGetValue(kv.Key, out var t) ? t : kv.Value);
            }
            return data;
        }

        internal static void ApplySnapshot(SaveData data)
        {
            if (data == null) return;
            _sun = Mathf.Max(0, data.sun);
            _coins = Mathf.Max(0, data.coins);
            _maxHealth = Mathf.Max(1, data.maxHealth);
            _health = Mathf.Clamp(data.health, 0, _maxHealth);
            _currentWave = Mathf.Max(0, data.currentWave);
            _totalWaves = Mathf.Max(0, data.totalWaves);
            _zombiesDefeated = Mathf.Max(0, data.zombiesDefeated);
            _highestWaveReached = Mathf.Max(0, data.highestWaveReached);
            CheatModeEnabled = data.cheatModeEnabled;

            _unlockedPlants.Clear();
            if (data.unlockedPlants != null)
            {
                foreach (var id in data.unlockedPlants) _unlockedPlants.Add(id);
            }

            _plantCooldowns.Clear();
            _plantCooldownTotals.Clear();
            if (data.plantCooldownIds != null)
            {
                for (int i = 0; i < data.plantCooldownIds.Count; i++)
                {
                    var id = data.plantCooldownIds[i];
                    _plantCooldowns[id] = i < data.plantCooldownRemain.Count ? data.plantCooldownRemain[i] : 0f;
                    _plantCooldownTotals[id] = i < data.plantCooldownTotal.Count ? data.plantCooldownTotal[i] : 1f;
                }
            }

            // Fire events so UI rebuilds from loaded state.
            OnSunChanged?.Invoke(_sun);
            OnCoinsChanged?.Invoke(_coins);
            OnHealthChanged?.Invoke(_health, _maxHealth);
            OnWaveProgressed?.Invoke(_currentWave, _totalWaves);
            OnZombieDefeated?.Invoke(_zombiesDefeated);
            foreach (var id in _unlockedPlants) OnPlantUnlocked?.Invoke(id);
            foreach (var kv in _plantCooldowns)
            {
                var total = _plantCooldownTotals.TryGetValue(kv.Key, out var t) ? t : 1f;
                OnPlantCooldownTick?.Invoke(kv.Key, kv.Value, total);
            }
        }
    }
}
