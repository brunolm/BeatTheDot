using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }

        public void FetchTimes(object sender, EventArgs e)
        {
            labelLastFetch.Text = DateTime.Now.ToString("HH:mm");


        }
    }
}