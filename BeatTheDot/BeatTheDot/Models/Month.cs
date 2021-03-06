﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace BeatTheDot.Models
{
    public class Month : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
    
        public string Grid { get; set; }

        public string LastFetchAt { get; set; }
        public string Loading { get; set; }
    }
}
