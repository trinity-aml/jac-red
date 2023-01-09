using Microsoft.AspNetCore.Mvc;
using JacRed.Engine.Parse;
using JacRed.Engine;
using System.Threading.Tasks;

namespace JacRed.Controllers
{
    [Route("/jsondb/[action]")]
    public class DbController : BaseController
    {
        static bool _saveDbWork = false;

        async public Task<string> Save()
        {
            if (_saveDbWork)
                return "work";

            _saveDbWork = true;

            try
            {
                await tParse.SaveAndUpdateDB();
            }
            catch { }

            _saveDbWork = false;
            return "ok";
        }
    }
}
