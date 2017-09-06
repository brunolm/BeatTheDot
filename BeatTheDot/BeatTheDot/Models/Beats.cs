using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BeatTheDot.Models
{
    public class Beats : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string BeatsRaw { get; set; }
        public string HoursToday { get; set; }

        public string LastFetchAt { get; set; }
        public string Loading { get; set; }
    }
}
