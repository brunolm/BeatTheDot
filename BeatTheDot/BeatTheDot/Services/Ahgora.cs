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
            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return new Dictionary<string, BeatInfo>();
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
                sb.AppendLine($"{item.Key} - {item.Value.BeatsRaw} (##)");

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

        public string Calc(string[] beats)
        {
            TimeSpan t1, t2, t3, t4;

            t1 = GetTime(beats.ElementAtOrDefault(0));
            t2 = GetTime(beats.ElementAtOrDefault(1));
            t3 = GetTime(beats.ElementAtOrDefault(2));
            t4 = GetTime(beats.ElementAtOrDefault(3));

            var settings = Settings;

            var hhmm = "hh':'mm";
            switch (beats.Length)
            {
                case 0:
                    return null;
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
    }
}

/*

async function calc([time1, time2, time3, time4]) {
  const options = await settingsService.get();

  const [t1, t2, t3, t4] = [
    moment(time1, hourMinuteFormat),
    moment(time2, hourMinuteFormat),
    moment(time3, hourMinuteFormat),
    moment(time4, hourMinuteFormat),
  ];

  const scenarios = [];

  const day = moment(options.workHours, hourMinuteFormat);
  const morning = subTime(t2, t1);
  const afternoon = subTime(t4, t3);

  const workedHours = t4.isValid()
    ? moment(morning).add(afternoon.hour() as any, 'hours').add(afternoon.minute() as any, 'minutes')
    : moment(morning);

  if (t4.isValid()) {
    if (workedHours.isAfter(moment(day).add(options.tolerance as any, 'minutes'))) {
      const timeToRemove = subTime(workedHours, day);
      {
        // Scenario 1 - Remove from end of the day
        const newEndTime = subTime(t4, timeToRemove);
        scenarios.push(`${time1} ${time2} ${time3} *${newEndTime.format(hourMinuteFormat)}*`);
      }
    }
    else if (workedHours.isBefore(moment(day).add(-options.tolerance as any, 'minutes'))) {
      const timeToAdd = subTime(day, workedHours);
      {
        // Scenario 1 - Add to the end of the day
        const newEndTime = addTime(t4, timeToAdd);
        scenarios.push(`${time1} ${time2} ${time3} *${newEndTime.format(hourMinuteFormat)}*`);
      }
    }
  }
  else if (t3.isValid()) {
    {
      // Scenario 1 - Predict end of the day
      const newEndTime = addTime(t3, subTime(day, morning));
      scenarios.push(`${time1} ${time2} ${time3} *${newEndTime.format(hourMinuteFormat)}*`);
    }
    {
      // Scenario 2 - Predict beginning of the day
      const section = subTime(t1, subTime(day, subTime(t3, t2)));
      scenarios.push(`*${section.format(hourMinuteFormat)}* ${time1} ${time2} ${time3}`);
    }
  }

  return scenarios;
}


export async function parseGrid(times) {
  const keys = Object.keys(times);

  let grid = '';
  for (const key of keys) {
    const time = times[key];

    if (time.beatsRaw) {
      grid += `${key} - ${time.beatsRaw} (${time.total.match(/\d{2}:\d{2}/)})`;
      if (time.patch.wrong.time) {
        grid += ` (${time.patch.wrong.time} -> ${time.patch.correct.time})`;
      }
      grid += '\n';

      const scenarios = await calc(time.beatsRaw.match(/\d{2}:\d{2}/g)) as string[];
      if (scenarios.length) {
        grid += '  ' + scenarios.join('\n  ');
        grid += '\n';
      }
    }
  }

  return grid;
}
*/
