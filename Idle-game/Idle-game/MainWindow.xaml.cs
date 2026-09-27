using Idle_game.Models;
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

        private readonly DispatcherTimer gameLoopTimer = new();
        private readonly DispatcherTimer automationLogTimer = new();

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
        }

        private void SetupTimers()
        {
            // Game Loop: runs 10 times per second (every 100ms) to update currency & HUD
            gameLoopTimer.Interval = TimeSpan.FromMilliseconds(100);
            gameLoopTimer.Tick += (sender, e) => ProcessGameTick();
            gameLoopTimer.Start();

            // Automation Timer: logs automated income every 10 seconds
            automationLogTimer.Interval = TimeSpan.FromSeconds(10);
            automationLogTimer.Tick += (sender, e) => LogAutomatedIncome();
            automationLogTimer.Start();
        }

        private void ProcessGameTick()
        {
            // 1. Calculate base income for 0.1s
            double baseTickIncome = baseIncomePerSecond * 0.1;

            // 2. Calculate actual automated income for 0.1s
            double autoTickIncome = (autoClicker.AmountOwned * autoClicker.IncomePerSecond) * 0.1;

            // 3. Add both to total currency
            currency += baseTickIncome + autoTickIncome;

            // 4. Track actual automated earnings for the log
            accumulatedAutomatedEarnings += autoTickIncome;

            // 5. Refresh the UI
            UpdateUI();
        }

        private void LogAutomatedIncome()
        {
            if (accumulatedAutomatedEarnings > 0)
            {
                AddLogEntry($"[Automation] {autoClicker.Name} generated +{accumulatedAutomatedEarnings:F1} currency.");

                // Reset counter for the next 10-second cycle
                accumulatedAutomatedEarnings = 0;
            }
        }

        private void UpdateUI()
        {
            // Update HUD
            CurrencyDisplay.Text = $"Currency: {Math.Floor(currency)}";
            IncomeDisplay.Text = $"Income: {CalculateTotalIncome():F1} /sec";

            // Update Upgrade Button text
            AutoClickerTitle.Text = $"{autoClicker.Name} (Owned: {autoClicker.AmountOwned})";
            AutoClickerCost.Text = $"Cost: {Math.Ceiling(autoClicker.CurrentCost)} | +{autoClicker.IncomePerSecond}/sec";

            // Disable button if player doesn't have enough currency (Requirement 3)
            UpgradeAutoClickerButton.IsEnabled = currency >= autoClicker.CurrentCost;
        }

        private double CalculateTotalIncome()
        {
            return baseIncomePerSecond + (autoClicker.AmountOwned * autoClicker.IncomePerSecond);
        }

        // Manual click action
        private void ClickButton_Click(object sender, RoutedEventArgs e)
        {
            currency += 1;
            UpdateUI();
        }

        // Purchasing an upgrade
        private void BuyAutoClicker_Click(object sender, RoutedEventArgs e)
        {
            double cost = autoClicker.CurrentCost;

            if (currency >= cost)
            {
                currency -= cost;
                autoClicker.AmountOwned++;

                AddLogEntry($"Purchased: {autoClicker.Name} for {Math.Ceiling(cost)} currency.");
                UpdateUI();
            }
        }

        // Helper method to add messages to the Action Log
        private void AddLogEntry(string message)
        {
            LogListView.Items.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {message}");

            if (LogListView.Items.Count > 50)
            {
                LogListView.Items.RemoveAt(50);
            }
        }
    }
}
