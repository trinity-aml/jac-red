using System;
using System.Collections.Generic;
using System.Linq;
using JacRed.Engine.Parse;
using JacRed.Models.tParse.AniLibria;
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

            int take = 10_000;
            DateTime lastsync = time == -1 ? default : DateTime.FromFileTimeUtc(time);

            return Json(new { take, torrents = tParse.db.OrderBy(i => i.Value.updateTime).Where(i => i.Value.updateTime > lastsync).Take(take) });
        }
    }
}
