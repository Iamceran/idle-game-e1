using Idle_game.Models;
using Idle_game.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Idle_game
{
    public sealed partial class MainWindow : Window
    {
        private double currency = 0;
        private double accumulatedAutomatedEarnings = 0;
        private readonly double baseIncomePerSecond = 0.1;

        // [Requirement 1, 4 & 6] Independent DispatcherTimers for game loop, logging, and auto-saving
        private readonly DispatcherTimer gameLoopTimer = new();
        private readonly DispatcherTimer automationLogTimer = new();
        private readonly DispatcherTimer autoSaveTimer = new();

        // [Requirement 3 & 5] Multi-tier Upgrade definitions scaled by store sizes (Pokemon Card Shop Theme)
        private readonly List<Upgrade> upgrades = new()
        {
            new Upgrade { ID = "small_cardshop", Name = "Small Card Shop", BaseCost = 10, CostMultiplier = 1.15, IncomePerSecond = 0.5 },
            new Upgrade { ID = "medium_cardshop", Name = "Medium Card Shop", BaseCost = 100, CostMultiplier = 1.15, IncomePerSecond = 4.0 },
            new Upgrade { ID = "global_cardshop", Name = "Global Card Shop", BaseCost = 1100, CostMultiplier = 1.15, IncomePerSecond = 32.0 }
        };

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(MyTitleBar);

            SetupTimers();
            UpdateUI();

            // Prepare Start Screen options
            CheckSaveStatusForStartScreen();
        }

        private void SetupTimers()
        {
            // Core Game Loop: runs 10 times per second (100ms interval)
            gameLoopTimer.Interval = TimeSpan.FromMilliseconds(100);
            gameLoopTimer.Tick += (sender, e) => ProcessGameTick();

            // Independent Automation Logging Timer: triggers every 10 seconds
            automationLogTimer.Interval = TimeSpan.FromSeconds(10);
            automationLogTimer.Tick += (sender, e) => LogAutomatedIncome();

            // Auto-Save Timer: automatically saves progress every 30 seconds
            autoSaveTimer.Interval = TimeSpan.FromSeconds(30);
            autoSaveTimer.Tick += (sender, e) => SaveGame(isAutoSave: true);
        }

        private void StartTimers()
        {
            gameLoopTimer.Start();
            automationLogTimer.Start();
            autoSaveTimer.Start();
        }

        // Checks save file status to enable/disable Continue button on Start Screen
        private void CheckSaveStatusForStartScreen()
        {
            if (SaveService.SaveExists())
            {
                var (loadedState, isCorrupted) = SaveService.Load();

                if (isCorrupted)
                {
                    ContinueButton.IsEnabled = false;
                    StartScreenSaveInfo.Text = "Warning: Corrupted save file detected.";
                }
                else if (loadedState != null)
                {
                    ContinueButton.IsEnabled = true;
                    StartScreenSaveInfo.Text = $"Save found: €{Math.Floor(loadedState.Currency)} (Last saved {loadedState.LastSaved:HH:mm:ss})";
                }
            }
            else
            {
                ContinueButton.IsEnabled = false;
                StartScreenSaveInfo.Text = "No previous save found. Click 'Start New Game' to play!";
            }
        }

        // Start Screen Handler: Continue existing save
        private void ContinueGame_Click(object sender, RoutedEventArgs e)
        {
            TryAutoLoad();
            StartScreenOverlay.Visibility = Visibility.Collapsed;
            StartTimers();
        }

        // Start Screen Handler: Start a fresh game
        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            ResetToDefault();
            StartScreenOverlay.Visibility = Visibility.Collapsed;
            StartTimers();
            AddLogEntry("Welcome! Your new store empire has begun.");
        }

        // Start Screen Handler: Exit Application
        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // [Requirement 1] Incremental tick processing (10Hz)
        private void ProcessGameTick()
        {
            double baseTickIncome = baseIncomePerSecond * 0.1;
            double autoTickIncome = upgrades.Sum(u => u.AmountOwned * u.IncomePerSecond) * 0.1;

            currency += baseTickIncome + autoTickIncome;
            accumulatedAutomatedEarnings += autoTickIncome;

            UpdateUI();
        }

        // [Requirement 4] Automated income logging
        private void LogAutomatedIncome()
        {
            if (accumulatedAutomatedEarnings > 0)
            {
                AddLogEntry($"[Sales] Store operations generated +€{accumulatedAutomatedEarnings:F1} in 10 seconds.");
                accumulatedAutomatedEarnings = 0;
            }
        }

        // [Requirement 1 & 3] Updating UI elements and button states
        private void UpdateUI()
        {
            CurrencyDisplay.Text = $"Currency: €{Math.Floor(currency)}";
            IncomeDisplay.Text = $"Income: €{CalculateTotalIncome():F1} /sec";

            var smallCardshop = GetUpgrade("small_cardshop");
            if (smallCardshop != null)
            {
                SmallStoreTitle.Text = $"{smallCardshop.Name} (Owned: {smallCardshop.AmountOwned})";
                SmallStoreCost.Text = $"Cost: €{Math.Ceiling(smallCardshop.CurrentCost)} | +€{smallCardshop.IncomePerSecond}/sec";
                UpgradeSmallStoreButton.IsEnabled = currency >= smallCardshop.CurrentCost;
            }

            var mediumCardshop = GetUpgrade("medium_cardshop");
            if (mediumCardshop != null)
            {
                MediumStoreTitle.Text = $"{mediumCardshop.Name} (Owned: {mediumCardshop.AmountOwned})";
                MediumStoreCost.Text = $"Cost: €{Math.Ceiling(mediumCardshop.CurrentCost)} | +€{mediumCardshop.IncomePerSecond}/sec";
                UpgradeMediumStoreButton.IsEnabled = currency >= mediumCardshop.CurrentCost;
            }

            var globalCardshop = GetUpgrade("global_cardshop");
            if (globalCardshop != null)
            {
                GlobalStoreTitle.Text = $"{globalCardshop.Name} (Owned: {globalCardshop.AmountOwned})";
                GlobalStoreCost.Text = $"Cost: €{Math.Ceiling(globalCardshop.CurrentCost)} | +€{globalCardshop.IncomePerSecond}/sec";
                UpgradeGlobalStoreButton.IsEnabled = currency >= globalCardshop.CurrentCost;
            }
        }

        private Upgrade GetUpgrade(string id) => upgrades.FirstOrDefault(u => u.ID == id);

        private double CalculateTotalIncome()
        {
            return baseIncomePerSecond + upgrades.Sum(u => u.AmountOwned * u.IncomePerSecond);
        }

        // [Requirement 2] Manual action: Button click adds currency
        private void ClickButton_Click(object sender, RoutedEventArgs e)
        {
            currency += 1;
            UpdateUI();
        }

        // [Requirement 3 & 5] Purchasing upgrades with exponential cost scaling
        private void BuyUpgrade_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string upgradeId)
            {
                Upgrade upgrade = GetUpgrade(upgradeId);
                if (upgrade == null) return;

                double cost = upgrade.CurrentCost;

                if (currency >= cost)
                {
                    currency -= cost;
                    upgrade.AmountOwned++;

                    AddLogEntry($"Purchased: {upgrade.Name} for €{Math.Ceiling(cost)}.");
                    UpdateUI();
                }
            }
        }

        // [Requirement 4] Action Log helper method
        private void AddLogEntry(string message)
        {
            LogListView.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

            if (LogListView.Items.Count > 50)
            {
                LogListView.Items.RemoveAt(50);
            }
        }

        // [Requirement 6] Centralized Save logic
        private void SaveGame(bool isAutoSave = false)
        {
            SaveState state = new()
            {
                Currency = currency,
                LastSaved = DateTime.Now,
                Upgrades = upgrades.Select(u => new UpgradeSaveData
                {
                    ID = u.ID,
                    AmountOwned = u.AmountOwned
                }).ToList()
            };

            bool isSuccess = SaveService.Save(state);

            if (isSuccess)
            {
                string prefix = isAutoSave ? "[Auto-Save]" : "[Save]";
                AddLogEntry($"{prefix} Store empire saved successfully!");
            }
            else if (!isAutoSave)
            {
                AddLogEntry("Failed to save store empire.");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveGame(isAutoSave: false);
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var (loadedState, isCorrupted) = SaveService.Load();

            if (isCorrupted)
            {
                ResetToDefault();
                AddLogEntry("Error: Save file was corrupted and could not be loaded. Reset to default state.");
                return;
            }

            if (loadedState == null)
            {
                AddLogEntry("No save file found.");
                return;
            }

            ApplySaveState(loadedState);
            AddLogEntry($"Store empire loaded! Progress restored from {loadedState.LastSaved:HH:mm:ss}.");
        }

        private void ApplySaveState(SaveState state)
        {
            currency = state.Currency;

            foreach (var savedData in state.Upgrades)
            {
                var upgrade = upgrades.FirstOrDefault(u => u.ID == savedData.ID);
                if (upgrade != null)
                {
                    upgrade.AmountOwned = savedData.AmountOwned;
                }
            }

            UpdateUI();
        }

        private void TryAutoLoad()
        {
            if (SaveService.SaveExists())
            {
                var (loadedState, isCorrupted) = SaveService.Load();

                if (isCorrupted)
                {
                    ResetToDefault();
                    AddLogEntry("Warning: Save file was corrupted! Restored fresh default game.");
                }
                else if (loadedState != null)
                {
                    ApplySaveState(loadedState);
                    AddLogEntry($"Auto-loaded progress from {loadedState.LastSaved:HH:mm:ss}.");
                }
            }
        }

        private void ResetToDefault()
        {
            currency = 0;
            foreach (var upgrade in upgrades)
            {
                upgrade.AmountOwned = 0;
            }
            UpdateUI();
        }
    }
}