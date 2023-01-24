using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using JacRed.Engine.CORE;
using JacRed.Models.tParse;

namespace JacRed.Engine.Parse
{
    public static class tParse
    {
        #region tParse
        public static ConcurrentDictionary<string, TorrentDetails> db = new ConcurrentDictionary<string, TorrentDetails>();

        public static ConcurrentDictionary<string, ConcurrentDictionary<string, TorrentDetails>> searchDb = new ConcurrentDictionary<string, ConcurrentDictionary<string, TorrentDetails>>();

        static tParse()
        {
            db = JsonStream.Read<ConcurrentDictionary<string, TorrentDetails>>("Data/torrents.json");

            foreach (var item in db)
                AddOrUpdateSearchDb(item.Value);
        }
        #endregion


        #region ReplaceBadNames
        public static string ReplaceBadNames(string html)
        {
            return html.Replace("Ванда/Вижн ", "ВандаВижн ").Replace("Ё", "Е").Replace("ё", "е").Replace("щ", "ш");
        }
        #endregion

        #region ParseCreateTime
        public static DateTime ParseCreateTime(string line, string format)
        {
            line = Regex.Replace(line, " янв\\.? ", ".01.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " февр?\\.? ", ".02.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " март?\\.? ", ".03.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " апр\\.? ", ".04.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " май\\.? ", ".05.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " июнь?\\.? ", ".06.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " июль?\\.? ", ".07.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " авг\\.? ", ".08.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " сент?\\.? ", ".09.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " окт\\.? ", ".10.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " нояб?\\.? ", ".11.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " дек\\.? ", ".12.", RegexOptions.IgnoreCase);

            line = Regex.Replace(line, " январ(ь|я)?\\.? ", ".01.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " феврал(ь|я)?\\.? ", ".02.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " марта?\\.? ", ".03.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " апрел(ь|я)?\\.? ", ".04.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " май?я?\\.? ", ".05.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " июн(ь|я)?\\.? ", ".06.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " июл(ь|я)?\\.? ", ".07.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " августа?\\.? ", ".08.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " сентябр(ь|я)?\\.? ", ".09.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " октябр(ь|я)?\\.? ", ".10.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " ноябр(ь|я)?\\.? ", ".11.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " декабр(ь|я)?\\.? ", ".12.", RegexOptions.IgnoreCase);

            line = Regex.Replace(line, " Jan ", ".01.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Feb ", ".02.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Mar ", ".03.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Apr ", ".04.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " May ", ".05.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Jun ", ".06.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Jul ", ".07.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Aug ", ".08.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Sep ", ".09.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Oct ", ".10.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Nov ", ".11.", RegexOptions.IgnoreCase);
            line = Regex.Replace(line, " Dec ", ".12.", RegexOptions.IgnoreCase);

            if (Regex.IsMatch(line, "^[0-9]\\."))
                line = $"0{line}";

            DateTime.TryParseExact(line.ToLower(), format, new CultureInfo("ru-RU"), DateTimeStyles.None, out DateTime createTime);
            return createTime;
        }
        #endregion


        #region AddOrUpdateSearchDb
        public static void AddOrUpdateSearchDb(TorrentDetails torrent)
        {
            if ((!string.IsNullOrWhiteSpace(torrent.name) || !string.IsNullOrWhiteSpace(torrent.originalname)) && !string.IsNullOrWhiteSpace(torrent.magnet))
            {
                string search_name = StringConvert.SearchName(torrent.name);
                string search_originalname = StringConvert.SearchName(torrent.originalname);

                string key = $"{search_name}:{search_originalname}";
                if (!searchDb.ContainsKey(key))
                    searchDb.TryAdd(key, new ConcurrentDictionary<string, TorrentDetails>());

                var tdb = searchDb[key];
                tdb.AddOrUpdate(torrent.url, torrent, (k,v) => torrent);
            }
        }
        #endregion


        #region TryGetValue
        public static bool TryGetValue(string url, out TorrentDetails torrent)
        {
            return db.TryGetValue(url, out torrent);
        }
        #endregion

        #region AddOrUpdate
        public static void AddOrUpdate(TorrentDetails torrent)
        {
            if (db.TryGetValue(torrent.url, out TorrentDetails t))
            {
                void upt() { t.updateTime = DateTime.UtcNow; }

                #region types
                if (torrent.types != null)
                {
                    if (t.types == null || t.types.Length != torrent.types.Length)
                        upt();

                    foreach (string type in torrent.types)
                    {
                        if (!t.types.Contains(type))
                            upt();
                    }

                    t.types = torrent.types;
                }
                #endregion

                if (torrent.trackerName != t.trackerName)
                {
                    t.trackerName = torrent.trackerName;
                    upt();
                }

                if (torrent.title != t.title)
                {
                    t.title = torrent.title;
                    upt();
                }

                if (!string.IsNullOrWhiteSpace(torrent.magnet) && torrent.magnet != t.magnet)
                {
                    t.magnet = torrent.magnet;
                    upt();
                }

                if (torrent.sid != t.sid)
                {
                    t.sid = torrent.sid;
                    upt();
                }

                if (torrent.pir != t.pir)
                {
                    t.pir = torrent.pir;
                    upt();
                }

                if (!string.IsNullOrWhiteSpace(torrent.sizeName) && torrent.sizeName != t.sizeName)
                {
                    t.sizeName = torrent.sizeName;
                    upt();
                }

                if (!string.IsNullOrWhiteSpace(torrent.name) && torrent.name != t.name)
                {
                    t.name = torrent.name;
                    upt();
                }

                if (!string.IsNullOrWhiteSpace(torrent.originalname) && torrent.originalname != t.originalname)
                {
                    t.originalname = torrent.originalname;
                    upt();
                }

                if (torrent.relased > 0 && torrent.relased != t.relased)
                {
                    t.relased = torrent.relased;
                    upt();
                }

                AddOrUpdateSearchDb(t);
            }
            else
            {
                db.TryAdd(torrent.url, torrent);
                AddOrUpdateSearchDb(torrent);
            }
        }
        #endregion


        #region SaveAndUpdateDB
        public static Task SaveAndUpdateDB()
        {
            try
            {
                return Task.Run(() => 
                {
                    JsonStream.Write("Data/torrents.json", db);

                    if (!File.Exists($"Data/torrents_{DateTime.Today:dd-MM-yyyy}.json.gz"))
                        File.Copy("Data/torrents.json.gz", $"Data/torrents_{DateTime.Today:dd-MM-yyyy}.json.gz");

                    if (File.Exists($"Data/torrents_{DateTime.Today.AddDays(-2):dd-MM-yyyy}.json.gz"))
                        File.Delete($"Data/torrents_{DateTime.Today.AddDays(-2):dd-MM-yyyy}.json.gz");
                });
            }
            catch { return Task.CompletedTask; }
        }
        #endregion
    }
}
