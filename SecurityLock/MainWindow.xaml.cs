using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Forms;
using System.IO;
using AudioSwitcher.AudioApi.CoreAudio;

namespace SecurityLock
{
    public partial class MainWindow : Window
    {
        private bool _isRunningOnBattery;
        private bool _armed = false;
        private bool _shutdownTime = false;

        public MainWindow()
        {
            InitializeComponent();
            InitialStart();

            NotifyIcon ni = new NotifyIcon();
            ni.Icon = Properties.Resources.lockIcon;
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();

            base.OnStateChanged(e);
        }

        private async void InitialStart()
        {
            ArmStatus.Text = "Status: Unknown";
            Task.Run(() => PowerStatusListen(ArmStatus));
        }

        public void AddPhoneNumberClick(object sender, RoutedEventArgs e)
        {
            //TODO: implement
            //string phoneNumber = PhoneTextBox.Text;
            //PhoneTextBox.Text = "";
        }

        internal void PowerStatusListen(TextBlock statusBlock)
        {
            while (!_shutdownTime)
            {
                _isRunningOnBattery =
                SystemInformation.PowerStatus.PowerLineStatus ==
                System.Windows.Forms.PowerLineStatus.Offline;
                string statusText = _isRunningOnBattery ? "Unplugged" : "Plugged In";
                this.Dispatcher.Invoke(() =>
                {
                    ArmStatus.Text = "Status: " + statusText;
                    _ = _isRunningOnBattery ? (ArmStatus.Background = Brushes.Red) : (ArmStatus.Background = Brushes.Green);
                    _ = (_isRunningOnBattery && !_armed) ? ArmButton.IsEnabled = false : ArmButton.IsEnabled = true;
                });
                System.Threading.Thread.Sleep(50);
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _shutdownTime = true;
            System.Threading.Thread.Sleep(100);
            System.Windows.Application.Current.Shutdown();
        }

        public void Disarm(object sender, EventArgs e)
        {
            _armed = false;
            ArmButton.Click -= Arm;
            ArmButton.Click += Arm;
            ArmButton.Content = "Arm";
            System.Threading.Thread.Sleep(100);
            DisplayArmStatus.Text = "Not Armed";
            DisplayArmStatus.Foreground = Brushes.Green;
        }

        private async void Arm(object sender, EventArgs e)
        {
            _armed = true;
            Task.Run(() => ArmedMode());
            ArmButton.Content = "Disarm";
            ArmButton.Click -= Disarm;
            ArmButton.Click += Disarm;
            DisplayArmStatus.Text = "Armed";
            DisplayArmStatus.Foreground = Brushes.Red;
        }

        internal void ArmedMode()
        {
            while (_armed)
            {
                if (_armed && _isRunningOnBattery)
                {
                    CoreAudioDevice defaultPlaybackDevice = new CoreAudioController().DefaultPlaybackDevice;
                    defaultPlaybackDevice.Volume = 100;
                    Stream alarmStream = Properties.Resources.Alarm;
                    System.Media.SoundPlayer alarmRecording = new System.Media.SoundPlayer(alarmStream);
                    Dispatcher.Invoke(() =>
                    {
                        if (_armed)
                        {
                            DisplayArmStatus.Text = "Alarm Triggered!";
                        }
                    });
                    alarmRecording.Play();
                    for (int i = 0; i < 880; i++)
                    {
                        System.Threading.Thread.Sleep(100);
                        if (_armed)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }
                    alarmRecording.Stop();
                    alarmRecording.Dispose();
                    alarmStream.Dispose();
                    defaultPlaybackDevice.Dispose();
                }
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}