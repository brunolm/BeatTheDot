using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BeatTheDot
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            this.BindingContext = UserSettings.Load();
        }

        public void Save(object sender, EventArgs e)
        {
            var settings = this.BindingContext as Models.Settings;

            UserSettings.Save(settings);

            DisplayAlert("Settings saved!", "Successfully saved settings.", "OK");
        }
    }
}