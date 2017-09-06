using BeatTheDot.Models;
using BeatTheDot.Services;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace BeatTheDot
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MonthPage : ContentPage
    {
        public MonthPage()
        {
            InitializeComponent();
            this.BindingContext = new Month();
        }

        public async void FetchTimes(object sender, EventArgs e)
        {
            var vm = this.BindingContext as Month;

            try
            {
                vm.Loading = "Loading...";

                var times = await Ahgora.Instance.GetTimes();

                vm.LastFetchAt = DateTime.Now.ToString("HH:mm");
                vm.Loading = "";
                vm.Grid = Ahgora.Instance.ParseGrid(times);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                await DisplayAlert("Error", "Server error, try again later.", "OK");
            }
        }
    }
}