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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Idle_game
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private float number;
        private float autoIncreaser = 0.1f;
        private static System.Timers.Timer timer;

        public MainWindow()
        {
            InitializeComponent();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(MyTitleBar);
            SetTimer();
        }

        private void SetTimer()
        {
            timer = new System.Timers.Timer(100);
            timer.Elapsed += (sender, e) =>
            {
                Timer();
            };
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        private void Timer()
        {
            number += autoIncreaser;
            DispatcherQueue.TryEnqueue(() =>
            {
                CounterDisplay.Text = number.ToString("F0");
            });
        }

        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            number++;
            CounterDisplay.Text = number.ToString("F0");
        }

        private void UpgradeButton_Click(object sender, RoutedEventArgs e)
        {
            autoIncreaser += 0.1f;
        }
    }
}
