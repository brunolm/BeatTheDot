using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

using HtmlAgilityPack;
using System.Linq;

namespace BeatTheDot.Services
{
    public class CookieAwareWebClient : WebClient
    {
        public CookieAwareWebClient()
        {
            this.BaseAddress = "https://www.ahgora.com.br";
            this.Headers.Add(HttpRequestHeader.Host, "www.ahgora.com.br");
            this.Headers.Add(HttpRequestHeader.Referer, this.BaseAddress);
            this.Headers.Add(HttpRequestHeader.Pragma, "no-cache");
        }
    }

    public class BeatInfo
    {
        public string[] Beats { get; set; }
        public string BeatsRaw { get; set; }
        public DateTime Date { get; set; }
    }

    public class Ahgora
    {
        private readonly string hhmm = "hh':'mm";
        private readonly Random rnd = new Random();
        private CookieAwareWebClient client = new CookieAwareWebClient();

        private static readonly Lazy<Ahgora> instance = new Lazy<Ahgora>(() => new Ahgora(), true);

        public Models.Settings Settings
        {
            get { return UserSettings.Load(); }
        }

        public static Ahgora Instance
        {
            get { return instance.Value; }
        }

        private Ahgora()
        {
        }

        public async Task<string> Login(string company, string user, string pass)
        {
            string cookie = null;
            try
            {
                var postData = $"empresa={company}&matricula={user}&senha={pass}";
                var postBytes = Encoding.UTF8.GetBytes(postData);

                this.client.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
                await this.client.UploadDataTaskAsync("/externo/login", postBytes);

                var cookieHeader = this.client.ResponseHeaders["Set-Cookie"];
                cookie = cookieHeader != null ? cookieHeader.Split(';')[0] : this.client.Headers[HttpRequestHeader.Cookie];

                this.client.Headers.Add(HttpRequestHeader.Cookie, cookie);
                this.client.Headers.Remove(HttpRequestHeader.ContentType);
            }
            catch
            {
            }

            return cookie;
        }

        public async Task<Dictionary<string, BeatInfo>> GetTimes()
        {
            if (this.client.Headers[HttpRequestHeader.Cookie] == null)
            {
                var settings = Settings;
                await Ahgora.Instance.Login(settings.Company, settings.User, settings.Pass);
            }

            var monthYear = DateTime.Now.ToString("MM-yyyy");
            var url = $"/externo/batidas/{monthYear}?nocache=true&breaker={this.rnd.Next()}";

            var page = await client.DownloadStringTaskAsync(url);
            return ParseHTML(page);
        }

        private Dictionary<string, BeatInfo> ParseHTML(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var tables = doc.DocumentNode.SelectNodes("//table");
            var tableTotalize = tables[0];
            var timetable = tables[1];

            var summary = Regex.Replace(tableTotalize.InnerText, @" {2,}", "");
            summary = Regex.Replace(summary, @"(\s?\n){2,}", "\n").Trim();

            var trs = timetable.SelectNodes(timetable.XPath + "/tbody/tr");

            var times = new Dictionary<string, BeatInfo>();
            foreach (var element in trs)
            {
                if (element.SelectNodes(element.XPath + "//td").Count <= 3)
                {
                    continue;
                }

                var col = element.SelectSingleNode(element.XPath + "/td[1]");
                var key = col.ChildNodes[0].InnerText.Trim();

                var beatsRaw = col.ChildNodes[3].InnerText.Trim();
                var beats = beatsRaw.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                var date = DateTime.ParseExact(key, "dd/MM/yyyy", CultureInfo.CurrentCulture);

                times[key] = new BeatInfo { Beats = beats, BeatsRaw = beatsRaw, Date = date };
            }

            return times;
        }

        public string ParseGrid(Dictionary<string, BeatInfo> times)
        {
            var sb = new StringBuilder();

            foreach (KeyValuePair<string, BeatInfo> item in times)
            {
                if (item.Value.Beats.Length == 0)
                {
                    continue;
                }

                sb.AppendLine($"{item.Key} - {item.Value.BeatsRaw} ({HoursWorked(item.Value.Beats)})");

                var scenarios = Calc(item.Value.Beats);
                if (scenarios != null)
                {
                    sb.AppendLine("Corrections:");
                    sb.AppendLine(scenarios);
                    sb.AppendLine("");
                }
            }

            return sb.ToString();
        }

        private TimeSpan GetTime(string beatTime) =>
            beatTime != null
                ? TimeSpan.ParseExact(beatTime, "g", CultureInfo.CurrentCulture)
                : TimeSpan.Zero;

        public string HoursWorked(Dictionary<string, BeatInfo> times)
        {
            return HoursWorked(GetToday(times).Beats);
        }

        public string HoursWorked(string[] beats)
        {
            switch (beats.Length)
            {
                case 4:
                    var total = GetTime(beats[3]) - GetTime(beats[2]) + GetTime(beats[1]) - GetTime(beats[0]);
                    return total.ToString(hhmm);
                case 3:
                    return (GetTime(beats[1]) - GetTime(beats[0])).ToString(hhmm) + "~";
                case 2:
                    return (GetTime(beats[1]) - GetTime(beats[0])).ToString(hhmm);
                case 1:
                    return "00~";
                case 0:
                default:
                    return "00";
            }
        }

        public string Calc(string[] beats)
        {
            TimeSpan t1, t2, t3, t4;

            t1 = GetTime(beats.ElementAtOrDefault(0));
            t2 = GetTime(beats.ElementAtOrDefault(1));
            t3 = GetTime(beats.ElementAtOrDefault(2));
            t4 = GetTime(beats.ElementAtOrDefault(3));

            var settings = Settings;
            
            switch (beats.Length)
            {
                case 1:
                    var halfTime = settings.Worktime.Ticks / 2;
                    var before = t1.Subtract(new TimeSpan(halfTime)).ToString(hhmm);
                    var after = t1.Add(new TimeSpan(halfTime)).ToString(hhmm);
                    return "  " + String.Join(", ", beats) + $", *{after}*"
                        + $"\n  *{after}*, " + String.Join(", ", beats);
                case 2:
                    return null;
                case 3:
                    var endOfDay = (settings.Worktime - (t2 - t1) + t3).ToString(hhmm);
                    var startOfDay = (settings.Worktime - (t3 - t2) - t1).ToString(hhmm);
                    return "  " + String.Join(", ", beats) + $", *{endOfDay}*"
                        + $"\n  *{startOfDay}*, " + String.Join(", ", beats);
                case 4:
                    var morning = t2 - t1;
                    var afternoon = t4 - t3;

                    var workedTime = morning + afternoon;
                    var workTime = settings.Worktime;

                    var min = workTime.Subtract(TimeSpan.FromMinutes(settings.Tolerance));
                    var max = workTime.Add(TimeSpan.FromMinutes(settings.Tolerance));
                    if (workedTime < min || workedTime > max)
                    {
                        var endOfDayFix = (settings.Worktime - (t2 - t1) + t3).ToString(hhmm);
                        var startOfDayFix = (settings.Worktime - (t4 - t3) - t2).ToString(hhmm);
                        return "  " + String.Join(", ", beats.Take(3)) + $", *{endOfDayFix}*"
                            + $"\n  *{startOfDayFix}*, " + String.Join(", ", beats.Skip(1));
                    }

                    return null;
            }

            return null;
        }

        public BeatInfo GetToday(Dictionary<string, BeatInfo> times)
        {
            return times[DateTime.Now.ToString("dd/MM/yyyy")];
        }

        public string ParseResult(Dictionary<string, BeatInfo> times)
        {
            TimeSpan t1, t2, t3, t4;

            var today = GetToday(times);

            t1 = GetTime(today.Beats.ElementAtOrDefault(0));
            t2 = GetTime(today.Beats.ElementAtOrDefault(1));
            t3 = GetTime(today.Beats.ElementAtOrDefault(2));
            t4 = GetTime(today.Beats.ElementAtOrDefault(3));

            switch (today.Beats.Length)
            {
                case 1:
                    return $"You can go to lunch at {Settings.LunchAt.ToString(hhmm)}";
                case 2:
                    var backFromLunchAt = t2 + TimeSpan.FromMinutes(Settings.LunchTime);
                    return $"You can come back from lunch at {backFromLunchAt.ToString(hhmm)} (±{Settings.Tolerance})";
                case 3:
                    var goHomeAt = Settings.Worktime - (t2 - t1) + t3;
                    return $"You can leave at {goHomeAt.ToString(hhmm)} (±{Settings.Tolerance})";
                case 4:
                    return "All done for today!";
                default:
                    return "No beats registered";
            }
        }
    }
}

/*
 
export async function parseResult(times) {
  const options = await settingsService.get();

  const today = getToday(times) || { beats: [] };
  const t1 = moment(today.beats[0], hourMinuteFormat);
  const t2 = moment(today.beats[1], hourMinuteFormat);
  const t3 = moment(today.beats[2], hourMinuteFormat);

  switch (today.beats.length) {
    case 1:
      return `You can go to lunch at ${options.lunchAt}`;

    case 2:
      const backFromLunchAt = moment(t2);
      backFromLunchAt.add(options.lunchTime, 'minutes');
      return `You can come back from lunch at ${moment(backFromLunchAt).format(hourMinuteFormat)} (±${options.tolerance})`;

    case 3:
      let section = moment(t2);
      section.add(-t1.hour(), 'hours');
      section.add(-t1.minute(), 'minutes');

      const d = moment(`${options.workHours}00`.slice(-6), 'HHmmss');
      d.add(-section.hour(), 'hours');
      d.add(-section.minute(), 'minutes');

      section = moment(t3);
      section.add(d.hour(), 'hours');
      section.add(d.minute(), 'minutes');

      return `You can leave at ${moment(section).format(hourMinuteFormat)} (±${options.tolerance})`;
    case 4:
      return 'All done for today!';
    default:
      return 'No beats registered today!';
  }
}

    */