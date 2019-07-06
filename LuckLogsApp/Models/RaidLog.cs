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
    public class RaidLog
    {
        public int ID { get; set; }
        public string EVTCFile { get; set; }
        public string HtmlFile { get; set; }
        public string BossName { get; set; }
        public bool Sucess { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan KillTime { get; set; }
        public string CompTable { get; set; }
        public string killDate { get; set; }
        public int uploaderID { get; set; }
        public int relRate { get; set; }
        public int groupID { get; set; }
    }

   
    public class RaidLogDBContext : DbContext {
        public RaidLogDBContext() : base(GetRDSConnectionString()) { }
        //  public static RaidLogDBContext Create() { return new RaidLogDBContext(); }
        public DbSet<RaidLog> RaidLogs { get; set; }
       
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