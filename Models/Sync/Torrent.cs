using JacRed.Models.tParse;

namespace JacRed.Models.Sync
{
    public class Torrent
    {
        public string key { get; set; }

        public TorrentDetails value { get; set; }
    }
}
