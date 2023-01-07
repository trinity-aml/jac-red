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
using Newtonsoft.Json;

namespace JacRed.Controllers.CRON
{
    //[Route("/cron/anidub/[action]")]
    public class AnidubController : BaseController
    {
        #region Parse
        static bool workParse = false;

        async public Task<string> Parse(bool fullparse)
        {
            if (workParse)
                return "work";

            workParse = true;

            try
            {
                if (fullparse)
                {
                    for (int page = 1; page <= 4; page++)
                        await parsePage("anime_tv/anime_ongoing", page);

                    for (int page = 1; page <= 41; page++)
                        await parsePage("anime_ova", page);

                    for (int page = 1; page <= 22; page++)
                        await parsePage("anime_movie", page);

                    for (int page = 1; page <= 124; page++)
                        await parsePage("anime_tv/full", page);
                }
                else
                {
                    foreach (string cat in new List<string>() { "anime_tv/anime_ongoing", "anime_tv/shonen", "anime_ova", "anime_movie" })
                        await parsePage(cat, 1);
                }
            }
            catch { }

            workParse = false;
            return "ok";
        }
        #endregion


        #region parsePage
        async Task<bool> parsePage(string cat, int page)
        {
            Console.WriteLine($"\n\n{AppInit.conf.Anidub.host}/{cat}/" + (page > 1 ? $"page/{page}/" : ""));
            string html = await HttpClient.Get($"{AppInit.conf.Anidub.host}/{cat}/" + (page > 1 ? $"page/{page}/" : ""), useproxy: AppInit.conf.Anidub.useproxy);
            if (html == null || !html.Contains("id=\"header_h\""))
                return false;

            Console.WriteLine("1");

            foreach (string row in tParse.ReplaceBadNames(html).Split("<article class=\"story\"").Skip(1))
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

                Console.WriteLine("2");

                #region Дата создания
                DateTime createTime = default;

                if (row.Contains("<b>Дата:</b> Сегодня"))
                {
                    createTime = DateTime.Today;
                }
                else if (row.Contains("<b>Дата:</b> Вчера"))
                {
                    createTime = DateTime.Today.AddDays(-1);
                }
                else
                {
                    createTime = tParse.ParseCreateTime(Match("b>Дата:</b> ([0-9-]+),").Replace("-", "."), "dd.MM.yyyy");
                }

                if (createTime == default)
                    continue;
                #endregion

                Console.WriteLine("3");

                #region Данные раздачи
                string url = Match("<h2><a href=\"(https?://[^/]+)?/([^\":]+)\"", 2);
                string title = Match(">([^<]+)</a></h2>");

                if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(title))
                    continue;

                url = $"{AppInit.conf.Anidub.host}/{url}";
                #endregion

                Console.WriteLine("4");

                #region name / originalname
                string name = null, originalname = null;

                // Диназенон / SSSS.Dynazenon [07 из 12]
                var g = Regex.Match(title, "^([^/\\[]+) / ([^/\\[]+) \\[").Groups;
                if (!string.IsNullOrWhiteSpace(g[1].Value) && !string.IsNullOrWhiteSpace(g[2].Value))
                {
                    name = g[1].Value;
                    originalname = g[2].Value;
                }
                #endregion

                // Год выхода
                if (!int.TryParse(Match("<b>Год: </b><span><a href=\"[^\"]+\">([0-9]{4})</a>"), out int relased) || relased == 0)
                    continue;

                Console.WriteLine("5");

                if (!string.IsNullOrWhiteSpace(name))
                {
                    if (!tParse.TryGetValue(url, out TorrentDetails _tcache) || _tcache.title != title)
                    {
                        Console.WriteLine("6");

                        await Task.Delay(AppInit.conf.Anidub.parseDelay);

                        #region Обновляем/Получаем Magnet
                        string magnet = null;
                        string sizeName = null;

                        string fulnews = await HttpClient.Get(url, useproxy: AppInit.conf.Anidub.useproxy);
                        if (fulnews == null)
                            continue;

                        Console.WriteLine("7");

                        string tid = Regex.Match(fulnews, "<div class=\"torrent_h\">[\n\r\t ]+<a href=\"/(engine/download.php\\?id=[0-9]+)\"").Groups[1].Value;

                        byte[] torrent = await HttpClient.Download($"{AppInit.conf.Anidub.host}/{tid}", referer: url, useproxy: AppInit.conf.Anidub.useproxy);
                        magnet = BencodeTo.Magnet(torrent);
                        sizeName = BencodeTo.SizeName(torrent);

                        if (string.IsNullOrWhiteSpace(magnet))
                            continue;
                        #endregion

                        Console.WriteLine(JsonConvert.SerializeObject(new TorrentDetails()
                        {
                            trackerName = "anidub",
                            types = new string[] { "anime" },
                            url = url,
                            title = title,
                            sid = 1,
                            sizeName = sizeName,
                            createTime = createTime,
                            magnet = magnet,
                            name = name,
                            originalname = originalname,
                            relased = relased
                        }, Formatting.Indented));

                        tParse.AddOrUpdate(new TorrentDetails()
                        {
                            trackerName = "anidub",
                            types = new string[] { "anime" },
                            url = url,
                            title = title,
                            sid = 1,
                            sizeName = sizeName,
                            createTime = createTime,
                            magnet = magnet,
                            name = name,
                            originalname = originalname,
                            relased = relased
                        });
                    }
                }
            }

            return true;
        }
        #endregion
    }
}
