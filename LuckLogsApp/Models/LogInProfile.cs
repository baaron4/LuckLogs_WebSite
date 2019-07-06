using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckLogsApp.Models
{
    public class LogInProfile
    {
        public int profileID { get; set; }
        public string profileName { get; set; }
        public string profileImg { get; set; }
        public string passwordHash { get; set; }
        public string email { get; set; }
        public bool emailConfirmed { get; set; }
        public string gw2profileName { get; set; }
        //consider adding in way to track alt accounts
        public string gw2APICode { get; set; }
        public DateTime dateCreated { get; set; }
    }
}