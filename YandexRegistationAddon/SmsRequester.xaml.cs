using MahApps.Metro.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

namespace YandexRegistationAddon
{
    /// <summary>
    /// Interaction logic for SmsRequester.xaml
    /// </summary>
    public partial class SmsRequester : MetroWindow, INotifyPropertyChanged
    {
        private readonly string _phoneNumber;
        private string _code = string.Empty;
        private int _timeSpend = 0;
        private System.Timers.Timer _timer = new System.Timers.Timer();

        public SmsRequester(string phone, int secondsAwait = 120)
        {
            InitializeComponent();
            _phoneNumber = phone;
            TimeSpend = secondsAwait;
            _timer.AutoReset = true;
            _timer.Interval = 1000; // 1 second
            _timer.Elapsed += (sender, e) =>
            {
                if (TimeSpend <= 0)
                {
                    _timer.Stop();
                    DialogResult = false;
                    Close();
                    return;
                }
                TimeSpend--;
            };
        }

        public string PhoneNumber => _phoneNumber;

        public string Code
        {
            get => _code;
            set
            {
                _code = value;
                OnPropertyChanged();
            }
        }

        public int TimeSpend
        {
            get => _timeSpend;
            set
            {
                _timeSpend = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged = delegate { };
        private void OnPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            _timer.Stop();
            Close();
        }

        public void StartStopwatch()
        {
            _timer.Start();
        }
    }
}
