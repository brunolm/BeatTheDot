using BeatTheDot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BeatTheDot
{
    // [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MonthPage : ContentPage
    {
        public MonthPage()
        {
            InitializeComponent();
        }

        public async void FetchTimes(object sender, EventArgs e)
        {
            var settings = UserSettings.Load();

            await Ahgora.Instance.Login(settings.Company, settings.User, settings.Pass);
            var times = await Ahgora.Instance.GetTimes();
        }
    }
}