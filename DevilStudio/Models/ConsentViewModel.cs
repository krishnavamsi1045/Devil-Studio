using DevilStudio.Views;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DevilStudio.Models
{
    public class ConsentViewModel : INotifyPropertyChanged
    {
        private bool _isAgreementChecked;
        public ICommand OpenPrivacyPolicyCommand { get; }


        public ConsentViewModel()
        {
            OpenPrivacyPolicyCommand = new Command(OpenPrivacyPolicy);
        }
        private async void OpenPrivacyPolicy()
        {
            // Navigate to the Privacy Policy page, or show it in a webview
            await Application.Current.MainPage.Navigation.PushAsync(new BeforeSignupprivacypolicy());
        }
        public bool IsAgreementChecked
        {
            get => _isAgreementChecked;
            set
            {
                if (_isAgreementChecked != value)
                {
                    _isAgreementChecked = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand OpenCookiePolicyCommand => new Command(() =>
        {
            // Replace with your cookie policy URL or navigation logic
            Browser.OpenAsync("https://www.moneycontrol.com/cdata/gdpr_cookiepolicy.php", BrowserLaunchMode.SystemPreferred);
        });

    }
}
