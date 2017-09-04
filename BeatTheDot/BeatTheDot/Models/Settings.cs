using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;

namespace BeatTheDot.Models
{
    public class Settings
    {
        public string Company { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public int Tolerance { get; set; } = 10;
        public int LunchTime { get; set; } = 60;
        public TimeSpan LunchAt { get; set; } = new TimeSpan(11, 30, 0);
        public TimeSpan Worktime { get; set; } = new TimeSpan(8, 0, 0);
    }
}
