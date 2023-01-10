using System.Collections.Generic;

namespace JacRed.Models.Sync
{
    public class RootObject
    {
        public int count { get; set; }

        public List<Torrent> torrents { get; set; }
    }
}
