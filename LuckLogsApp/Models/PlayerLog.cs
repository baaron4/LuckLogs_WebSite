using System;
using System.Data.Entity;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Configuration;

namespace LuckLogsApp.Models
{
    public class PlayerLog
    {
        public int ID { get; set; }
        public int logID { get; set; }
        public int uploaderID { get; set; }
        public int groupID { get; set; }
        public int reliabilityRate { get; set; }
        public string bossName { get; set; }
        public int dpsRank { get; set; }
        public int suppRank { get; set; }
        public int subGroup { get; set; }
        public string profession { get; set; }
        public string build { get; set; }
        public string weapons { get; set; }
        public string playerName { get; set; }
        public string accountName { get; set; }

        public int bossDPS { get; set; }
        public int bossDMG { get; set; }
        public int bossDPSPhys { get; set; }
        public int bossDMGPhys { get; set; }
        public int bossDPSCondi { get; set; }
        public int bossDMGCondi { get; set; }

        public int allDPS { get; set; }
        public int allDMG { get; set; }
        public int allDPSPhys { get; set; }
        public int allDMGPhys { get; set; }
        public int allDPSCondi { get; set; }
        public int allDMGCondi { get; set; }

        public int incDMG { get; set; }
        public decimal wastedTime { get; set; }
        public int wastedAmount { get; set; }
        public int failed { get; set; }
        
        public int critHits { get; set; }
        public int scholarHits { get; set; }
        public int swsHits { get; set; }
        public int hits { get; set; }
        public int downs { get; set; }
        public TimeSpan timeDead { get; set; }

        public int resurects { get; set; }
        public double resTime { get; set; }
        public int condiCleanse { get; set; }
        public double condiCleanseTime { get; set; }

        public int protection { get; set; }
        public int fury { get; set; }
        public double might { get; set; }
        public int aegis { get; set; }
        public int retail { get; set; }
        public int quickness { get; set; }


        public int sunSpirit { get; set; }
        public int frostSpirit { get; set; }
        public int stoneSpirit { get; set; }
        public int spotter { get; set; }
        public int empowerAllies { get; set; }
        public int bannerStr { get; set; }
        public int bannerDisc { get; set; }
        public int alacrity { get; set; }
        public int glyphEmpower { get; set; }
        public double GoTL { get; set; }

        public decimal genSelf_Aegis { get; set; }
        public decimal genSelf_Fury { get; set; }
        public decimal genSelf_Might { get; set; }
        public decimal genSelf_Protection { get; set; }
        public decimal genSelf_Quickness { get; set; }
        public decimal genSelf_Regen { get; set; }
        public decimal genSelf_Resist { get; set; }
        public decimal genSelf_Retail { get; set; }
        public decimal genSelf_Stability { get; set; }
        public decimal genSelf_Swift { get; set; }
        public decimal genSelf_Vigor { get; set; }
        public decimal genSelf_Alacrity { get; set; }
        public decimal genSelf_GoTL { get; set; }

        public decimal genGroup_Aegis { get; set; }
        public decimal genGroup_Fury { get; set; }
        public decimal genGroup_Might { get; set; }
        public decimal genGroup_Protection { get; set; }
        public decimal genGroup_Quickness { get; set; }
        public decimal genGroup_Regen { get; set; }
        public decimal genGroup_Resist { get; set; }
        public decimal genGroup_Retail { get; set; }
        public decimal genGroup_Stability { get; set; }
        public decimal genGroup_Swift { get; set; }
        public decimal genGroup_Vigor { get; set; }
        public decimal genGroup_Alacrity { get; set; }
        public decimal genGroup_GoTL { get; set; }

        public decimal genOGroup_Aegis { get; set; }
        public decimal genOGroup_Fury { get; set; }
        public decimal genOGroup_Might { get; set; }
        public decimal genOGroup_Protection { get; set; }
        public decimal genOGroup_Quickness { get; set; }
        public decimal genOGroup_Regen { get; set; }
        public decimal genOGroup_Resist { get; set; }
        public decimal genOGroup_Retail { get; set; }
        public decimal genOGroup_Stability { get; set; }
        public decimal genOGroup_Swift { get; set; }
        public decimal genOGroup_Vigor { get; set; }
        public decimal genOGroup_Alacrity { get; set; }
        public decimal genOGroup_GoTL { get; set; }

        public decimal genSquad_Aegis { get; set; }
        public decimal genSquad_Fury { get; set; }
        public decimal genSquad_Might { get; set; }
        public decimal genSquad_Protection { get; set; }
        public decimal genSquad_Quickness { get; set; }
        public decimal genSquad_Regen { get; set; }
        public decimal genSquad_Resist { get; set; }
        public decimal genSquad_Retail { get; set; }
        public decimal genSquad_Stability { get; set; }
        public decimal genSquad_Swift { get; set; }
        public decimal genSquad_Vigor { get; set; }
        public decimal genSquad_Alacrity { get; set; }
        public decimal genSquad_GoTL { get; set; }
    }
    public class PlayerLogDBContext : DbContext
    {
        public PlayerLogDBContext() : base(GetRDSConnectionString()) { }
        public static PlayerLogDBContext Create() { return new PlayerLogDBContext(); }
        public DbSet<PlayerLog> PlayerLogs { get; set; }

        public static string GetRDSConnectionString()
        {
            var appConfig = ConfigurationManager.AppSettings;

            string dbname = appConfig["RDS_DB_NAME"];

            if (string.IsNullOrEmpty(dbname)) return null;

            string username = appConfig["RDS_USERNAME"];
            string password = appConfig["RDS_PASSWORD"];
            string hostname = appConfig["RDS_HOSTNAME"];
            string port = appConfig["RDS_PORT"];

            return "Data Source=" + hostname + ";Initial Catalog=" + dbname + ";User ID=" + username + ";Password=" + password + ";";
        }
    }


}