using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckLogsApp.Models
{
    public class ViewModel
    {
        public IEnumerable<LuckLogsApp.Models.RaidLog> DbList { get; set; }
        public IEnumerable<LuckLogsApp.Models.RaidLog> GroupLogList { get; set; }
        public LuckLogsApp.Models.RaidLog Rl;
        public IEnumerable<LuckLogsApp.Models.PlayerLog> PlList { get; set; }
        public IEnumerable<LuckLogsApp.Models.PlayerLog> DPSPlayerList { get; set; }
        public IEnumerable<LuckLogsApp.Models.PlayerLog> TankPlayerList { get; set; }
        public IEnumerable<LuckLogsApp.Models.PlayerLog> HealerPlayerList { get; set; }
        public IEnumerable<LuckLogsApp.Models.PlayerLog> MightPlayerList { get; set; }
        public int PartialID { get; set; }

    }
}