using Microsoft.AspNetCore.Mvc;
using JacRed.Engine.Parse;
using JacRed.Engine;

namespace JacRed.Controllers
{
    [Route("/jsondb/[action]")]
    public class DbController : BaseController
    {
        static bool _saveDbWork = false;

        public string Save()
        {
            if (_saveDbWork)
                return "work";

            _saveDbWork = true;

            try
            {
                tParse.SaveAndUpdateDB();
            }
            catch { }

            _saveDbWork = false;
            return "ok";
        }
    }
}
