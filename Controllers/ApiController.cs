using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using JacRed.Engine.Parse;
using JacRed.Models.tParse;
using Newtonsoft.Json;
using JacRed.Engine.CORE;
using System.Text.RegularExpressions;
using JacRed.Engine;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;
using System;
using System.Web;
using MonoTorrent;

namespace JacRed.Controllers
{
    public class ApiController : BaseController
    {
        [Route("/")]
        public ActionResult Index()
        {
            return File(System.IO.File.OpenRead("wwwroot/index.html"), "text/html");
        }

        [Route("api/v1.0/conf")]
        public JsonResult JacRedConf(string apikey)
        {
            return Json(new
            {
                apikey = string.IsNullOrWhiteSpace(AppInit.conf.apikey) || apikey == AppInit.conf.apikey
            });
        }

        #region Jackett
        [Route("/api/v2.0/indexers/{status}/results")]
        public ActionResult Jackett(string apikey, string query, string title, string title_original, int year, int is_serial, Dictionary<string, string> category)
        {
            bool rqnum = false;
            var torrents = new List<TorrentDetails>();

            #region Запрос с NUM
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(title_original))
            {
                var mNum = Regex.Match(query ?? string.Empty, "^([^a-z-A-Z]+) ([^а-я-А-Я]+) ([0-9]{4})$");

                if (mNum.Success)
                {
                    if (Regex.IsMatch(mNum.Groups[2].Value, "[a-zA-Z]{4}"))
                    {
                        rqnum = true;
                        var g = mNum.Groups;

                        title = g[1].Value;
                        title_original = g[2].Value;
                        year = int.Parse(g[3].Value);
                    }
                }
                else
                {
                    mNum = Regex.Match(query ?? string.Empty, "^([^a-z-A-Z]+) ([0-9]{4})$");

                    if (mNum.Success)
                    {
                        if (Regex.IsMatch(mNum.Groups[1].Value, "[а-я-А-Я]{4}"))
                        {
                            rqnum = true;
                            var g = mNum.Groups;

                            title = g[1].Value;
                            year = int.Parse(g[2].Value);
                        }
                    }
                }
            }
            #endregion

            #region category
            if (is_serial == 0 && category != null)
            {
                string cat = category.FirstOrDefault().Value;
                if (cat != null)
                {
                    if (cat.Contains("5020") || cat.Contains("2010"))
                        is_serial = 3; // tvshow
                    else if (cat.Contains("5080"))
                        is_serial = 4; // док
                    else if (cat.Contains("5070"))
                        is_serial = 5; // аниме
                    else if (is_serial == 0)
                    {
                        if (cat.StartsWith("20"))
                            is_serial = 1; // фильм
                        else if (cat.StartsWith("50"))
                            is_serial = 2; // сериал
                    }
                }
            }
            #endregion

            if (!string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(title_original))
            {
                #region Точный поиск
                string _n = StringConvert.SearchName(title);
                string _o = StringConvert.SearchName(title_original);

                void torrentsSearch(bool exact)
                {
                    // Быстрая выборка по совпадению ключа в имени
                    foreach (var val in tParse.searchDb.Where(i => (_n != null && i.Key.Contains(_n)) || (_o != null && i.Key.Contains(_o))).Select(i => i.Value.Values))
                    {
                        foreach (var t in val)
                        {
                            if (t.types == null)
                                continue;

                            string name = StringConvert.SearchName(t.name);
                            string originalname = StringConvert.SearchName(t.originalname);

                            // Точная выборка по name или originalname
                            if (!exact || (_n != null && _n == name) || (_o != null && _o == originalname))
                            {
                                if (is_serial == 1)
                                {
                                    #region Фильм
                                    if (t.types.Contains("movie") || t.types.Contains("multfilm") || t.types.Contains("anime") || t.types.Contains("documovie"))
                                    {
                                        if (year > 0)
                                        {
                                            if (t.relased == year || t.relased == (year - 1) || t.relased == (year + 1))
                                                torrents.Add(t);
                                        }
                                        else
                                        {
                                            torrents.Add(t);
                                        }
                                    }
                                    #endregion
                                }
                                else if (is_serial == 2)
                                {
                                    #region Сериал
                                    if (t.types.Contains("serial") || t.types.Contains("multserial") || t.types.Contains("anime") || t.types.Contains("docuserial") || t.types.Contains("tvshow"))
                                    {
                                        if (year > 0)
                                        {
                                            if (t.relased >= (year - 1))
                                                torrents.Add(t);
                                        }
                                        else
                                        {
                                            torrents.Add(t);
                                        }
                                    }
                                    #endregion
                                }
                                else if (is_serial == 3)
                                {
                                    #region tvshow
                                    if (t.types.Contains("tvshow"))
                                    {
                                        if (year > 0)
                                        {
                                            if (t.relased >= (year - 1))
                                                torrents.Add(t);
                                        }
                                        else
                                        {
                                            torrents.Add(t);
                                        }
                                    }
                                    #endregion
                                }
                                else if (is_serial == 4)
                                {
                                    #region docuserial / documovie
                                    if (t.types.Contains("docuserial") || t.types.Contains("documovie"))
                                    {
                                        if (year > 0)
                                        {
                                            if (t.relased >= (year - 1))
                                                torrents.Add(t);
                                        }
                                        else
                                        {
                                            torrents.Add(t);
                                        }
                                    }
                                    #endregion
                                }
                                else if (is_serial == 5)
                                {
                                    #region anime
                                    if (t.types.Contains("anime"))
                                    {
                                        if (year > 0)
                                        {
                                            if (t.relased >= (year - 1))
                                                torrents.Add(t);
                                        }
                                        else
                                        {
                                            torrents.Add(t);
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    #region Неизвестно
                                    if (year > 0)
                                    {
                                        if (t.types.Contains("movie") || t.types.Contains("multfilm") || t.types.Contains("documovie"))
                                        {
                                            if (t.relased == year || t.relased == (year - 1) || t.relased == (year + 1))
                                                torrents.Add(t);
                                        }
                                        else
                                        {
                                            if (t.relased >= (year - 1))
                                                torrents.Add(t);
                                        }
                                    }
                                    else
                                    {
                                        torrents.Add(t);
                                    }
                                    #endregion
                                }
                            }
                        }
                    }
                }

                torrentsSearch(exact: true);
                if (torrents.Count == 0)
                    torrentsSearch(exact: false);
                #endregion
            }
            else if (!string.IsNullOrWhiteSpace(query))
            {
                #region Обычный поиск
                string _s = StringConvert.SearchName(query);

                #region torrentsSearch
                void torrentsSearch(bool exact)
                {
                    foreach (var val in tParse.searchDb.Where(i => i.Key.Contains(_s)).Select(i => i.Value.Values))
                    {
                        foreach (var t in val)
                        {
                            if (exact)
                            {
                                if (StringConvert.SearchName(t.name) != _s && StringConvert.SearchName(t.originalname) != _s)
                                    continue;
                            }

                            if (t.types == null)
                                continue;

                            if (is_serial == 1)
                            {
                                if (t.types.Contains("movie") || t.types.Contains("multfilm") || t.types.Contains("anime") || t.types.Contains("documovie"))
                                    torrents.Add(t);
                            }
                            else if (is_serial == 2)
                            {
                                if (t.types.Contains("serial") || t.types.Contains("multserial") || t.types.Contains("anime") || t.types.Contains("docuserial") || t.types.Contains("tvshow"))
                                    torrents.Add(t);
                            }
                            else if (is_serial == 3)
                            {
                                if (t.types.Contains("tvshow"))
                                    torrents.Add(t);
                            }
                            else if (is_serial == 4)
                            {
                                if (t.types.Contains("docuserial") || t.types.Contains("documovie"))
                                    torrents.Add(t);
                            }
                            else if (is_serial == 5)
                            {
                                if (t.types.Contains("anime"))
                                    torrents.Add(t);
                            }
                            else
                            {
                                torrents.Add(t);
                            }
                        }
                    }
                }
                #endregion

                torrentsSearch(exact: true);
                if (torrents.Count == 0)
                    torrentsSearch(exact: false);
                #endregion
            }

            #region getCategoryIds
            HashSet<int> getCategoryIds(TorrentDetails t, out string categoryDesc)
            {
                categoryDesc = null;
                HashSet<int> categoryIds = new HashSet<int>();

                foreach (string type in t.types)
                {
                    switch (type)
                    {
                        case "movie":
                            categoryDesc = "Movies";
                            categoryIds.Add(2000);
                            break;

                        case "serial":
                            categoryDesc = "TV";
                            categoryIds.Add(5000);
                            break;

                        case "documovie":
                        case "docuserial":
                            categoryDesc = "TV/Documentary";
                            categoryIds.Add(5080);
                            break;

                        case "tvshow":
                            categoryDesc = "TV/Foreign";
                            categoryIds.Add(5020);
                            categoryIds.Add(2010);
                            break;

                        case "anime":
                            categoryDesc = "TV/Anime";
                            categoryIds.Add(5070);
                            break;
                    }
                }

                return categoryIds;
            }
            #endregion

            #region Объединить дубликаты
            var tsort = new List<TorrentDetails>();

            if (!AppInit.conf.mergeduplicates || rqnum)
            {
                tsort = torrents;
            }
            else 
            {
                Dictionary<string, (TorrentDetails torrent, string title, string Name, List<string> AnnounceUrls)> temp = new Dictionary<string, (TorrentDetails, string, string, List<string>)>();

                foreach (var torrent in torrents)
                {
                    var magnetLink = MagnetLink.Parse(torrent.magnet);
                    string hex = magnetLink.InfoHash.ToHex();

                    if (!temp.TryGetValue(hex, out _))
                    {
                        temp.TryAdd(hex, ((TorrentDetails)torrent.Clone(), torrent.trackerName == "kinozal" ? torrent.title : null, magnetLink.Name, magnetLink.AnnounceUrls?.ToList() ?? new List<string>()));
                    }
                    else
                    {
                        var t = temp[hex];
                        t.torrent.trackerName += $", {torrent.trackerName}";

                        #region UpdateMagnet
                        void UpdateMagnet()
                        {
                            string magnet = $"magnet:?xt=urn:btih:{hex.ToLower()}";

                            if (!string.IsNullOrWhiteSpace(t.Name))
                                magnet += $"&dn={HttpUtility.UrlEncode(t.Name)}";

                            if (t.AnnounceUrls.Count > 0)
                                magnet += $"&tr={string.Join("&tr=", t.AnnounceUrls)}";

                            t.torrent.magnet= magnet ;
                        }
                        #endregion

                        if (string.IsNullOrWhiteSpace(t.Name) && !string.IsNullOrWhiteSpace(magnetLink.Name))
                        {
                            t.Name = magnetLink.Name;
                            temp[hex] = t;
                            UpdateMagnet();
                        }

                        if (magnetLink.AnnounceUrls != null && magnetLink.AnnounceUrls.Count > 0)
                        {
                            t.AnnounceUrls.AddRange(magnetLink.AnnounceUrls);
                            UpdateMagnet();
                        }

                        #region UpdateTitle
                        void UpdateTitle()
                        {
                            if (string.IsNullOrWhiteSpace(t.title))
                                return;

                            string title = t.title;

                            if (t.torrent.voices != null && t.torrent.voices.Count > 0)
                                title += $" | {string.Join(" | ", t.torrent.voices)}";

                            t.torrent.title = title;
                        }

                        if (torrent.trackerName == "kinozal")
                        {
                            t.title = torrent.title;
                            temp[hex] = t;
                            UpdateTitle();
                        }

                        if (torrent.voices != null && torrent.voices.Count > 0)
                        {
                            if (t.torrent.voices == null)
                            {
                                t.torrent.voices = torrent.voices;
                            }
                            else
                            {
                                foreach (var v in torrent.voices)
                                    t.torrent.voices.Add(v);
                            }

                            UpdateTitle();
                        }
                        #endregion

                        if (torrent.sid > t.torrent.sid)
                            t.torrent.sid = torrent.sid;

                        if (torrent.pir > t.torrent.pir)
                            t.torrent.pir = torrent.pir;

                        if (torrent.createTime > t.torrent.createTime)
                            t.torrent.createTime = torrent.createTime;

                        if (torrent.voices != null && torrent.voices.Count > 0)
                        {
                            if (t.torrent.voices == null)
                                t.torrent.voices = new HashSet<string>();

                            foreach (var v in torrent.voices)
                                t.torrent.voices.Add(v);
                        }

                        if (torrent.languages != null && torrent.languages.Count > 0)
                        {
                            if (t.torrent.languages == null)
                                t.torrent.languages = new HashSet<string>();

                            foreach (var v in torrent.languages)
                                t.torrent.languages.Add(v);
                        }

                        if (t.torrent.ffprobe == null && torrent.ffprobe != null)
                            t.torrent.ffprobe = torrent.ffprobe;
                    }
                }

                tsort = temp.Select(i => i.Value.torrent).ToList();
            }
            #endregion

            var result = tsort.OrderByDescending(i => i.createTime).Take(2_000);
            if (apikey == "rus")
                result = result.Where(i => i.languages != null && i.languages.Contains("rus"));

            return Content(JsonConvert.SerializeObject(new
            {
                Results = result.Select(i => new
                {
                    Tracker = i.trackerName,
                    Details = i.url != null && i.url.StartsWith("http") ? i.url : null,
                    Title = i.title,
                    Size = i.size,
                    PublishDate = i.createTime,
                    Category = getCategoryIds(i, out string categoryDesc),
                    CategoryDesc = categoryDesc,
                    Seeders = i.sid,
                    Peers = i.pir,
                    MagnetUri = i.magnet,
                    i.ffprobe,
                    i.languages,
                    info = new
                    {
                        i.name,
                        i.originalname,
                        i.sizeName,
                        i.relased,
                        i.videotype,
                        i.quality,
                        i.voices,
                        seasons = i.seasons != null && i.seasons.Count > 0 ? i.seasons : null,
                        i.types
                    }
                }),
                jacred = true

            }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }), contentType: "application/json; charset=utf-8");
        }
        #endregion

        #region Torrents
        [Route("/api/v1.0/torrents")]
        async public Task<JsonResult> Torrents(string search, string altname, bool exact, string type, string sort, string tracker, string voice, string videotype, long relased, long quality, long season)
        {
            #region search kp/imdb
            if (!string.IsNullOrWhiteSpace(search) && Regex.IsMatch(search.Trim(), "^(tt|kp)[0-9]+$"))
            {
                string memkey = $"api/v1.0/torrents:{search}";
                if (!memoryCache.TryGetValue(memkey, out (string original_name, string name) cache))
                {
                    search = search.Trim();
                    string uri = $"&imdb={search}";
                    if (search.StartsWith("kp"))
                        uri = $"&kp={search.Remove(0, 2)}";

                    var root = await HttpClient.Get<JObject>("https://api.alloha.tv/?token=04941a9a3ca3ac16e2b4327347bbc1" + uri, timeoutSeconds: 8);
                    cache.original_name = root?.Value<JObject>("data")?.Value<string>("original_name");
                    cache.name = root?.Value<JObject>("data")?.Value<string>("name");

                    memoryCache.Set(memkey, cache, DateTime.Now.AddDays(1));
                }

                if (!string.IsNullOrWhiteSpace(cache.name) && !string.IsNullOrWhiteSpace(cache.original_name))
                {
                    search = cache.original_name;
                    altname = cache.name;
                }
                else
                {
                    search = cache.original_name ?? cache.name;
                }
            }
            #endregion

            #region Выборка 
            IEnumerable<TorrentDetails> query = null;
            var torrents = new List<TorrentDetails>();

            if (string.IsNullOrWhiteSpace(search))
                return Json(torrents);

            string _s = StringConvert.SearchName(search);
            string _altsearch = StringConvert.SearchName(altname);

            if (exact)
            {
                #region Точный поиск
                foreach (var val in tParse.searchDb.Where(i => i.Key.Contains(_s) || (_altsearch != null && i.Key.Contains(_altsearch))).Select(i => i.Value.Values))
                {
                    foreach (var t in val)
                    {
                        if (t.types == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(type) || t.types.Contains(type))
                        {
                            string _n = StringConvert.SearchName(t.name);
                            string _o = StringConvert.SearchName(t.originalname);

                            if (_n == _s || _o == _s || (_altsearch != null && (_n == _altsearch || _o == _altsearch)))
                                torrents.Add(t);
                        }
                    }
                }
                #endregion
            }
            else
            {
                #region Поиск по совпадению ключа в имени
                foreach (var val in tParse.searchDb.Where(i => i.Key.Contains(_s) || (_altsearch != null && i.Key.Contains(_altsearch))).Select(i => i.Value.Values))
                {
                    foreach (var t in val)
                    {
                        if (t.types == null)
                            continue;

                        if (string.IsNullOrWhiteSpace(type) || t.types.Contains(type))
                            torrents.Add(t);
                    }
                }
                #endregion
            }

            if (torrents.Count == 0)
                return Json(torrents);

            #region sort
            switch (sort ?? string.Empty)
            {
                case "sid":
                    query = torrents.OrderByDescending(i => i.sid);
                    break;
                case "pir":
                    query = torrents.OrderByDescending(i => i.pir);
                    break;
                case "size":
                    query = torrents.OrderByDescending(i => i.size);
                    break;
                default:
                    query = torrents.OrderByDescending(i => i.createTime);
                    break;
            }
            #endregion

            if (!string.IsNullOrWhiteSpace(tracker))
                query = query.Where(i => i.trackerName == tracker);

            if (relased > 0)
                query = query.Where(i => i.relased == relased);

            if (quality > 0)
                query = query.Where(i => i.quality == quality);

            if (!string.IsNullOrWhiteSpace(videotype))
                query = query.Where(i => i.videotype == videotype);

            if (!string.IsNullOrWhiteSpace(voice))
                query = query.Where(i => i.voices.Contains(voice));

            if (season > 0)
                query = query.Where(i => i.seasons.Contains((int)season));
            #endregion

            return Json(query.Take(5_000).Select(i => new
            {
                tracker = i.trackerName,
                url = i.url != null && i.url.StartsWith("http") ? i.url : null,
                i.title,
                i.size,
                i.sizeName,
                i.createTime,
                i.sid,
                i.pir,
                i.magnet,
                i.name,
                i.originalname,
                i.relased,
                i.videotype,
                i.quality,
                i.voices,
                i.seasons,
                i.types
            }));
        }
        #endregion
    }
}
