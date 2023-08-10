using JacRed.Models.Tracks;
using System;
using System.Collections.Generic;

namespace JacRed.Models.tParse
{
    public class TorrentDetails : ICloneable
    {
        public string trackerName { get; set; }

        public string[] types { get; set; }

        public string url { get; set; }


        public string title { get; set; }

        public int sid { get; set; }

        public int pir { get; set; }

        public double size { get; set; }

        public string sizeName { get; set; }

        public DateTime createTime { get; set; } = DateTime.Now;

        public DateTime updateTime { get; set; } = DateTime.UtcNow;

        public string magnet { get; set; }



        public string name { get; set; }

        public string originalname { get; set; }

        public int relased { get; set; }


        public HashSet<string> languages { get; set; }

        public List<ffStream> ffprobe { get; set; }


        #region Быстрая сортировка
        public int quality { get; set; }

        public string videotype { get; set; }

        public HashSet<string> voices { get; set; } = new HashSet<string>();

        public HashSet<int> seasons { get; set; } = new HashSet<int>();
        #endregion


        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
