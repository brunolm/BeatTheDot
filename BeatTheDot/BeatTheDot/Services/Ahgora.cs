using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;

// using HtmlAgilityPack;

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
            catch (Exception ex)
            {
            }

            return cookie;
        }

        public async Task<Dictionary<string, BeatInfo>> GetTimes()
        {
            var monthYear = DateTime.Now.ToString("MM-yyyy");
            var url = $"/externo/batidas/{monthYear}?nocache=true&breaker={this.rnd.Next()}";

            var page = await client.DownloadStringTaskAsync(url);
            return ParseHTML(page);
        }

        private Dictionary<string, BeatInfo> ParseHTML(string html)
        {
            return new Dictionary<string, BeatInfo>();
            //var doc = new HtmlDocument();
            //doc.LoadHtml(html);

            //var tables = doc.DocumentNode.SelectNodes("//table");
            //var tableTotalize = tables[0];
            //var timetable = tables[1];

            //var summary = Regex.Replace(tableTotalize.InnerText, @" {2,}", "");
            //summary = Regex.Replace(summary, @"(\s?\n){2,}", "\n").Trim();

            //var trs = timetable.SelectNodes(timetable.XPath + "/tbody/tr");

            //var times = new Dictionary<string, BeatInfo>();
            //foreach (var element in trs)
            //{
            //    if (element.SelectNodes(element.XPath + "//td").Count <= 3)
            //    {
            //        continue;
            //    }

            //    var col = element.SelectSingleNode(element.XPath + "/td[1]");
            //    var key = col.ChildNodes[0].InnerText.Trim();

            //    var beatsRaw = col.ChildNodes[3].InnerText.Trim();
            //    var beats = beatsRaw.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            //    var date = DateTime.ParseExact(key, "dd/MM/yyyy", CultureInfo.CurrentCulture);

            //    times[key] = new BeatInfo { Beats = beats, BeatsRaw = beatsRaw, Date = date };
            //}

            //return times;
        }
    }
}
