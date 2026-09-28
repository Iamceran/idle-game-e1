using Idle_game.Models;
using Idle_game.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

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

        // [Requirement 3 & 5] Upgrade definition with ID, Base Cost, and Multiplier
        private readonly Upgrade autoClicker = new()
        {
            ID = "autoclicker",
            Name = "Auto-Clicker",
            BaseCost = 10,
            CostMultiplier = 1.15,
            IncomePerSecond = 0.5
        };

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(MyTitleBar);

            SetupTimers();
            UpdateUI();

            // [Requirement 7] Automatically try loading saved progress on startup
            TryAutoLoad();
        }

        private void SetupTimers()
        {
            // [Requirement 1] Core Game Loop: runs 10 times per second (100ms interval)
            gameLoopTimer.Interval = TimeSpan.FromMilliseconds(100);
            gameLoopTimer.Tick += (sender, e) => ProcessGameTick();
            gameLoopTimer.Start();

            // [Requirement 4] Independent Automation Logging Timer: triggers every 10 seconds
            automationLogTimer.Interval = TimeSpan.FromSeconds(10);
            automationLogTimer.Tick += (sender, e) => LogAutomatedIncome();
            automationLogTimer.Start();

            // [Requirement 6] Auto-Save Timer: automatically saves progress every 30 seconds
            autoSaveTimer.Interval = TimeSpan.FromSeconds(30);
            autoSaveTimer.Tick += (sender, e) => SaveGame(isAutoSave: true);
            autoSaveTimer.Start();
        }

        // [Requirement 1] Incremental tick processing (10Hz)
        private void ProcessGameTick()
        {
            // Calculate base income for 0.1s
            double baseTickIncome = baseIncomePerSecond * 0.1;

            // Calculate actual automated income for 0.1s
            double autoTickIncome = (autoClicker.AmountOwned * autoClicker.IncomePerSecond) * 0.1;

            // Add both to total currency
            currency += baseTickIncome + autoTickIncome;

            // Track automated earnings for periodic logging
            accumulatedAutomatedEarnings += autoTickIncome;

            // Refresh HUD display continuously
            UpdateUI();
        }

        // [Requirement 4] Automated income logging
        private void LogAutomatedIncome()
        {
            if (accumulatedAutomatedEarnings > 0)
            {
                AddLogEntry($"[Automation] {autoClicker.Name} generated +{accumulatedAutomatedEarnings:F1} currency in 10 seconds.");

                // Reset counter for the next 10-second cycle
                accumulatedAutomatedEarnings = 0;
            }
        }

        // [Requirement 1 & 3] Updating UI elements and button states
        private void UpdateUI()
        {
            // [Requirement 1] Update HUD display (Currency & Total Income per second)
            CurrencyDisplay.Text = $"Currency: {Math.Floor(currency)}";
            IncomeDisplay.Text = $"Income: {CalculateTotalIncome():F1} /sec";

            // [Requirement 3] Display upgrade info (Name, Owned Count, Current Scaled Cost, Effect)
            AutoClickerTitle.Text = $"{autoClicker.Name} (Owned: {autoClicker.AmountOwned})";
            AutoClickerCost.Text = $"Cost: {Math.Ceiling(autoClicker.CurrentCost)} | +{autoClicker.IncomePerSecond}/sec";

            // [Requirement 3] Disable buy button if player cannot afford the upgrade
            UpgradeAutoClickerButton.IsEnabled = currency >= autoClicker.CurrentCost;
        }

        private double CalculateTotalIncome()
        {
            return baseIncomePerSecond + (autoClicker.AmountOwned * autoClicker.IncomePerSecond);
        }

        // [Requirement 2] Manual action: Button click adds currency
        private void ClickButton_Click(object sender, RoutedEventArgs e)
        {
            currency += 1;
            UpdateUI();
        }

        // [Requirement 3 & 5] Purchasing upgrades with exponential cost scaling
        private void BuyAutoClicker_Click(object sender, RoutedEventArgs e)
        {
            double cost = autoClicker.CurrentCost;

            if (currency >= cost)
            {
                currency -= cost;

                // [Requirement 5] Incrementing AmountOwned triggers Math.Pow cost multiplier in Upgrade.cs
                autoClicker.AmountOwned++;

                AddLogEntry($"Purchased: {autoClicker.Name} for {Math.Ceiling(cost)} currency.");
                UpdateUI();
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

        // [Requirement 6] Centralized Save logic (used by manual Save button and Auto-Save timer)
        private void SaveGame(bool isAutoSave = false)
        {
            SaveState state = new()
            {
                Currency = currency,
                LastSaved = DateTime.Now,
                Upgrades = new List<UpgradeSaveData>
                {
                    new UpgradeSaveData
                    {
                        ID = autoClicker.ID,
                        AmountOwned = autoClicker.AmountOwned
                    }
                }
            };

            bool isSuccess = SaveService.Save(state);

            if (isSuccess)
            {
                string prefix = isAutoSave ? "[Auto-Save]" : "[Save]";
                AddLogEntry($"{prefix} Game saved successfully!");
            }
            else if (!isAutoSave)
            {
                AddLogEntry("Failed to save game.");
            }
        }

        // [Requirement 6] Manual Save button click handler
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveGame(isAutoSave: false);
        }

        // [Requirement 7] Manual Load button click handler
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
            AddLogEntry($"Game loaded! Progress restored from {loadedState.LastSaved:HH:mm:ss}.");
        }

        // [Requirement 7] Restores loaded save data back into game variables
        private void ApplySaveState(SaveState state)
        {
            currency = state.Currency;

            UpgradeSaveData? savedAutoClicker = state.Upgrades
                .FirstOrDefault(u => u.ID == autoClicker.ID);

            if (savedAutoClicker != null)
            {
                autoClicker.AmountOwned = savedAutoClicker.AmountOwned;
            }

            UpdateUI();
        }

        // [Requirement 7] Automatic save data restoration on startup
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
            autoClicker.AmountOwned = 0;
            UpdateUI();
        }
    }
}