using BeatTheDot.Models;
using BeatTheDot.Services;
using System;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BeatTheDot
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class BeatsPage : ContentPage
    {
        public BeatsPage()
        {
            InitializeComponent();

            this.BindingContext = new Beats();
        }

        public async void FetchTimes(object sender, EventArgs e)
        {
            var vm = this.BindingContext as Beats;

            try
            {
                vm.Loading = "Loading...";

                var times = await Ahgora.Instance.GetTimes();

                vm.LastFetchAt = DateTime.Now.ToString("HH:mm");
                vm.Loading = "";
                vm.BeatsRaw = Ahgora.Instance.ParseResult(times);
                vm.HoursToday = Ahgora.Instance.HoursWorked(times);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await DisplayAlert("Error", "Server error, try again later.", "OK");
            }
        }
    }
}