using System;
using System.Collections.Generic;
using System.Linq;
using JacRed.Engine.Parse;
using Microsoft.AspNetCore.Mvc;

namespace JacRed.Controllers
{
    [Route("/sync/[action]")]
    public class SyncController : Controller
    {
        public JsonResult Torrents(long time)
        {
            if (!AppInit.conf.opensync || time == 0)
                return Json(new List<string>());

            DateTime lastsync = time == -1 ? default : DateTime.FromFileTimeUtc(time);
            var query = tParse.db.OrderBy(i => i.Value.updateTime).Where(i => i.Value.updateTime > lastsync);

            return Json(new { count = query.Count(), torrents = query.Take(1000) });
        }
    }
}
