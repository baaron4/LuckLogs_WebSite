using System.Web;
using System.Web.Mvc;
using LuckLogsApp.Models;
using System.IO;
using System.Linq;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Configuration;
using System.Net;
using System.Diagnostics;
namespace LuckLogsApp.Controllers
{
    public class ApplicationController : Controller
    {
        
        public RaidLogDBContext db = new RaidLogDBContext();
        public RaidLogDBContext DbContext {
            get { return db; }
        }
        public PlayerLogDBContext playerDB = new PlayerLogDBContext();
        public PlayerLogDBContext DbContextPlay {
        get { return playerDB; }
        }

        public ApplicationController() {
            // ViewData["logs"] = from c in db.RaidLogs select c;
            //ViewData["logs"] =  db.RaidLogs.ToList() ;
            

            ViewData["Vale Guardian"] = "https://wiki.guildwars2.com/images/f/fb/Mini_Vale_Guardian.png";
            ViewData["Gorseval"] = "https://wiki.guildwars2.com/images/d/d1/Mini_Gorseval_the_Multifarious.png";
            ViewData["Sabetha"] = "https://wiki.guildwars2.com/images/5/54/Mini_Sabetha.png";
            ViewData["Slothasor"] = "https://wiki.guildwars2.com/images/e/ed/Mini_Slubling.png";
            ViewData["Matthias"] = "https://wiki.guildwars2.com/images/5/5d/Mini_Matthias_Abomination.png";
            ViewData["Keep Construct"] = "https://wiki.guildwars2.com/images/e/ea/Mini_Keep_Construct.png";
            ViewData["Xera"] = "https://wiki.guildwars2.com/images/4/4b/Mini_Xera.png";
            ViewData["Cairn"] = "https://wiki.guildwars2.com/images/b/b8/Mini_Cairn_the_Indomitable.png";
            ViewData["Overseer"] = "https://wiki.guildwars2.com/images/c/c8/Mini_Mursaat_Overseer.png";
            ViewData["Samarog"] = "https://wiki.guildwars2.com/images/f/f0/Mini_Samarog.png";
            ViewData["Deimos"] = "https://wiki.guildwars2.com/images/e/e0/Mini_Ragged_White_Mantle_Figurehead.png";

            ViewData["Warrior"] = " https://wiki.guildwars2.com/images/4/43/Warrior_tango_icon_20px.png";
            ViewData["Berserker"] = "https://wiki.guildwars2.com/images/d/da/Berserker_tango_icon_20px.png";
            ViewData["Spellbreaker"] = "";

           ViewData["Guardian"] = "https://wiki.guildwars2.com/images/8/8c/Guardian_tango_icon_20px.png";
            ViewData["DragonHunter"] = "https://wiki.guildwars2.com/images/c/c9/Dragonhunter_tango_icon_20px.png";
            ViewData["Firebrand"] = "https://wiki.guildwars2.com/images/0/02/Firebrand_tango_icon_20px.png";

            ViewData["Revenant"] = "https://wiki.guildwars2.com/images/b/b5/Revenant_tango_icon_20px.png";
            ViewData["Herald"] = "https://wiki.guildwars2.com/images/6/67/Herald_tango_icon_20px.png";
            ViewData["Renegade"] = "https://wiki.guildwars2.com/images/7/7c/Renegade_tango_icon_20px.png";

            ViewData["Engineer"] = "https://wiki.guildwars2.com/images/2/27/Engineer_tango_icon_20px.png";
            ViewData["Scrapper"] = "https://wiki.guildwars2.com/images/3/3a/Scrapper_tango_icon_200px.png";
            ViewData["Holosmith"] = "https://wiki.guildwars2.com/images/2/28/Holosmith_tango_icon_20px.png";

           ViewData["Ranger"] = "https://wiki.guildwars2.com/images/4/43/Ranger_tango_icon_20px.png";
            ViewData["Druid"] = "https://wiki.guildwars2.com/images/d/d2/Druid_tango_icon_20px.png";
            ViewData["Soulbeast"] = "https://wiki.guildwars2.com/images/7/7c/Soulbeast_tango_icon_20px.png";

            ViewData["Thief"] = "https://wiki.guildwars2.com/images/7/7a/Thief_tango_icon_20px.png";
            ViewData["Daredevil"] = "https://wiki.guildwars2.com/images/e/e1/Daredevil_tango_icon_20px.png";
            ViewData["Deadeye"] = "";

            ViewData["Elementalist"] = "https://wiki.guildwars2.com/images/a/aa/Elementalist_tango_icon_20px.png";
            ViewData["Tempest"] = "https://wiki.guildwars2.com/images/4/4a/Tempest_tango_icon_20px.png";
            ViewData["Weaver"] = "https://wiki.guildwars2.com/images/f/fc/Weaver_tango_icon_20px.png";

            ViewData["Mesmer"] = "https://wiki.guildwars2.com/images/6/60/Mesmer_tango_icon_20px.png";
            ViewData["Chronomancer"] = "https://wiki.guildwars2.com/images/f/f4/Chronomancer_tango_icon_20px.png";
            ViewData["Mirage"] = "https://wiki.guildwars2.com/images/d/df/Mirage_tango_icon_20px.png";

            ViewData["Necromancer"] = "https://wiki.guildwars2.com/images/4/43/Necromancer_tango_icon_20px.png";
            ViewData["Reaper"] = "https://wiki.guildwars2.com/images/1/11/Reaper_tango_icon_20px.png";
            ViewData["Scourge"] = "https://wiki.guildwars2.com/images/0/06/Scourge_tango_icon_20px.png";

           ViewData["Color-Warrior"] = "rgb(255,209,102)";
            ViewData["Color-Berserker"] = "rgb(255,209,102)";
            ViewData["Color-Spellbreaker"] = "rgb(255,209,102)";
            ViewData["Color-Guardian"] = "rgb(114,193,217)";
            ViewData["Color-DragonHunter"] = "rgb(114,193,217)";
            ViewData["Color-Firebrand"] = "rgb(114,193,217)";
            ViewData["Color-Revenant"] = "rgb(209,110,90)";
            ViewData["Color-Herald"] = "rgb(209,110,90)";
            ViewData["Color-Renegade"] = "rgb(209,110,90)";
            ViewData["Color-Engineer"] = "rgb(208,156,89)";
            ViewData["Color-Scrapper"] = "rgb(208,156,89)";
            ViewData["Color-Holosmith"] = "rgb(208,156,89)";
            ViewData["Color-Ranger"] = "rgb(140,220,130)";
            ViewData["Color-Druid"] = "rgb(140,220,130)";
            ViewData["Color-Soulbeast"] = "rgb(140,220,130)";
            ViewData["Color-Thief"] = "rgb(192,143,149)";
            ViewData["Color-Daredevil"] = "rgb(192,143,149)";
            ViewData["Color-Deadeye"] = "rgb(192,143,149)";
            ViewData["Color-Elementalist"] = "rgb(246,138,135)";
            ViewData["Color-Tempest"] = "rgb(246,138,135)";
            ViewData["Color-Weaver"] = "rgb(246,138,135)";
            ViewData["Color-Mesmer"] = "rgb(182,121,213)";
            ViewData["Color-Chronomancer"] = "rgb(182,121,213)";
            ViewData["Color-Mirage"] = "rgb(182,121,213)";
            ViewData["Color-Necromancer"] = "rgb(82,167,111)";
            ViewData["Color-Reaper"] = "rgb(82,167,111)";
            ViewData["Color-Scourge"] = "rgb(82,167,111)";

            ViewData["Aegis"] = "https://wiki.guildwars2.com/images/e/e5/Aegis.png";
            ViewData["Fury"] = "https://wiki.guildwars2.com/images/4/46/Fury.png";
            ViewData["Might"] = "https://wiki.guildwars2.com/images/7/7c/Might.png";
            ViewData["Protection"] = "https://wiki.guildwars2.com/images/6/6c/Protection.png";
            ViewData["Quick"] = "https://wiki.guildwars2.com/images/b/b4/Quickness.png";
            ViewData["Regen"] = "https://wiki.guildwars2.com/images/5/53/Regeneration.png";
            ViewData["Resistance"] = "https://wiki.guildwars2.com/images/thumb/e/e9/Resistance_40px.png/20px-Resistance_40px.png";
            ViewData["Retal"] = "https://wiki.guildwars2.com/images/5/53/Retaliation.png";
            ViewData["Stability"] = "https://wiki.guildwars2.com/images/a/ae/Stability.png";
            ViewData["Swift"] = "https://wiki.guildwars2.com/images/a/af/Swiftness.png";
            ViewData["Vigor"] = "https://wiki.guildwars2.com/images/f/f4/Vigor.png";

            ViewData["Alacrity"] = "https://wiki.guildwars2.com/images/thumb/4/4c/Alacrity.png/20px-Alacrity.png";
            ViewData["GoE"] = "https://wiki.guildwars2.com/images/thumb/f/f0/Glyph_of_Empowerment.png/33px-Glyph_of_Empowerment.png";
            ViewData["GoTL"] = "https://wiki.guildwars2.com/images/thumb/4/45/Grace_of_the_Land.png/25px-Grace_of_the_Land.png";
            ViewData["SunSpirit"] = "https://wiki.guildwars2.com/images/thumb/d/dd/Sun_Spirit.png/33px-Sun_Spirit.png";
            ViewData["FrostSpirit"] = "https://wiki.guildwars2.com/images/thumb/c/c6/Frost_Spirit.png/33px-Frost_Spirit.png";
            ViewData["Strbanner"] = "https://wiki.guildwars2.com/images/thumb/e/e1/Banner_of_Strength.png/33px-Banner_of_Strength.png";
            ViewData["Discbanner"] = "https://wiki.guildwars2.com/images/thumb/5/5f/Banner_of_Discipline.png/33px-Banner_of_Discipline.png";


            ViewData["Condi"] = "https://wiki.guildwars2.com/images/5/54/Condition_Damage.png";
            ViewData["Healing"] = "https://wiki.guildwars2.com/images/8/81/Healing_Power.png";
            ViewData["Tough"] = "https://wiki.guildwars2.com/images/1/12/Toughness.png";
        }
    }
}