using JacRed.Models.AppConf;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace JacRed
{
    public class AppInit
    {
        #region СНЕСТИ
        public static string lostfilmCookie;

        public static (string u, string p) baibakoLogin;

        public static (string u, string p) hamsterLogin;

        public static (string u, string p) animelayerLogin;
        #endregion





        #region AppInit
        static (AppInit, DateTime) cacheconf = default;

        public static AppInit conf
        {
            get
            {
                if (cacheconf.Item1 == null)
                {
                    if (!File.Exists("init.conf"))
                        return new AppInit();
                }

                var lastWriteTime = File.GetLastWriteTime("init.conf");

                if (cacheconf.Item2 != lastWriteTime)
                {
                    cacheconf.Item1 = JsonConvert.DeserializeObject<AppInit>(File.ReadAllText("init.conf"));
                    cacheconf.Item2 = lastWriteTime;
                }

                return cacheconf.Item1;
            }
        }

        public static string Host(HttpContext httpContext) => $"http://{httpContext.Request.Host.Value}";
        #endregion


        public int listenport = 9117;

        public string apikey = null;


        public TrackerSettings Rutor = new TrackerSettings("http://rutor.info", priority: "torrent");

        public TrackerSettings Megapeer = new TrackerSettings("http://megapeer.vip");

        public TrackerSettings TorrentBy = new TrackerSettings("https://torrent.by", priority: "torrent");

        public TrackerSettings Kinozal = new TrackerSettings("https://kinozal.tv");

        public TrackerSettings NNMClub = new TrackerSettings("https://nnmclub.to");

        public TrackerSettings Bitru = new TrackerSettings("https://bitru.org");

        public TrackerSettings Toloka = new TrackerSettings("https://toloka.to", enable: false);

        public TrackerSettings Rutracker = new TrackerSettings("https://rutracker.net", enable: false, priority: "torrent");

        public TrackerSettings Selezen = new TrackerSettings("https://selezen.org", enable: false, priority: "torrent");

        public TrackerSettings Anilibria = new TrackerSettings("https://www.anilibria.tv");

        public TrackerSettings Animelayer = new TrackerSettings("http://animelayer.ru", enable: false);


        public ProxySettings proxy = new ProxySettings();

        public List<ProxySettings> globalproxy = new List<ProxySettings>()
        {
            new ProxySettings()
            {
                pattern = "\\.onion",
                list = new List<string>() { "socks5://127.0.0.1:9050" }
            }
        };
    }
}
