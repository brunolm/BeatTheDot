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
            
            var times = await Ahgora.Instance.GetTimes();

            vm.Grid = Ahgora.Instance.ParseGrid(times);
        }
    }
}