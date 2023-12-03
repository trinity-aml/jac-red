using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Globalization;
using System.Text;
using System.Threading;
using JacRed.Engine;
using Microsoft.Extensions.Logging;

namespace JacRed
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //var db = JsonStream.Read<ConcurrentDictionary<string, TorrentDetails>>("Data/torrents.json");
            //var dblost = JsonStream.Read<ConcurrentDictionary<string, TorrentDetails>>("Data/lost.json");

            //foreach (var torrent in dblost)
            //{
            //    if (torrent.Value.trackerName != "lostfilm" && torrent.Value.trackerName != "hdrezka")
            //        continue;

            //    if (db.ContainsKey(torrent.Key))
            //        continue;

            //    db.TryAdd(torrent.Key, torrent.Value);
            //}

            //JsonStream.Write("Data/torrents.json", db);


            ThreadPool.QueueUserWorkItem(async _ => await SyncCron.Run());

            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(op => op.Listen((AppInit.conf.listenip == "any" ? IPAddress.Any : IPAddress.Parse(AppInit.conf.listenip)), AppInit.conf.listenport))
                    .UseStartup<Startup>();
                });
    }
}
