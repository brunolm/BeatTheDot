using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Xml.Serialization;
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

        private TimeSpan lunchAt { get; set; } = new TimeSpan(11, 30, 0);

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), XmlIgnore]
        public TimeSpan LunchAt
        {
            get { return this.lunchAt; }
            set { this.lunchAt = value; }
        }

        [XmlElement("LunchAt")]
        public long LunchAtTicks
        {
            get { return this.lunchAt.Ticks; }
            set { this.lunchAt = new TimeSpan(value); }
        }

        private TimeSpan worktime { get; set; } = new TimeSpan(8, 0, 0);

        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never), XmlIgnore]
        public TimeSpan Worktime
        {
            get { return this.worktime; }
            set { this.worktime = value; }
        }

        [XmlElement("Worktime")]
        public long WorktimeTicks
        {
            get { return this.worktime.Ticks; }
            set { this.worktime = new TimeSpan(value); }
        }
    }
}
