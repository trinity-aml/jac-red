using JacRed.Engine.CORE;
using JacRed.Engine.Parse;
using JacRed.Models.Sync;
using JacRed.Models.tParse;
using System;
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

                        var root = await HttpClient.Get<RootObject>($"{AppInit.conf.syncapi}/sync/torrents?time={lastsync}", MaxResponseContentBufferSize: 200_000_000);
                        if (root?.torrents != null && root.torrents.Count > 0)
                        {
                            foreach (var torrent in root.torrents)
                            {
                                if (!tParse.db.TryGetValue(torrent.key, out TorrentDetails t))
                                {
                                    tParse.db.TryAdd(torrent.key, (TorrentDetails)torrent.value.Clone());
                                    continue;
                                }

                                if (t.updateTime > torrent.value.updateTime)
                                    continue;

                                tParse.db[torrent.key] = (TorrentDetails)torrent.value.Clone();
                            }

                            lastsync = root.torrents.Last().value.updateTime.ToFileTimeUtc();
                            File.WriteAllText("lastsync.txt", lastsync.ToString());

                            if (root.take == root.torrents.Count)
                                continue;
                        }
                    }
                }
                catch { }

                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }
}
