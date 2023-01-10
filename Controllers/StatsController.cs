using System;
using System.Collections.Generic;
using System.Linq;
using JacRed.Engine.Parse;
using Microsoft.AspNetCore.Mvc;

namespace JacRed.Controllers
{
    [Route("/stats/[action]")]
    public class StatsController : Controller
    {
        public JsonResult Torrents(string trackerName)
        {
            if (!AppInit.conf.openstats)
                return Json(new { });

            if (string.IsNullOrWhiteSpace(trackerName))
            {
                var stats = new Dictionary<string, (string lastnewtor, int newtor, int update, int nullParse, int alltorrents)>();

                foreach (var t in tParse.db.Values.OrderByDescending(i => i.createTime))
                {
                    if (!stats.TryGetValue(t.trackerName, out var val))
                        stats.Add(t.trackerName, (t.createTime.ToString("dd.MM.yyyy"), 0, 0, 0, 0));

                    var s = stats[t.trackerName];
                    s.alltorrents = s.alltorrents + 1;

                    if (t.createTime >= DateTime.Today)
                        s.newtor = s.newtor + 1;

                    if (t.updateTime >= DateTime.Today)
                        s.update = s.update + 1;

                    if (t.magnet == null)
                        s.nullParse = s.nullParse + 1;

                    stats[t.trackerName] = s;
                }

                return Json(stats.OrderByDescending(i => i.Value.alltorrents).Select(i => new 
                {
                    trackerName = i.Key,
                    i.Value.lastnewtor,
                    i.Value.newtor,
                    i.Value.update,
                    i.Value.nullParse,
                    i.Value.alltorrents,
                }));
            }
            else
            {
                var torrents = tParse.db.Values.Where(i => i.trackerName == trackerName);

                return Json(new
                {
                    nullParse = torrents.Where(i => i.magnet == null).OrderByDescending(i => i.createTime).Select(i =>
                    {
                        return new
                        {
                            i.trackerName,
                            i.types,
                            i.url,

                            i.title,
                            i.sid,
                            i.pir,
                            i.size,
                            i.sizeName,
                            i.createTime,
                            i.updateTime,
                            i.magnet,

                            i.name,
                            i.originalname,
                            i.relased
                        };
                    }),
                    lastToday = torrents.Where(i => i.createTime >= DateTime.Today).OrderByDescending(i => i.createTime).Select(i =>
                    {
                        return new
                        {
                            i.trackerName,
                            i.types,
                            i.url,

                            i.title,
                            i.sid,
                            i.pir,
                            i.size,
                            i.sizeName,
                            i.createTime,
                            i.updateTime,
                            i.magnet,

                            i.name,
                            i.originalname,
                            i.relased
                        };
                    }),
                    lastCreateTime = torrents.OrderByDescending(i => i.createTime).Take(40).Select(i =>
                    {
                        return new
                        {
                            i.trackerName,
                            i.types,
                            i.url,

                            i.title,
                            i.sid,
                            i.pir,
                            i.size,
                            i.sizeName,
                            i.createTime,
                            i.updateTime,
                            i.magnet,

                            i.name,
                            i.originalname,
                            i.relased
                        };
                    })
                });
            }
        }
    }
}
