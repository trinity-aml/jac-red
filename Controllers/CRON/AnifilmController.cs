using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using JacRed.Engine;
using JacRed.Engine.CORE;
using JacRed.Engine.Parse;
using JacRed.Models.tParse;
using Microsoft.AspNetCore.Mvc;

namespace JacRed.Controllers.CRON
{
    [Route("/cron/anifilm/[action]")]
    public class AnifilmController : BaseController
    {
        #region Parse
        static bool workParse = false;

        async public Task<string> Parse(bool fullparse)
        {
            if (workParse)
                return "work";

            workParse = true;

            string log = "";

            try
            {
                if (fullparse)
                {
                    for (int page = 1; page <= 70; page++)
                    {
                        await Task.Delay(AppInit.conf.Anifilm.parseDelay);
                        await parsePage("serials", page, DateTime.Today.AddDays(-(2 * page)));
                    }

                    for (int page = 1; page <= 32; page++)
                    {
                        await Task.Delay(AppInit.conf.Anifilm.parseDelay);
                        await parsePage("ova", page, DateTime.Today.AddDays(-(2 * page)));
                    }

                    for (int page = 1; page <= 2; page++)
                    {
                        await Task.Delay(AppInit.conf.Anifilm.parseDelay);
                        await parsePage("ona", page, DateTime.Today.AddDays(-(2 * page)));
                    }

                    for (int page = 1; page <= 17; page++)
                    {
                        await Task.Delay(AppInit.conf.Anifilm.parseDelay);
                        await parsePage("movies", page, DateTime.Today.AddDays(-(2 * page)));
                    }
                }
                else
                {
                    foreach (string cat in new List<string>() { "serials", "ova", "ona", "movies" })
                    {
                        await parsePage(cat, 1, DateTime.Now);
                        log += $"{cat} - 1\n";
                    }
                }
            }
            catch { }

            workParse = false;
            return string.IsNullOrWhiteSpace(log) ? "ok" : log;
        }
        #endregion


        #region parsePage
        async Task<bool> parsePage(string cat, int page, DateTime createTime)
        {
            string html = await HttpClient.Get($"{AppInit.conf.Anifilm.host}/releases/page/{page}?category={cat}", useproxy: AppInit.conf.Anifilm.useproxy);
            if (html == null || !html.Contains("id=\"ui-components\""))
                return false;

            foreach (string row in tParse.ReplaceBadNames(html).Split("class=\"releases__item\"").Skip(1))
            {
                #region Локальный метод - Match
                string Match(string pattern, int index = 1)
                {
                    string res = HttpUtility.HtmlDecode(new Regex(pattern, RegexOptions.IgnoreCase).Match(row).Groups[index].Value.Trim());
                    res = Regex.Replace(res, "[\n\r\t ]+", " ");
                    return res.Trim();
                }
                #endregion

                if (string.IsNullOrWhiteSpace(row))
                    continue;

                #region Данные раздачи
                string url = Match("<a href=\"/(releases/[^\"]+)\"");
                string name = Match("<a class=\"releases__title-russian\" [^>]+>([^<]+)</a>");
                string originalname = Match("<span class=\"releases__title-original\">([^<]+)</span>");
                string episodes = Match("([0-9]+(-[0-9]+)?) из [0-9]+ эп.,");

                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(originalname))
                    continue;

                if (cat != "movies" && string.IsNullOrWhiteSpace(episodes))
                    continue;

                url = $"{AppInit.conf.Anifilm.host}/{url}";
                string title = $"{name} / {originalname}";

                if (!string.IsNullOrWhiteSpace(episodes))
                    title += $" ({episodes})";
                #endregion

                // Год выхода
                if (!int.TryParse(Match("<a href=\"/releases/releases/[^\"]+\">([0-9]{4})</a> г\\."), out int relased) || relased == 0)
                    continue;

                // Удаляем торрент где титл не совпадает (возможно обновлен, добавлена серия и т.д)
                if (tParse.TryGetValue(url, out TorrentDetails _tcache) && _tcache.title.Replace(" [1080p]", "") != title)
                    tParse.db.TryRemove(url, out _);

                tParse.AddOrUpdate(new TorrentDetails()
                {
                    trackerName = "anifilm",
                    types = new string[] { "anime" },
                    url = url,
                    title = title,
                    sid = 1,
                    createTime = createTime,
                    name = name.Split("(")[0].Trim(),
                    originalname = originalname,
                    relased = relased
                });
            }

            return true;
        }
        #endregion

        #region parseMagnet
        static bool _parseMagnetWork = false;

        async public Task<string> parseMagnet()
        {
            if (_parseMagnetWork)
                return "work";

            _parseMagnetWork = true;

            try
            {
                foreach (var torrent in tParse.db.Where(i => i.Value.trackerName == "anifilm" && string.IsNullOrWhiteSpace(i.Value.magnet)))
                {
                    var fullNews = await HttpClient.Get(torrent.Key, useproxy: AppInit.conf.Anifilm.useproxy);
                    if (fullNews != null)
                    {
                        string title = torrent.Value.title;

                        string tid = null;
                        string[] releasetorrents = fullNews.Split("<li class=\"release__torrents-item\">");

                        string _rnews = releasetorrents.FirstOrDefault(i => i.Contains("href=\"/releases/download-torrent/") && i.Contains(" 1080p "));
                        if (!string.IsNullOrWhiteSpace(_rnews))
                        {
                            tid = Regex.Match(_rnews, "href=\"/(releases/download-torrent/[0-9]+)\">скачать</a>").Groups[1].Value;
                            if (!string.IsNullOrWhiteSpace(tid) && !title.Contains(" [1080p]"))
                                title += " [1080p]";
                        }

                        if (string.IsNullOrWhiteSpace(tid))
                            tid = Regex.Match(fullNews, "href=\"/(releases/download-torrent/[0-9]+)\">скачать</a>").Groups[1].Value;

                        byte[] t = await HttpClient.Download($"{AppInit.conf.Anifilm.host}/{tid}", referer: torrent.Key, useproxy: AppInit.conf.Anifilm.useproxy);
                        string magnet = BencodeTo.Magnet(t);

                        if (!string.IsNullOrWhiteSpace(magnet))
                        {
                            torrent.Value.magnet = magnet;
                            torrent.Value.title = title;
                            torrent.Value.updateTime = DateTime.UtcNow;
                            torrent.Value.sizeName = BencodeTo.SizeName(t);
                        }
                    }
                }
            }
            catch { }

            _parseMagnetWork = false;
            return "ok";
        }
        #endregion
    }
}
