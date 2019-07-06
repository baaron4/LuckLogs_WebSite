using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckLogsApp.Models
{
    public class ProfessionDPSModel
    {
        public string ProfName { get; set; }
        public string Build { get; set; }
        public int AvgDPS { get; set; }
        public int MaxDPS { get; set; }
        public int MaxDPSID { get; set; }
        public int MinDPS { get; set; }
        public int MinDPSID { get; set; }
        public int Entrys { get; set; }
        public List<int[]> AvgOverTime { get; set; }//[date,avgdps,entriesatdate]   avgDPS = ((avgDPS * (entrys - 1)) + player.bossDPS) / (entrys);
    }
}