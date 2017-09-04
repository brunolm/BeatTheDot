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
    public partial class BeatsPage : ContentPage
    {
        public BeatsPage()
        {
            InitializeComponent();
        }

        public void FetchTimes(object sender, EventArgs e)
        {
            labelLastFetch.Text = DateTime.Now.ToString("HH:mm");

            foreach (var item in DateTimeFormatInfo.CurrentInfo.MonthNames)
            {
                pickerMonth.Items.Add(item);
            }

        }
    }
}