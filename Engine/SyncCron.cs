using JacRed.Engine.CORE;
using JacRed.Engine.Parse;
using JacRed.Models.tParse;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace JacRed.Engine
{
    public static class SyncCron
    {
        static long lastsync = -1;

        async public static Task Run()
        {
            while (true)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(AppInit.conf.syncapi))
                    {
                        if (lastsync == -1 && File.Exists("lastsync.txt"))
                            lastsync = long.Parse(File.ReadAllText("lastsync.txt"));

                        var root = await HttpClient.Get<JObject>($"{AppInit.conf.syncapi}/sync/torrents?time={lastsync}");
                        if (root != null && root.ContainsKey("torrents"))
                        {
                            var torrents = root.Value<JArray>("torrents").ToObject<Dictionary<string, TorrentDetails>>();
                            if (torrents != null && torrents.Count > 0)
                            {
                                foreach (var torrent in torrents)
                                {
                                    if (!tParse.db.TryGetValue(torrent.Key, out TorrentDetails t))
                                    {
                                        tParse.db.TryAdd(torrent.Key, torrent.Value);
                                        continue;
                                    }

                                    if (t.updateTime > torrent.Value.updateTime)
                                        continue;

                                    tParse.db[torrent.Key] = torrent.Value;
                                }

                                lastsync = torrents.Last().Value.updateTime.ToBinary();
                                File.WriteAllText("lastsync.txt", lastsync.ToString());

                                if (root.Value<int>("count") > torrents.Count)
                                    continue;
                            }
                        }
                    }
                }
                catch { }

                await Task.Delay(TimeSpan.FromHours(5));
            }
        }
    }
}
