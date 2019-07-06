using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using LuckLogsApp.Models;
using LuckLogsApp.Models.ParseModels;
using LuckLogsApp.Models.ParseEnums;

using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Configuration;
using System.Diagnostics;
using HtmlAgilityPack;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace LuckLogsApp.Controllers
{
    public class RaidLogsController : ApplicationController
    {

       // private RaidLogDBContext db = new RaidLogDBContext();
        private static readonly string _awsAccessKey = ConfigurationManager.AppSettings["AWSAccessKey"];
        private static readonly string _awsSecretKey = ConfigurationManager.AppSettings["AWSSecretKey"];
        private static readonly string _bucketName = ConfigurationManager.AppSettings["Bucketname"];

        // GET: RaidLogs
        //[Authorize]
        public ActionResult Index()
        {
            //Database.SetInitializer(new DropCreateDatabaseIfModelChanges<RaidLogDBContext>());
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();
            return View(model);
        }

        // GET: RaidLogs/Details/5
        [Authorize]
        public ActionResult Details(int? id)
        {
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RaidLog raidLog = db.RaidLogs.Find(id);
            if (raidLog == null)
            {
                return HttpNotFound();
            }
            TempData["Log"] = raidLog;
            return View(model);
        }

        // GET: RaidLogs/Delete/5
        //[Authorize]
        public ActionResult Delete(int? id)
        {
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RaidLog raidLog = db.RaidLogs.Find(id);
            if (raidLog == null)
            {
                return HttpNotFound();
            }
            TempData["Log"] = raidLog;
            return View(model);
        }

        // POST: RaidLogs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            RaidLog raidLog = db.RaidLogs.Find(id);
            if(raidLog.EVTCFile != null) { DeleteS3Item(raidLog.EVTCFile); }
            if (raidLog.HtmlFile != null) { DeleteS3Item(raidLog.HtmlFile); }
            db.RaidLogs.Remove(raidLog);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        // GET: RaidLogs/Parse/5 ---------------------------------------------------------------------------------------------------------------------------------------
        //[Authorize]
        public ActionResult Parse(int? id)
        {
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RaidLog raidLog = db.RaidLogs.Find(id);
            if (raidLog == null)
            {
                return HttpNotFound();
            }
            //TempData["Log"] = raidLog;
            return View(model);
        }

        // POST: RaidLogs/Parse/5
        [HttpPost, ActionName("Parse")]
        [ValidateAntiForgeryToken]
        public ActionResult ParseConfirmed(int id)
        {
            bool p = ParseLog(id);
            if (p)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Error");
            }
           
        }
        public static byte[] StreamToBytes(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }


        // Private Methods
        private Stream origstream =  null;
        private MemoryStream stream = new MemoryStream();
        private void safeSkip(long bytes_to_skip)
        {

            while (bytes_to_skip > 0)
            {
                int dummyByte = stream.ReadByte();
                long bytes_actually_skipped = 1;
                if (bytes_actually_skipped > 0)
                {
                    bytes_to_skip -= bytes_actually_skipped;
                }
                else if (bytes_actually_skipped == 0)
                {
                    if (stream.ReadByte() == -1)
                    {
                        break;
                    }
                    else
                    {
                        bytes_to_skip--;
                    }
                }
            }
            
            return;
        }

        private int getbyte()
        {
            byte byt = Convert.ToByte(stream.ReadByte());
           // stream.Position++;

            return byt;
        }
        private int getShort()
        {
            byte[] bytes = new byte[2];
            for (int b = 0; b < bytes.Length; b++)
            {
                bytes[b] = Convert.ToByte(stream.ReadByte());
                //stream.Position++;
            }
            // return Short.toUnsignedInt(ByteBuffer.wrap(bytes).order(ByteOrder.LITTLE_ENDIAN).getShort());
            return BitConverter.ToUInt16(bytes,0);
        }

        private int getInt()
        {
            byte[] bytes = new byte[4];
            for (int b = 0; b < bytes.Length; b++)
            {
                bytes[b] = Convert.ToByte(stream.ReadByte());
               // stream.Position++;
            }
            //return ByteBuffer.wrap(bytes).order(ByteOrder.LITTLE_ENDIAN).getInt();
            return BitConverter.ToInt32(bytes,0);
        }

        private long getLong()
        {
            byte[] bytes = new byte[8];
            for (int b = 0; b < bytes.Length; b++)
            {
                bytes[b] = Convert.ToByte(stream.ReadByte());
               // stream.Position++;
            }

            // return ByteBuffer.wrap(bytes).order(ByteOrder.LITTLE_ENDIAN).getLong();
            return BitConverter.ToInt64(bytes,0);
        }

        private String getString(int length)
        {
            byte[] bytes = new byte[length];
            for(int b = 0;b < bytes.Length;b++) {
                bytes[b] = Convert.ToByte(stream.ReadByte());
               // stream.Position++;
            }
           
            string s = new String(System.Text.Encoding.UTF8.GetString(bytes).ToCharArray()).TrimEnd();
            if (s != null) {
                return s;
            }
            return "UNKNOWN";
        }

        private LogData log_data;
        private BossData boss_data;
        private AgentData agent_data = new AgentData();
        private SkillData skill_data = new SkillData();
        private CombatData combat_data = new CombatData();

        // Public Methods
        public LogData getLogData()
        {
            return log_data;
        }
        public BossData getBossData()
        {
            return boss_data;
        }
        public AgentData getAgentData()
        {
            return agent_data;
        }
        public SkillData getSkillData()
        {
            return skill_data;
        }
        public CombatData getCombatData()
        {
            return combat_data;
        }

        public bool ParseLog(int ID)
        {

            bool sucessfulParse = false;
            RaidLog raidLog = db.RaidLogs.Find(ID);
            WebClient client = new WebClient();
            origstream = client.OpenRead(raidLog.EVTCFile);
            origstream.CopyTo(stream);
            stream.Position = 0;
            
            TempData["Debug"] = "";
            
            parseBossData();
            parseAgentData();
            parseSkillData();
            parseCombatList();
            fillMissingData();

            origstream.Close();
            stream.Close();
            sucessfulParse = true;

            CreateHTML();
            return (sucessfulParse);
        }

        private void parseBossData()
        {
            // 12 bytes: arc build version
            String build_version = getString(12);
            this.log_data = new LogData(build_version);

            // 1 byte: skip
            safeSkip(1);

            // 2 bytes: boss instance ID
            int instid = getShort();

            // 1 byte: position
            safeSkip(1);

            //Save
           // TempData["Debug"] = build_version +" "+ instid.ToString() ;
            this.boss_data = new BossData(instid);
        }

        private void parseAgentData() {
            // 4 bytes: player count
            int player_count = getInt();
            //TempData["Debug"] += "player count:" + player_count.ToString();

            // 96 bytes: each player
            for (int i = 0; i < player_count; i++)
            {
                // 8 bytes: agent
                long agent = getLong();

                // 4 bytes: profession
                int prof = getInt();

                // 4 bytes: is_elite
                int is_elite = getInt();

                // 4 bytes: toughness
                int toughness = getInt();

                // 4 bytes: healing
                int healing = getInt();

                // 4 bytes: condition
                int condition = getInt();

                // 68 bytes: name
                String name = getString(68);
                //Save
                //TempData["Debug"] += "<br/> " + agent.ToString() + " "+ name +" "+ prof.ToString() + " "+ is_elite.ToString() + " "+ toughness.ToString()+ " "+healing.ToString() +" "+condition.ToString();
                Agent a = new Agent(agent,name,prof,is_elite);
                if (a != null)
                {
                    // NPC
                    if (a.getProf(this.log_data.getBuildVersion()) =="NPC")
                    {
                        agent_data.addItem(a, new AgentItem(agent, name, a.getName()+":"+prof.ToString().PadLeft(5,'0')), this.log_data.getBuildVersion());//a.getName() + ":" + String.format("%05d", prof)));
                    }
                    // Gadget
                    else if (a.getProf(this.log_data.getBuildVersion()) == "GDG")
                    {
                        agent_data.addItem(a, new AgentItem(agent, name, a.getName() + ":" + (prof & 0x0000ffff).ToString().PadLeft(5, '0')), this.log_data.getBuildVersion());//a.getName() + ":" + String.format("%05d", prof & 0x0000ffff)));
                    }
                    // Player
                    else
                    {
                        agent_data.addItem(a, new AgentItem(agent, name, a.getProf(this.log_data.getBuildVersion()), toughness, healing, condition), this.log_data.getBuildVersion());
                        //TempData["Debug"] += "<br/>"+is_elite.ToString() + a.getProf(this.log_data.getBuildVersion());
                    }
                }
                // Unknown
                else
                {
                    agent_data.addItem(a, new AgentItem(agent, name, prof.ToString(), toughness, healing, condition),this.log_data.getBuildVersion());
                }
            }
        }

        private void parseSkillData() {
            // 4 bytes: player count
            int skill_count = getInt();
            //TempData["Debug"] += "Skill Count:" + skill_count.ToString();
            // 68 bytes: each skill
            for (int i = 0; i < skill_count; i++)
            {
                // 4 bytes: skill ID
                int skill_id = getInt();

                // 64 bytes: name
                String name = getString(64);

                //Save
                //TempData["Debug"] += "<br/>" + skill_id + " " + name;
                skill_data.addItem(new SkillItem(skill_id, name));
            }
        }

        private void parseCombatList() {
            // 64 bytes: each combat
            while (stream.Length - stream.Position >= 64)
            {
                // 8 bytes: time
                int time = (int)getLong();

                // 8 bytes: src_agent
                long src_agent = getLong();

                // 8 bytes: dst_agent
                long dst_agent = getLong();

                // 4 bytes: value
                int value = getInt();

                // 4 bytes: buff_dmg
                int buff_dmg = getInt();

                // 2 bytes: overstack_value
                int overstack_value = getShort();

                // 2 bytes: skill_id
                int skill_id = getShort();

                // 2 bytes: src_instid
                int src_instid = getShort();

                // 2 bytes: dst_instid
                int dst_instid = getShort();

                // 2 bytes: src_master_instid
                int src_master_instid = getShort();

                // 9 bytes: garbage
                safeSkip(9);

                // 1 byte: iff
                //IFF iff = IFF.getEnum(f.read());
                IFF iff = new IFF(Convert.ToByte(stream.ReadByte())); //Convert.ToByte(stream.ReadByte());

                // 1 byte: buff
                int buff = stream.ReadByte();

                // 1 byte: result
                //Result result = Result.getEnum(f.read());
                Result result = new Result( Convert.ToByte(stream.ReadByte()));

                // 1 byte: is_activation
                //Activation is_activation = Activation.getEnum(f.read());
                Activation is_activation = new Activation( Convert.ToByte(stream.ReadByte()));

                // 1 byte: is_buffremove
                //BuffRemove is_buffremove = BuffRemove.getEnum(f.read());
                BuffRemove is_buffremoved = new BuffRemove( Convert.ToByte(stream.ReadByte()));

                // 1 byte: is_ninety
                int is_ninety = stream.ReadByte();

                // 1 byte: is_fifty
                int is_fifty = stream.ReadByte();

                // 1 byte: is_moving
                int is_moving = stream.ReadByte();

                // 1 byte: is_statechange
                //StateChange is_statechange = StateChange.getEnum(f.read());
                StateChange is_statechange = new StateChange( Convert.ToByte(stream.ReadByte()));

                // 1 byte: is_flanking
                int is_flanking = stream.ReadByte();

                // 3 bytes: garbage
                safeSkip(3);

                //save
                // Add combat
                combat_data.addItem(new CombatItem(time, src_agent, dst_agent, value, buff_dmg, overstack_value, skill_id,
                        src_instid, dst_instid, src_master_instid, iff, buff, result, is_activation, is_buffremoved,
                        is_ninety, is_fifty, is_moving, is_statechange, is_flanking));
            }
        }

        private void fillMissingData() {
            // Set Agent instid, first_aware and last_aware
            List<AgentItem> player_list = agent_data.getPlayerAgentList();
            List<AgentItem> agent_list = agent_data.getAllAgentsList();
            List<CombatItem> combat_list = combat_data.getCombatList();
            foreach (AgentItem a in agent_list)
            {
                bool assigned_first = false;
                foreach (CombatItem c in combat_list)
                {
                    if (a.getAgent() == c.getSrcAgent() && c.getSrcInstid() != 0)
                    {
                        if (!assigned_first)
                        {
                            a.setInstid(c.getSrcInstid());
                            a.setFirstAware(c.getTime());
                            assigned_first = true;
                        }
                        a.setLastAware(c.getTime());
                    }
                    else if (a.getAgent() == c.getDstAgent() && c.getDstInstid() != 0)
                    {
                        if (!assigned_first)
                        {
                            a.setInstid(c.getDstInstid());
                            a.setFirstAware(c.getTime());
                            assigned_first = true;
                        }
                        a.setLastAware(c.getTime());
                    }
                    else if (c.isStateChange().getEnum() == "POINT_OF_VIEW")
                    {
                        int pov_instid = c.getSrcInstid();
                        foreach (AgentItem p in player_list)
                        {
                            if (pov_instid == p.getInstid())
                            {
                                log_data.setPOV(p.getName());
                            }
                        }

                    }
                    else if (c.isStateChange().getEnum() == "LOG_START")
                    {
                        log_data.setLogStart(c.getValue());
                    }
                    else if (c.isStateChange().getEnum() == "LOG_END")
                    {
                        log_data.setLogEnd(c.getValue());
                    }
                }
            }

            // Manual log target selection NOT NEEDED?
            //if (boss_data.getID() == 1)
            //{
            //    targetSelection();
            //}

            // Set Boss data agent, instid, first_aware, last_aware and name
            List<AgentItem> NPC_list = agent_data.getNPCAgentList();
            foreach (AgentItem NPC in NPC_list)
            {
                if (NPC.getProf().EndsWith(boss_data.getID().ToString()))
                {
                    if (boss_data.getAgent() == 0)
                    {
                        boss_data.setAgent(NPC.getAgent());
                        boss_data.setInstid(NPC.getInstid());
                        boss_data.setFirstAware(NPC.getFirstAware());
                        boss_data.setName(NPC.getName());
                    }
                    boss_data.setLastAware(NPC.getLastAware());
                }
            }

            // Set Boss health
            foreach (CombatItem c in combat_list)
            {
                if (c.getSrcInstid() == boss_data.getInstid() && c.isStateChange().getEnum() == "MAX_HEALTH_UPDATE")
                {
                    boss_data.setHealth((int)c.getDstAgent());
                    break;
                }
            }

            // Dealing with second half of Xera | ((22611300 * 0.5) + (25560600 *
            // 0.5)
            int xera_2_instid = 0;
            foreach (AgentItem NPC in NPC_list)
            {
                if (NPC.getProf().Contains("16286"))
                {
                    xera_2_instid = NPC.getInstid();
                    boss_data.setHealth(24085950);
                    boss_data.setLastAware(NPC.getLastAware());
                    foreach (CombatItem c in combat_list)
                    {
                        if (c.getSrcInstid() == xera_2_instid)
                        {
                            c.setSrcInstid(boss_data.getInstid());
                        }
                        if (c.getDstInstid() == xera_2_instid)
                        {
                            c.setDstInstid(boss_data.getInstid());
                        }
                    }
                    break;
                }
            }
        }
        //Generate HTML---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //Call in upload process after ParseLog() is called
        public List<Player> p_list = new List<Player>();
        public string CreateHTML() {
            string HTML_CONTENT = "";
            string HTML_Head = "<!DOCTYPE html><html lang=\"en\"><head> " +
                "<meta charset=\"utf-8\">" +
                 "<link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap.min.css\" integrity=\"sha384-BVYiiSIFeK1dGmJRAkycuHAHRg32OmUcww7on3RYdg4Va+PmSTsz/K68vbdEjh4u\" crossorigin=\"anonymous\">" +
            "<link rel=\"stylesheet\" href=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/css/bootstrap-theme.min.css\" integrity=\"sha384-rHyoN1iRsVXV4nD0JutlnGaslCJuC7uwjduW9SVrLvRYooPp2bWYgmgJQIXwl/Sp\" crossorigin=\"anonymous\">" +
            "<link href=\"https://fonts.googleapis.com/css?family=Open+Sans\" rel=\"stylesheet\">" +
            "" +
            "<script src=\"https://code.jquery.com/jquery-3.1.1.min.js\" integrity=\"sha256-hVVnYaiADRTO2PzUGmuLJr8BLUSjGIZsDYGmIJLv2b8=\" crossorigin=\"anonymous\"></script>" +
            "<script src=\"https://maxcdn.bootstrapcdn.com/bootstrap/3.3.7/js/bootstrap.min.js\" integrity=\"sha384-Tc5IQib027qvyjSMfHjOMaLkfuWVxZxUPnCJA7l2mCWNIpG9mGCD8wGNIcPD7Txa\" crossorigin=\"anonymous\"></script>" +
            "<style>body { font-family: 'Open Sans', sans-serif;color: #444;}</style>" +
                    
                 "</head>";
            string HTML_Body = "<body><h3>Testing here ow for data that looks like page</h3></body>";
            string HTML_foot = "</html>";
            HTML_CONTENT = HTML_Head + HTML_Body +HTML_foot;
            // Players
            List<AgentItem> playerAgentList = getAgentData().getPlayerAgentList();
            foreach (AgentItem playerAgent in playerAgentList)
            {
                p_list.Add(new Player(playerAgent));
            }

            // Sort
            // p_list.sort((a, b)->Integer.parseInt(a.getGroup()) - Integer.parseInt(b.getGroup()));
            //foreach (Player player in p_list) {
            //    TempData["Debug"] += "<br/>" + player.getCharacter() +" "+ player.getAccount() + " "+player.getGroup().ToString();
            //}
            return HTML_CONTENT;
        }
        public void dummySave(string filename) {
            string nameShort = filename.Substring(0, Math.Min(filename.Length, 15));
            var itemEVTC = db.RaidLogs.FirstOrDefault(i => i.EVTCFile.Contains(nameShort));//db search evtc

            //Create stream
            byte[] byteArray = System.Text.Encoding.ASCII.GetBytes(CreateHTML());
            Stream stream = new MemoryStream(byteArray);
            //create file
            string bossname = getBossData().getName();
            string fileAttr = bossname.Substring(0, 4);
            HttpPostedFileBase endFile = new MemoryFile(stream, "text/html", nameShort+"_AB_"+ string.Join("", fileAttr.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries))  +".html"); 
            //send file to Amazon
            string fileLink = UploadToS3(endFile, "htmlfiles/");
            //Save info to DB
            itemEVTC.BossName = bossname;
            itemEVTC.HtmlFile = fileLink;
            //itemEVTC.arcVersion = getLogData().getBuildVersion();
            itemEVTC.killDate = getLogData().getLogEnd();
            itemEVTC.relRate = 100;
            itemEVTC.uploaderID = 1;
            itemEVTC.groupID = 1;
            db.Entry(itemEVTC).State = EntityState.Modified;
            db.SaveChanges();
        }
        // GET: RaidLogs/Scrape/5------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        [Authorize]
        public ActionResult Scrape(int? id)
        {
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            RaidLog raidLog = db.RaidLogs.Find(id);
            if (raidLog == null)
            {
                return HttpNotFound();
            }
            TempData["Log"] = raidLog;
            return View(model);
        }

        // POST: RaidLogs/Scrape/5
        [HttpPost, ActionName("Scrape")]
        [ValidateAntiForgeryToken]
        public ActionResult ScrapeConfirmed(int id)
        {
            bool p = ScrapeAll();
            if (p)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Error");
            }

        }

        public bool ScrapeAll() {
            foreach (var item in db.RaidLogs.ToList()) {

                //  ScrapeLog(item.ID);
                updateDBValues(item.ID);
                
            }
            return true;
        }
        public bool updateDBValues(int ID) {
            RaidLog raidLog = db.RaidLogs.Find(ID);
            //raidLog.killDate = raidLog.HtmlFile.Substring(51,15);
            //raidLog.uploaderID = 1;
            //raidLog.relRate = 100;
            raidLog.groupID = 1;
            db.Entry(raidLog).State = EntityState.Modified;
            db.SaveChanges();
            return true;
        }

        public bool ScrapeLog(int ID)
        {
            bool sucessfulScrape = false;
            
            RaidLog raidLog = db.RaidLogs.Find(ID);
            var html = @raidLog.HtmlFile;
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(html);
            //getting boss name
            var bossNode = htmlDoc.DocumentNode.SelectSingleNode("//body/div/div/center/h1");
            raidLog.BossName = bossNode.InnerHtml;
            //getting comp table
            var compNode = htmlDoc.DocumentNode.SelectSingleNode("//body/div/div/center/table");
            raidLog.CompTable = compNode.OuterHtml;
            //parseing success string for kill time and successbool
            var durationNodeSuc = htmlDoc.DocumentNode.SelectSingleNode("//body/div/div/center/p[@class='text text-success']");
            var durationNodeFail = htmlDoc.DocumentNode.SelectSingleNode("//body/div/div/center/p[@class='text text-danger']");
            bool successBool = false;
            if (durationNodeSuc != null)
            {
                successBool = true;
                decimal d;
                string[] array = durationNodeSuc.InnerHtml.Split(' ').Where(s => decimal.TryParse(s, out d)).ToArray();
                int m = int.Parse(array[0]);
                int sec = (int)Math.Truncate(decimal.Parse(array[1]));
                //int ms = (int)((decimal.Parse(array[1]) - sec) * 100);
                raidLog.KillTime = new TimeSpan(0, m, sec);
            }
            if ( bossNode.InnerHtml == "Deimos")
            {// Deimos
                successBool = true;
                var durNode = htmlDoc.DocumentNode.SelectNodes("//body/div/div/center/p[@class='text text-muted']");
                decimal d;
                string[] array = durNode[1].InnerHtml.Split(' ').Where(s => decimal.TryParse(s, out d)).ToArray();
                int m = int.Parse(array[0]);
                int sec = (int)Math.Truncate(decimal.Parse(array[1]));
                //int ms = (int)((decimal.Parse(array[1]) - sec) * 100);
                raidLog.KillTime = new TimeSpan(0, m, sec);
            }
            raidLog.Sucess = successBool;
            db.Entry(raidLog).State = EntityState.Modified;
            if (!successBool) {
                db.SaveChanges();
                return false;
            }
            var totalTable = htmlDoc.DocumentNode.SelectNodes("//div[@class = 'tab-content']")[0];

            //Create dps player entrys
            var dpstable = totalTable.SelectNodes(".//div[@id='s_glob']/table/tbody/tr");
            var boontable = totalTable.SelectNodes(".//div[@id='bs_glob']/table/tbody/tr");
            //Check if entrys arleady exist
            var playLogs = playerDB.PlayerLogs.Where(i=>i.logID == ID).ToList();
            
            foreach (HtmlNode row in dpstable)
            {
                PlayerLog playLog = new PlayerLog();
                //Get TD array
                var td = row.SelectNodes("td");
                //Get PlayerName
                string playerName;
                if (td[2].SelectSingleNode("span") != null)
                {
                    playerName = td[2].InnerText.Substring(0, td[2].InnerText.Length - 4);
                }
                else
                {
                    playerName = td[2].InnerText;
                }
                //Does exist?
                PlayerLog entryExisting = null;
                if (playLogs != null)
                {
                    entryExisting = playLogs.FirstOrDefault(x => x.playerName == playerName);
                    if (entryExisting != null) {
                        playLog = entryExisting;
                    }
                }
                playLog.uploaderID = 1;//Change when Upload processs improves:baaron = 1
                playLog.groupID = 1;//Change Luck = 1
                playLog.reliabilityRate = 100;//reduces as flagged
                playLog.logID = ID;
                playLog.bossName = bossNode.InnerHtml;
                playLog.subGroup = Int32.Parse(td[0].InnerHtml);
                playLog.profession = td[1].SelectSingleNode("img").Attributes["alt"].Value;
                playLog.playerName = playerName;
                playLog.accountName = td[3].InnerText;
                playLog.bossDPS = Int32.Parse( td[4].InnerText);
                playLog.bossDMG = Int32.Parse(td[4].SelectSingleNode("span").Attributes["title"].Value.Substring(0, td[4].SelectSingleNode("span").Attributes["title"].Value.Length-3));
                playLog.bossDPSPhys = Int32.Parse(td[5].InnerText);
                playLog.bossDMGPhys = Int32.Parse(td[5].SelectSingleNode("span").Attributes["title"].Value.Substring(0, td[5].SelectSingleNode("span").Attributes["title"].Value.Length - 3));
                playLog.bossDPSCondi = Int32.Parse(td[6].InnerText);
                playLog.bossDMGCondi = Int32.Parse(td[6].SelectSingleNode("span").Attributes["title"].Value.Substring(0, td[6].SelectSingleNode("span").Attributes["title"].Value.Length - 3));
                playLog.allDPS = Int32.Parse(td[8].InnerText);
                playLog.allDMG = Int32.Parse(td[8].SelectSingleNode("span").Attributes["title"].Value.Substring(0, td[8].SelectSingleNode("span").Attributes["title"].Value.Length - 3));
                playLog.allDPSPhys = Int32.Parse(td[9].InnerText);
                playLog.allDMGPhys = Int32.Parse(td[9].SelectSingleNode("span").Attributes["title"].Value.Substring(0, td[9].SelectSingleNode("span").Attributes["title"].Value.Length - 3));
                playLog.allDPSCondi = Int32.Parse(td[10].InnerText);
                playLog.allDMGCondi = Int32.Parse(td[10].SelectSingleNode("span").Attributes["title"].Value.Substring(0, td[10].SelectSingleNode("span").Attributes["title"].Value.Length - 3));
                if (playLog.allDMGCondi > playLog.allDMGPhys)
                {
                    playLog.build = "Condi";
                }
                else {
                    playLog.build = "Power";
                }
                playLog.incDMG = Int32.Parse(td[12].InnerText);
                playLog.wastedTime = Decimal.Parse(td[13].InnerText.Substring(0, td[13].InnerText.Length-1));
                playLog.wastedAmount = Int32.Parse(td[13].SelectSingleNode("span").Attributes["title"].Value.Substring(0, td[13].SelectSingleNode("span").Attributes["title"].Value.Length - 6));
                playLog.failed = Int32.Parse(td[14].InnerText);
                String critString = new String(td[15].SelectSingleNode("span").Attributes["title"].Value.TakeWhile(Char.IsDigit).ToArray());
                playLog.critHits = Int32.Parse(critString);
                String scholarString = new String(td[16].SelectSingleNode("span").Attributes["title"].Value.TakeWhile(Char.IsDigit).ToArray());
                playLog.scholarHits = Int32.Parse(scholarString);
                String SwsString = new String(td[17].SelectSingleNode("span").Attributes["title"].Value.TakeWhile(Char.IsDigit).ToArray());
                playLog.swsHits = Int32.Parse(SwsString);
                String hitsString = td[15].SelectSingleNode("span").Attributes["title"].Value.Substring(critString.Length + 4, td[15].SelectSingleNode("span").Attributes["title"].Value.Length - 4 - critString.Length);
                playLog.hits = Int32.Parse(hitsString);
                playLog.downs = Int32.Parse(td[18].InnerText);
                if (td[19].InnerHtml != null) {
                    if (td[19].SelectSingleNode("img") != null)
                    {
                        int m = 0;
                        int s = 0;
                        if (td[19].SelectSingleNode("img").Attributes["alt"].Value == "Dead") { 
                        
                        var charArray = td[19].SelectSingleNode("img").Attributes["title"].Value.ToCharArray();
                            for (int i = 0; i < charArray.Length; i++)
                            {
                                if (charArray[i] == 'm')
                                {
                                    if (i == 1) { m = Int32.Parse(charArray[i - 1].ToString()); }//1 digit long
                                    if (i == 2) { m = Int32.Parse(charArray[i - 2].ToString() + charArray[i - 1].ToString()); }//2 digit long
                                }

                                if (charArray[i] == 's')
                                {
                                    if (i == 1) { s = Int32.Parse(charArray[i - 1].ToString()); }//1 digit long
                                    if (i == 2) { s = Int32.Parse(charArray[i - 2].ToString() + charArray[i - 1].ToString()); }//2 digit long
                                    if (i > 2)
                                    {
                                        if (charArray[i - 2] == ' ') { s = Int32.Parse(charArray[i - 1].ToString()); }//1 digit
                                        if (charArray[i - 3] == ' ') { s = Int32.Parse(charArray[i - 2].ToString() + charArray[i - 1].ToString()); }//2 digit
                                    }
                                }
                            }
                        }
               
                        playLog.timeDead = new TimeSpan(0,m, s);
                        //TempData["Debug"] =new TimeSpan(0, m, s);
                   }
                }
                //Add disconent possibility log 145
                if (entryExisting != null)
                {
                    playerDB.Entry(playLog).State = EntityState.Modified;
                }
                else {
                    playerDB.PlayerLogs.Add(playLog);
                }
               
            }
            playerDB.SaveChanges();
            db.SaveChanges();

            //GetBoons
            playLogs = playerDB.PlayerLogs.Where(i => i.logID == ID).ToList();
            var boonHeader = totalTable.SelectNodes(".//div[@id='bs_glob']/table/thead/tr/th");
            if (boontable != null)
            {
                foreach (HtmlNode row in boontable)
                {
                    PlayerLog playLog = new PlayerLog();
                    //Get TD array
                    var td = row.SelectNodes("td");
                    //Get PlayerName
                    string playerName;
                    if (td[2].SelectSingleNode("span") != null)
                    {
                        playerName = td[2].InnerText.Substring(0, td[2].InnerText.Length - 4);
                    }
                    else
                    {
                        playerName = td[2].InnerText;
                    }
                    //Does exist?
                    PlayerLog entryExisting = null;
                    if (playLogs != null)
                    {
                        entryExisting = playLogs.FirstOrDefault(x => x.playerName == playerName);
                        if (entryExisting != null)
                        {
                            playLog = entryExisting;
                        }
                    }
                    int col = 0;
                    foreach (var header in boonHeader)
                    {
                        if (header.SelectSingleNode("img") != null)
                        {
                            string boon = header.SelectSingleNode("img").Attributes["alt"].Value;
                            switch (boon)
                            {
                                case "Protection":
                                    playLog.protection = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Fury":
                                    playLog.fury = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Might":
                                    playLog.might = Double.Parse(td[col].InnerText);
                                    break;
                                case "Aegis":
                                    playLog.aegis = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Retaliation":
                                    playLog.retail = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Quickness":
                                    playLog.quickness = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Sun Spirit":
                                    playLog.sunSpirit = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Spirit of Frost":
                                    playLog.frostSpirit = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Stone Spirit":
                                    playLog.stoneSpirit = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Spotter":
                                    playLog.spotter = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Empower Allies":
                                    playLog.empowerAllies = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Banner of Strength":
                                    playLog.bannerStr = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Banner of Discipline":
                                    playLog.bannerDisc = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Alacrity":
                                    playLog.alacrity = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Glyph of Empowerment":
                                    playLog.glyphEmpower = Int32.Parse(td[col].InnerText.Substring(0, td[col].InnerText.Length - 1));
                                    break;
                                case "Grace of the Land":
                                    playLog.GoTL = Double.Parse(td[col].InnerText);
                                    break;
                                default:
                                    break;
                            }
                        }
                        col++;

                    }


                    if (entryExisting != null)
                    {
                        playerDB.Entry(playLog).State = EntityState.Modified;
                    }
                    
                }
            }

            playerDB.SaveChanges();
            db.SaveChanges();

            //Player Stats and boon gen
            var playerStats = totalTable.SelectNodes(".//div[starts-with(@id,'ps_glob')]");
            playLogs = playerDB.PlayerLogs.Where(i => i.logID == ID).ToList();
            if (playerStats != null)
            {
                foreach (HtmlNode tab in playerStats)
                {

                    PlayerLog playLog = new PlayerLog();
                    //Get name in tab
                    var nameNode = tab.SelectNodes(".//li")[0];

                    //TempData["Debug"] = nameNode[0].OuterHtml;
                    //Does exist?
                    PlayerLog entryExisting = null;
                    if (playLogs != null)
                    {
                        //Get PlayerName
                        string playerName;
                        if (nameNode != null)
                        {
                            playerName = nameNode.InnerText.Substring(1, nameNode.InnerText.Length - 1);//This tab has a space in front of name holy fuck

                            entryExisting = playLogs.FirstOrDefault(x => x.playerName == playerName);

                            if (entryExisting != null)
                            {

                                playLog = entryExisting;
                            }
                            //TempData["Debug"] = playerName;
                        }

                    }
                    //GET WEAPON HERE??

                    //Get rezes/reztime/condi cleanse/condiclleanse time
                    var support = tab.SelectNodes(".//div[starts-with(@id,'psu_glob')]/table");
                    var sup1td = support[0].SelectNodes(".//tr/td");
                    if (sup1td == null) { sup1td = support[0].SelectNodes(".//tbody/tr/td"); }

                    for (int k = 0; k < sup1td.Count; k++)
                    {
                        //TempData["Debug"] = TempData["Debug"] +"                      "+support[0].OuterHtml;
                        switch (sup1td[k].InnerText)
                        {
                            case "Resurrects":
                                playLog.resurects = Int32.Parse(sup1td[k + 1].InnerText);
                                break;
                            case "Ressurect Time":
                                playLog.resTime = Double.Parse(sup1td[k + 1].InnerText.TrimEnd(new char[] { 's' }));
                                break;
                            case "Conditions Cleansed":
                                int cEnd = 1;
                                int cStart = 0;
                                bool firstS = false;
                                foreach (char c in sup1td[k + 1].InnerText)
                                {
                                    if (c == 's' && firstS == false)
                                    {
                                        playLog.condiCleanseTime = Double.Parse(sup1td[k + 1].InnerText.Substring(0, cEnd - 1));
                                        firstS = true;
                                        //TempData["Debug"] = sup1td[k + 1].InnerText.Substring(0, cEnd - 1);
                                    }
                                    if (c == '(')
                                    {
                                        cStart = cEnd;
                                    }
                                    if (c == 't')
                                    {
                                        playLog.condiCleanse = Int32.Parse(sup1td[k + 1].InnerText.Substring(cStart, cEnd - 2 - cStart));
                                    }
                                    cEnd++;
                                }

                                break;
                            default:
                                break;
                        }
                        if (support.Count == 2)
                        {
                            var sup2td = support[1].SelectNodes(".//tbody/tr/td");
                            for (int j = 0; j < sup2td.Count; j++)
                            {
                                switch (sup2td[j].InnerText)
                                {
                                    case " Aegis":
                                        playLog.genSelf_Aegis = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Aegis = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Aegis = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Aegis = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Fury":
                                        playLog.genSelf_Fury = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Fury = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Fury = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Fury = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Might":
                                        playLog.genSelf_Might = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Might = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Might = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Might = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Protection":
                                        playLog.genSelf_Protection = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Protection = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Protection = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Protection = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Quickness":
                                        playLog.genSelf_Quickness = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Quickness = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Quickness = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Quickness = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Regeneration":
                                        playLog.genSelf_Regen = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Regen = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Regen = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Regen = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Resistance":
                                        playLog.genSelf_Resist = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Resist = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Resist = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Resist = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Retaliation":
                                        playLog.genSelf_Retail = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Retail = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Retail = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Retail = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Stability":
                                        playLog.genSelf_Stability = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Stability = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Stability = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Stability = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Swiftness":
                                        playLog.genSelf_Swift = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Swift = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Swift = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Swift = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Vigor":
                                        playLog.genSelf_Vigor = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Vigor = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Vigor = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Vigor = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Alacrity":
                                        playLog.genSelf_Alacrity = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_Alacrity = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_Alacrity = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_Alacrity = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    case " Grace of the Land":
                                        playLog.genSelf_GoTL = Decimal.Parse(sup2td[j + 1].InnerText);
                                        if (sup2td[j + 2].InnerText != "nan") playLog.genGroup_GoTL = Decimal.Parse(sup2td[j + 2].InnerText);
                                        if (sup2td[j + 3].InnerText != "nan") playLog.genOGroup_GoTL = Decimal.Parse(sup2td[j + 3].InnerText);
                                        if (sup2td[j + 4].InnerText != "nan") playLog.genSquad_GoTL = Decimal.Parse(sup2td[j + 4].InnerText);
                                        break;
                                    default:
                                        break;
                                }
                            }
                        }
                    }

                    //TempData["Debug"] = support[0].OuterHtml;
                    if (entryExisting != null)
                    {

                        playerDB.Entry(playLog).State = EntityState.Modified;
                    }
                    //else
                    //{
                    //    playerDB.PlayerLogs.Add(playLog);
                    //}

                }
            }
            playerDB.SaveChanges();
            db.SaveChanges();
            
            //Player Rotation for weapons
            SetSkillList();
            var playerRotation = totalTable.SelectNodes(".//div[starts-with(@id,'prot_glob')]");
            playLogs = playerDB.PlayerLogs.Where(i => i.logID == ID).ToList();
            if (playerRotation != null)
            {
                foreach (HtmlNode tab in playerRotation)
                {

                    PlayerLog playLog = new PlayerLog();
                    //Get name in tab
                    var nameNode = tab.SelectSingleNode(".//center/h4");
                    //Does exist?
                    PlayerLog entryExisting = null;
                    if (playLogs != null)
                    {
                        //Get PlayerName
                        string playerName;
                        if (nameNode != null)
                        {
                            playerName = nameNode.InnerText;

                            entryExisting = playLogs.FirstOrDefault(x => x.playerName == playerName);

                            if (entryExisting != null)
                            {

                                playLog = entryExisting;
                            }
                        }

                    }
                    //Finding Weapons
                    var fullRot = tab.SelectNodes(".//img");
                    string wep1 = null;
                    string wep2 = null;
                    string wep3 = null;
                    string wep4 = null;
                    int wep1Num = -1;
                    int wep2Num = -1;
                    int wep3Num = -1;
                    int wep4Num = -1;
                    if (fullRot != null) {
                        int timesSwapped = 0;
                        bool skipSkill = false;
                        foreach (var skillImg in fullRot) {
                            if (skipSkill) { skipSkill = false; continue; }
                            if (wep1 == null || wep2 == null || wep3 == null || wep4 == null)
                            {
                                string skillName = skillImg.Attributes["title"].Value.Split('[')[0];//cut off ms
                                if (skillName.Length <= 0)
                                {
                                    continue;
                                }
                                else {
                                    skillName = skillName.Substring(0, skillName.Length - 1);//Cut off space
                                }

                                // TempData["Debug"] = TempData["Debug"] + fullRot.Count.ToString() +" / ";
                                if (skillName == "Dodge" || skillName == "Lightning Storm" || skillName == "Static Field" || skillName == "Ring of Fire" || skillName == "Wind Blast" || skillName == "Thunderclap" || skillName == "Geyser")//Elementalist skill namers are bad
                                {
                                    continue;
                                }
                                if (skillName == "Weapon Swap")
                                {
                                    timesSwapped++;
                                    skipSkill = true;
                                    continue;
                                }
                                GW2APISkill skillMatch = SkillListWeap.Items.FirstOrDefault(x => x.name == skillName);
                                if (skillMatch != null)
                                {
                                    if (skillMatch.type == "Weapon")
                                    {
                                        //Two handed wep
                                        if (skillMatch.weapon_type == "Greatsword" || skillMatch.weapon_type == "Hammer" || skillMatch.weapon_type == "Longbow" || skillMatch.weapon_type == "Shortbow" || skillMatch.weapon_type == "Rifle" || skillMatch.weapon_type == "Staff")
                                        {
                                            //Check2 hander if on same swap
                                            if (wep1Num == timesSwapped && wep2Num == timesSwapped) { wep1 = skillMatch.weapon_type; wep2 = wep1; wep1Num = timesSwapped; wep2Num = wep1Num; }
                                            else if (wep3Num == timesSwapped && wep4Num == timesSwapped) { wep3 = skillMatch.weapon_type; wep4 = wep3; wep3Num = timesSwapped; wep4Num = timesSwapped; }
                                            
                                            else //if same as before at least change num swap
                                            if (wep1 == skillMatch.weapon_type && wep2 == skillMatch.weapon_type) { wep1Num = timesSwapped; wep2Num = timesSwapped; } else if (wep3 == skillMatch.weapon_type && wep4 == skillMatch.weapon_type) { wep3Num = timesSwapped; wep4Num = timesSwapped; }else
                                            //set to 1 and 2 or 3 4 if taken
                                            if (wep1 == null && wep2 == null) { wep1 = skillMatch.weapon_type; wep2 = wep1; wep1Num = timesSwapped; wep2Num = wep1Num; } else if(wep3 == null && wep4 == null) { wep3 = skillMatch.weapon_type; wep4 = wep3; wep3Num = timesSwapped; wep4Num = timesSwapped; }
                                            
                                        }
                                        else
                                        //Thief 3 skill
                                        if (skillMatch.duel_wield != null)
                                        {
                                            //Check duel if on same swap
                                            if (wep1Num == timesSwapped || wep2Num == timesSwapped) { wep1 = skillMatch.weapon_type; wep2 = skillMatch.duel_wield; wep1Num = timesSwapped; wep2Num = wep1Num; }
                                            else if (wep3Num == timesSwapped || wep4Num == timesSwapped) { wep3 = skillMatch.weapon_type; wep4 = skillMatch.duel_wield; wep3Num = timesSwapped; wep4Num = timesSwapped; }
                                            else
                                                //if same as before at least change num swap
                                                if (wep1 == skillMatch.weapon_type && wep2 == skillMatch.duel_wield) { wep1Num = timesSwapped; wep2Num = timesSwapped; } else if (wep3 == skillMatch.weapon_type && wep4 == skillMatch.duel_wield) { wep3Num = timesSwapped; wep4Num = timesSwapped; }else
                                            //set to 1 and 2 or 3 4 if taken
                                            if (wep1 == null && wep2 == null) { wep1 = skillMatch.weapon_type; wep2 = skillMatch.duel_wield; } else if(wep3 == null && wep4 == null) { wep3 = skillMatch.weapon_type; wep4 = skillMatch.duel_wield; }
                                               
                                        }
                                        else
                                        //Off hand only
                                        if (skillMatch.weapon_type == "Focus" || skillMatch.weapon_type == "Shield" || skillMatch.weapon_type == "Torch" || skillMatch.weapon_type == "Warhorn")
                                        {
                                            //Check main hand if on same swap
                                            if (wep1Num == timesSwapped) { wep2 = skillMatch.weapon_type; wep2Num = timesSwapped; }
                                            else if (wep3Num == timesSwapped) { wep4 = skillMatch.weapon_type; wep4Num = timesSwapped; }
                                            else
                                            //if same as before at least change num swap
                                            if (wep2 == skillMatch.weapon_type) { wep2Num = timesSwapped; } else if (wep4 == skillMatch.weapon_type) { wep4Num = timesSwapped; }else
                                            //just check if it was null
                                            if (wep2 == null) { wep2 = skillMatch.weapon_type; wep2Num = timesSwapped; }
                                            else if (wep4 == null) { wep4 = skillMatch.weapon_type; wep4Num = timesSwapped; }
                                            
                                            
                                        }
                                        else
                                        //One handed
                                        if (skillMatch.weapon_type == "Axe" || skillMatch.weapon_type == "Dagger" || skillMatch.weapon_type == "Mace" || skillMatch.weapon_type == "Pistol" || skillMatch.weapon_type == "Sword" || skillMatch.weapon_type == "Scepter")
                                        {
                                            if (skillMatch.slot == "Weapon_1" || skillMatch.slot == "Weapon_2" || skillMatch.slot == "Weapon_3")
                                            {//Main hand
                                                //Check off hand if on same swap
                                                if (wep2Num == timesSwapped) { wep1 = skillMatch.weapon_type; wep1Num = timesSwapped; }
                                                else if (wep4Num == timesSwapped) { wep3 = skillMatch.weapon_type; wep3Num = timesSwapped; }
                                                else
                                                //if same as before at least change num swap
                                                if (wep1 == skillMatch.weapon_type) { wep1Num = timesSwapped; } else if (wep3 == skillMatch.weapon_type) { wep3Num = timesSwapped; }else
                                                //just check if it was null
                                                if (wep1 == null) { wep1 = skillMatch.weapon_type; wep1Num = timesSwapped; }
                                                else if (wep3 == null) { wep3 = skillMatch.weapon_type; wep3Num = timesSwapped; }
                                               
                                               
                                            }else
                                            if (skillMatch.slot == "Weapon_4" || skillMatch.slot == "Weapon_5")
                                            {//Like off hand
                                                //Check main hand if on same swap
                                                if (wep1Num == timesSwapped) { wep2 = skillMatch.weapon_type; wep2Num = timesSwapped; }
                                                else if (wep3Num == timesSwapped) { wep4 = skillMatch.weapon_type; wep4Num = timesSwapped; }
                                                //if same as before at least change num swap
                                                else if(wep2 == skillMatch.weapon_type) { wep2Num = timesSwapped; } else if (wep4 == skillMatch.weapon_type) { wep4Num = timesSwapped; }
                                                //just check if it was null
                                                else if(wep2 == null) { wep2 = skillMatch.weapon_type; wep2Num = timesSwapped; }
                                                else if (wep4 == null) { wep4 = skillMatch.weapon_type; wep4Num = timesSwapped; }
                                               
                                                
                                            }
                                        }
                                    }
                                }
                            }
                            else {//All weps found break loop
                                break;
                            }
                           
                            //TempData["Debug"] = TempData["Debug"] + skillName;
                        }
                        //Never found weapon
                        if (wep1 == null) { wep1 = "?"; }
                        if (wep2 == null) { wep2 = "?"; }
                        if (wep3 == null) { wep3 = "?"; }
                        if (wep4 == null) { wep4 = "?"; }
                    }
                    playLog.weapons = wep1 + "," + wep2 + "/" + wep3 + "," + wep4;
                    //TempData["Debug"] = TempData["Debug"] + "</br>" + nameNode.InnerText +":"+ wep1 + " " + wep2 + "/" + wep3 + " " + wep4 ; 
                    if (entryExisting != null)
                    {
                        playerDB.Entry(playLog).State = EntityState.Modified;
                    }
                }
            }
            playerDB.SaveChanges();
            db.SaveChanges();

            // TempData["Debug"] = SkillListWeap.Count.ToString();
            SetRank(raidLog.BossName);

            sucessfulScrape = true;
            return (sucessfulScrape);
        }
        //sets all playerlogs rank in db of one boss
        public void SetRank(string bossname) {
            var playerList = playerDB.PlayerLogs.Where(y => y.bossName == bossname).ToList();
            var dpslist = playerList.Where(x => db.RaidLogs.FirstOrDefault(y => y.ID == x.logID).Sucess == true).OrderByDescending(x => x.bossDPS);
            var tanklist = playerList.Where(z => z.profession == "Chronomancer").OrderByDescending(x => x.genGroup_Quickness);
            var heallist = playerList.Where(z => z.profession == "Druid").OrderByDescending(x => x.genGroup_GoTL);
            var mightlist = playerList.Where(z => z.profession == "Berserker").OrderByDescending(x => x.genGroup_Might);

            int i = 0;
            foreach (var item in dpslist)
            {
                i++;
                item.dpsRank = i;
                playerDB.Entry(item).State = EntityState.Modified;
            }
            int j = 0;
            foreach (var item in tanklist)
            {
                j++;
                item.suppRank = j;
                playerDB.Entry(item).State = EntityState.Modified;
            }
            int k = 0;
            foreach (var item in heallist)
            {
                k++;
                item.suppRank = k;
                playerDB.Entry(item).State = EntityState.Modified;
            }
            int n = 0;
            foreach (var item in mightlist)
            {
                n++;
                item.suppRank = n;
                playerDB.Entry(item).State = EntityState.Modified;
            }
            playerDB.SaveChanges();
        }

        //GW2 API Methods---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        
        static HttpClient APIClient { get; set; }
        private void GetAPIClient() {
            if (APIClient == null) {
                APIClient = new HttpClient();
                APIClient.BaseAddress = new Uri("https://api.guildwars2.com");
                APIClient.DefaultRequestHeaders.Accept.Clear();
                APIClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            }
            return;
        }
        private GW2APISkill GetGW2APISKill(string path)
        {
            //System.Threading.Thread.Sleep(100);
            if (APIClient == null) { GetAPIClient(); }
            GW2APISkill skill = null;
            HttpResponseMessage response = APIClient.GetAsync(path).Result;
            if (response.IsSuccessStatusCode)
            {
                skill = response.Content.ReadAsAsync<GW2APISkill>().Result;
            }
            return skill;
        }
        public GW2APISpec GetGW2APISpec(string path)
        {
            //System.Threading.Thread.Sleep(100);
            if (APIClient == null) { GetAPIClient(); }
            GW2APISpec spec = null;
            HttpResponseMessage response = APIClient.GetAsync(path).Result;
            if (response.IsSuccessStatusCode)
            {
                spec = response.Content.ReadAsAsync<GW2APISpec>().Result;
            }
            return spec;
        }
        [XmlRoot("ArrayOfGW2APISkill")]
        public class  SkillListWeapClass {  
            public SkillListWeapClass() { Items = new List<GW2APISkill>(); }
            [XmlElement("GW2APISkill")]
            public List<GW2APISkill> Items { get; set; }
         }
        static SkillListWeapClass SkillListWeap;
        private void SetSkillList() {

            if (SkillListWeap == null) {
                //Get list from local XML
                using (var reader = new StreamReader(Server.MapPath("~/Content/") + "SkillList.txt"))
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(SkillListWeapClass));
                    SkillListWeap = (SkillListWeapClass)deserializer.Deserialize(reader);
                    
                    reader.Close();
                }
                if (SkillListWeap.Items.Count == 0)
                {
                    //Get list from API
                    GetAPIClient();
                    SkillListWeap = new SkillListWeapClass();
                    HttpResponseMessage response = APIClient.GetAsync("/v2/skills").Result;
                    int[] idArray;
                    if (response.IsSuccessStatusCode)
                    {
                        // Get Skill ID list
                        idArray = response.Content.ReadAsAsync<int[]>().Result;
                        TempData["Debug"] = "ID ARRAY: " + idArray.Length;
                        foreach (int id in idArray)
                        {
                            GW2APISkill curSkill = new GW2APISkill();
                            curSkill = GetGW2APISKill("/v2/skills/" + id);
                            if (curSkill != null)
                            {
                                if (curSkill.type == "Weapon")
                                {
                                    SkillListWeap.Items.Add(curSkill);
                                    // TempData["Debug"] = TempData["Debug"] + "</br>" + curSkill.id + ":" + curSkill.name;
                                }
                            }
                            else
                            {
                                //TempData["Debug"] = TempData["Debug"] + "</br>" + "FAIL TO GET "+id;
                            }

                        }
                        Stream stream = System.IO.File.OpenWrite(Server.MapPath("~/Content/") + "SkillList.txt");
                        XmlSerializer xmlSer = new XmlSerializer(typeof(List<GW2APISkill>));
                        xmlSer.Serialize(stream, SkillListWeap.Items);
                        stream.Close();
                    }
                }
            }



            return;
        }
        public ActionResult Skills()
        {
            //Weapon testing
            
            SetSkillList();

            TempData["Skills"] = "Total of:" + SkillListWeap.Items.Count + "Weapon Skills";
            foreach (GW2APISkill skill in SkillListWeap.Items) {
                TempData["Skills"] = TempData["Skills"] + "</br>" + skill.id + ":" +"<img src='"+ skill.icon+ "'style='width: 30px; height: 30px; '>" + skill.name;
            }
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            // model.PlList = playerDB.PlayerLogs.ToList();
            return View(model);
        }
        //AMAZON Services Methods
        // static IAmazonS3 AS3Client { get; set; }
        private IAmazonS3 GetAmazonS3Client()
        {
            //if (AS3Client == null) {
            //    AS3Client = Amazon.AWSClientFactory.CreateAmazonS3Client(_awsAccessKey, _awsSecretKey, Amazon.RegionEndpoint.USEast1);
            //}
            return Amazon.AWSClientFactory.CreateAmazonS3Client(_awsAccessKey, _awsSecretKey, Amazon.RegionEndpoint.USEast1);
        }

        public string UploadToS3(HttpPostedFileBase file, string folderLoc)
        {
            //try
            //{
            //    ListBucketsResponse response = s3Client.ListBuckets();
            //    List<S3Bucket> buckets = response.Buckets;
            //    foreach (S3Bucket bucket in buckets)
            //    {
            //        System.Diagnostics.Debug.WriteLine("Found bucket name {0} created at {1}", bucket.BucketName, bucket.CreationDate);
            //    }
            //}
            //catch (AmazonS3Exception e)
            //{
            //    System.Diagnostics.Debug.WriteLine("Bucket listing has failed.");
            //    System.Diagnostics.Debug.WriteLine("Amazon error code: {0}",
            //        string.IsNullOrEmpty(e.ErrorCode) ? "None" : e.ErrorCode);
            //    System.Diagnostics.Debug.WriteLine("Exception message: {0}", e.Message);
            //}
            System.Diagnostics.Debug.WriteLine("Sending to Amazon S3");
            //Upload to S3
            using (IAmazonS3 s3Client = GetAmazonS3Client())
            {
                try
                {
                    var request = new PutObjectRequest()
                    {
                        BucketName = _bucketName,
                        CannedACL = S3CannedACL.PublicRead,//PERMISSION TO FILE PUBLIC ACCESIBLE
                        Key = string.Format(folderLoc + "{0}", file.FileName),
                        InputStream = file.InputStream//SEND THE FILE STREAM
                    };
                    s3Client.PutObject(request);

                }
                catch (AmazonS3Exception e)
                {

                }
            }
            System.Diagnostics.Debug.WriteLine("Amazon Sucess retrieval");
            return "https://s3.amazonaws.com/" + _bucketName + "/" + folderLoc + file.FileName;
            //return View();
        }

        public string UploadToS3(string fileLoc, string folderLoc)
        {
            System.Diagnostics.Debug.WriteLine("Sending to Amazon S3");
            //Upload to S3
            using (IAmazonS3 s3Client = GetAmazonS3Client())
            {
                try
                {
                    var request = new PutObjectRequest()
                    {
                        BucketName = _bucketName,
                        CannedACL = S3CannedACL.PublicRead,//PERMISSION TO FILE PUBLIC ACCESIBLE
                        Key = string.Format(folderLoc + "{0}", Path.GetFileName(fileLoc))
                    };
                    using (FileStream stream = new FileStream(fileLoc, FileMode.Open))
                    {
                        request.InputStream = stream;
                        // Put object
                        PutObjectResponse response = s3Client.PutObject(request);
                    }
                }
                catch (AmazonS3Exception e) {   }
            }
            System.Diagnostics.Debug.WriteLine("Amazon Sucess retrieval");
            return "https://s3.amazonaws.com/" + _bucketName + "/" + folderLoc + Path.GetFileName(fileLoc);
        }

        public void DeleteS3Item(string fileLoc) {
            System.Diagnostics.Debug.WriteLine("Deleting Amazon S3 item");
            string keyFile = fileLoc.Substring(41, fileLoc.Length - 41);
            using (IAmazonS3 s3Client = GetAmazonS3Client())
            {
                try
                {
                    DeleteObjectRequest request = new DeleteObjectRequest()
                    {
                        BucketName = _bucketName,
                        Key = keyFile
                    };
                    s3Client.DeleteObject(request);
                    System.Diagnostics.Debug.WriteLine("Deleting...");
                }
                catch (AmazonS3Exception e) { System.Diagnostics.Debug.WriteLine(e.Message, e.InnerException); }
            }
            System.Diagnostics.Debug.WriteLine("Deleted");
            return;
        }

        public ActionResult UploadProcess(IEnumerable<HttpPostedFileBase> files)
        {
            foreach (var file in files)
            {
                //Get ext Type
                var ext = Path.GetExtension(file.FileName).ToLower();
                string awsLoc = "";
                string nameShort = file.FileName.Substring(0, Math.Min(file.FileName.Length, 15));
                var itemEVTC = db.RaidLogs.FirstOrDefault(i => i.EVTCFile.Contains(nameShort));//db search evtc
                var itemHtml = db.RaidLogs.FirstOrDefault(a => a.HtmlFile.Contains(nameShort));//db search html
                if (ext == ".evtc")
                {
                    awsLoc = "evtcfiles/";
                    ////Search existing db if(evtc found ) dont contine     
                    if (itemEVTC != null) { System.Diagnostics.Debug.WriteLine("itemEVTC wasnt null"); itemEVTC = null; continue; }
                    ////if(html found) upload and add to database entry
                    if (itemHtml != null)
                    {
                        System.Diagnostics.Debug.WriteLine("itemHtml wasnt null");
                        string fileLink = UploadToS3(file, awsLoc);
                        RaidLog rl = (RaidLog)itemHtml;
                        rl.EVTCFile = fileLink;
                        rl.killDate = file.FileName.Substring(0,15);
                        rl.relRate = 100;
                        rl.uploaderID = 1;
                        rl.groupID = 1;
                        db.Entry(itemHtml).State = EntityState.Modified;
                        db.SaveChanges();
                        continue;
                    }
                    //if (nothing found) uplaod save evtc to db entry
                    RaidLog raidLog = new RaidLog();
                    raidLog.EVTCFile = UploadToS3(file, awsLoc);
                    raidLog.Date = DateTime.Now;
                    raidLog.killDate = file.FileName.Substring(0, 15);
                    raidLog.relRate = 100;
                    raidLog.uploaderID = 1; //LUCKGroup by default
                    raidLog.groupID = 1;
                    db.RaidLogs.Add(raidLog);
                    db.SaveChanges();
                    //Run raid heros and fill html entry
                    ParseLog(raidLog.ID);//TEST
                    dummySave(file.FileName);//TEST
                }
                else if (ext == ".html")
                {
                    awsLoc = "htmlfiles/";
                    ////if(evtc found) upload and add to database entry
                    if (itemEVTC != null)
                    {
                        System.Diagnostics.Debug.WriteLine("itemEVTC wasnt null");
                        string fileLink = UploadToS3(file, awsLoc);
                        RaidLog rl = (RaidLog)itemEVTC;
                        rl.HtmlFile = fileLink;
                        rl.killDate = file.FileName.Substring(0, 15);
                        rl.relRate = 100;
                        rl.uploaderID = 1;
                        rl.groupID = 1;
                        db.Entry(itemEVTC).State = EntityState.Modified;
                        db.SaveChanges();
                        ScrapeLog(rl.ID);
                        continue;
                    }
                    ////Search existing db if(html found ) dont contine
                    if (itemHtml != null) { System.Diagnostics.Debug.WriteLine("itemHtml wasnt null");itemHtml = null; continue; }
                    //if (nothing found) uplaod save html to db entry
                    RaidLog raidLog = new RaidLog();
                    raidLog.HtmlFile = UploadToS3(file, awsLoc);
                    //scrape
                    raidLog.Date = DateTime.Now;
                    raidLog.killDate = file.FileName.Substring(0, 15);
                    raidLog.relRate = 100;
                    raidLog.uploaderID = 1; //LUCKGroup by default
                    raidLog.groupID = 1;
                    db.RaidLogs.Add(raidLog);
                    db.SaveChanges();
                    SetBossFromHTML(raidLog.ID);
                   // SetRank(raidLog.);
                }
            }
            return Json(files.Count() + "file(s) uploaded successfully");
        }

        //This should be achieved threw scrapeing not this method 
        public void SetBossFromHTML(int id) {
            RaidLog rl = db.RaidLogs.Find(id);
            string bossTag = rl.HtmlFile.Substring(51+15,rl.HtmlFile.Length-20-51);
            switch (bossTag) {
                case "_vg":
                    rl.BossName = "Vale Guardian";
                    break;
                case "_gorse":
                    rl.BossName = "Gorseval";
                    break;
                case "_sab":
                    rl.BossName = "Sabetha";
                    break;
                case "_sloth":
                    rl.BossName = "Slothasor";
                    break;
                case "_matt":
                    rl.BossName = "Matthias";
                    break;
                case "_kc":
                    rl.BossName = "Keep Construct";
                    break;
                case "_xera":
                    rl.BossName = "Xera";
                    break;
                case "_cairn":
                    rl.BossName = "Cairn";
                    break;
                case "_mo":
                    rl.BossName = "Overseer";
                    break;
                case "_sam":
                    rl.BossName = "Samarog";
                    break;
                case "_dei":
                    rl.BossName = "Deimos";
                    break;
                default:
                    rl.BossName = "Undefined";
                    break;
            }
            rl.Sucess = true;
            db.Entry(rl).State = EntityState.Modified;
            db.SaveChanges();
            return;
        }
        //Used to attach to end of generate files
        public string SetHTMLFromBoss(string name)
        {
            switch (name)
            {
                case "Vale Guardian":
                    return "vg";
                case "Gorseval":
                    return "gorse";
                case "Sabetha":
                    return "sab";
                case "Slothasor":
                    return "sloth";
                case "Matthias":
                    return "matt";
                case "Keep Construct":
                    return "kc";
                case "Xera":
                    return "xera";
                case "Cairn":
                    return "cairn";
                case "Mursaat Overseer":
                    return "mo";
                case "Samarog":
                    return "sam";
                case "Deimos":
                    return "dei";
                default:
                    return "undef";
                    
            }
            
            
           
        }
    }
}
