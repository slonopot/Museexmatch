﻿using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Topten.JsonKit;

namespace Museexmatch
{
    internal class Util
    {
        private static Logger Logger = LogManager.GetCurrentClassLogger();

        public static string ToQueryString(NameValueCollection nvc)
        {
            var array = (
                from key in nvc.AllKeys
                from value in nvc.GetValues(key)
                select string.Format(
                "{0}={1}",
                //HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value))
                Uri.EscapeDataString(key), Uri.EscapeDataString(value))
                ).ToArray();
            return string.Join("&", array);
        }

        public static bool PropertyExists(dynamic obj, string name)
        {
            if (obj == null) return false;
            if (obj is IDictionary<string, object> dict)
            {
                return dict.ContainsKey(name);
            }
            return obj.GetType().GetProperty(name) != null;
        }


        public static int ComputeDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            // Step 1
            if (n == 0)
            {
                return m;
            }

            if (m == 0)
            {
                return n;
            }

            // Step 2
            for (int i = 0; i <= n; d[i, 0] = i++)
            {
            }

            for (int j = 0; j <= m; d[0, j] = j++)
            {
            }

            // Step 3
            for (int i = 1; i <= n; i++)
            {
                //Step 4
                for (int j = 1; j <= m; j++)
                {
                    // Step 5
                    int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                    // Step 6
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            // Step 7
            return d[n, m];
        }

        public static bool ValidateResult(string originalArtist, string originalTitle, string foundArtist, string foundTitle, int allowedDistance)
        {
            var originalEntry = $"{originalArtist} {originalTitle}".ToLower();
            var foundEntry = $"{foundArtist} {foundTitle}".ToLower();

            Logger.Info("Comparing {originalEntry} with {foundEntry}", originalEntry, foundEntry);

            if (Util.ComputeDistance(originalEntry, foundEntry) <= allowedDistance)
            {
                Logger.Info("It's good");
                return true;
            }
            Logger.Info("It's not good");
            return false;
        }

        public static string GenerateHex(int length)
        {
            Random random = new Random();
            byte[] buffer = new byte[length / 2];
            random.NextBytes(buffer);
            string result = string.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (length % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        public static string MusixmatchSignature(string URL, NameValueCollection parameters, string key)
        {
            string data = URL + "?" + ToQueryString(parameters) + DateTime.Now.ToString("yyyyMMdd");
            HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
            byte[] result = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(result);
        }

        public static string Trim(string title)
        {
            title = Regex.Replace(title, @"\[.*\]", "");
            title = Regex.Replace(title, @"\(.*\)", "");
            title = Regex.Replace(title, @"\<.*\>", "");
            title = Regex.Replace(title, @"\{.*\}", "");
            return title.Trim();
        }

        public static string ConvertRichsyncToLRC(string richsync)
        {
            dynamic content = Json.Parse<object>(richsync);
            var result = string.Empty;

            foreach (dynamic segment in content)
            {
                var startTS = TimeSpan.FromSeconds(segment.ts);
                var start = startTS.ToString(@"mm\:ss") + "." + startTS.ToString("fffffff").Substring(0, 2);
                result += $"[{start}]{segment.x}\n";
            }

            return result;
        }
        public static string ConvertRichsyncToAdvancedLRC(string richsync)
        {
            dynamic content = Json.Parse<object>(richsync);
            var result = string.Empty;

            foreach (dynamic segment in content)
            {
                var startTS = TimeSpan.FromSeconds(segment.ts);
                var start = startTS.ToString(@"mm\:ss") + "." + startTS.ToString("fffffff").Substring(0, 2);
                var text = string.Empty;
                try
                {
                    var _text = string.Empty;
                    foreach (dynamic subsegment in segment.l)
                    {
                        var subStartTS = TimeSpan.FromSeconds(segment.ts + subsegment.o);
                        var subStart = subStartTS.ToString(@"mm\:ss") + "." + startTS.ToString("fffffff").Substring(0, 2);
                        _text += $"<{subStart}>{subsegment.c}";
                    }
                    text = _text;
                }
                catch (Exception ex)
                {
                    text = segment.x;
                }
               result += $"[{start}]{text}\n";
            }

            return result;
        }


    }
}
