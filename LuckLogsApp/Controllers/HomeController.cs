using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.Configuration;
using System.Data;
using System.Data.Entity;
using System.Net;
using LuckLogsApp.Models;




namespace LuckLogsApp.Controllers
{
    public class HomeController : ApplicationController
    {
       //initial
       
        public ActionResult Index()
        {
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();
            model.DPSPlayerList = playerDB.PlayerLogs.Where(g => g.dpsRank == 1).ToList();
            return View(model);
            
        }


        public ActionResult Error()
        {
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();
            return View(model);
        }
        public ActionResult SetPatch(int? patchStart, int? patchEnd, int? timeSpan,string patchName, string timeSpanName,string groupName,int? groupID) {
            if (patchStart == null) { patchStart = 0; }
            if (patchEnd == null) { patchEnd = 0; }
            if (timeSpan == null) { timeSpan = 0; }
            if (groupID == null) { groupID = 0; }
            Session["PatchStart"] = patchStart;
            Session["PatchEnd"] = patchEnd;
            Session["TimeSpan"] = timeSpan;
            Session["GroupID"] = groupID;

            Session["PatchName"] = patchName;
            Session["TimeSpanName"] = timeSpanName;
            Session["GroupName"] = groupName;
            return Redirect(Request.UrlReferrer.ToString());
        }
        // GET: Logs
        public ActionResult Logs(int? id)
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

        public ActionResult Rankings(string boss)
        {
            Sessionreset();

            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.Where(y => y.bossName == boss).ToList();
            if ((int)Session["TimeSpan"] > 0) {
                model.PlList = model.PlList.Where(x => Int32.Parse(model.DbList.FirstOrDefault(y => y.ID == x.logID).HtmlFile.Substring(51, 8)) > Int32.Parse(DateTime.Now.AddDays(7 * -((int)Session["TimeSpan"])).ToString("yyyyMMdd"))).ToList();
            }
            if ((int)Session["PatchStart"] > 0) {
                model.PlList = model.PlList.Where(x => Int32.Parse(model.DbList.FirstOrDefault(y => y.ID == x.logID).HtmlFile.Substring(51, 8)) >= (int)Session["PatchStart"]).ToList();
            }
            if ((int)Session["PatchEnd"] > 0)
            {
                model.PlList = model.PlList.Where(x => Int32.Parse(model.DbList.FirstOrDefault(y => y.ID == x.logID).HtmlFile.Substring(51, 8)) < (int)Session["PatchEnd"]).ToList();
            }
            if ((int)Session["GroupID"] > 0)
            {
                model.PlList = model.PlList.Where(x =>  x.groupID == (int)Session["GroupID"]).ToList();
            }
            model.DPSPlayerList = model.PlList.Where(x => model.DbList.FirstOrDefault(y => y.ID == x.logID).Sucess == true).OrderByDescending(x=>x.bossDPS);
            model.TankPlayerList = model.PlList.Where(z => z.profession == "Chronomancer").OrderByDescending(x=>x.genGroup_Quickness);
            model.HealerPlayerList = model.PlList.Where(z => z.profession == "Druid").OrderByDescending(x => x.genGroup_GoTL);
            model.MightPlayerList = model.PlList.Where(z=>z.profession=="Berserker").OrderByDescending(x => x.genGroup_Might);
            //set rank COMMENT THIS FOR DEPLOY
            //int i = 0;
            //foreach (var item in model.DPSPlayerList)
            //{
            //    i++;
            //    item.dpsRank = i;
            //    playerDB.Entry(item).State = EntityState.Modified;
            //}
            //playerDB.SaveChanges();
            //int j = 0;
            //foreach (var item in model.TankPlayerList)
            //{
            //    j++;
            //    item.suppRank = j;
            //    playerDB.Entry(item).State = EntityState.Modified;
            //}
            //int k = 0;
            //foreach (var item in model.HealerPlayerList)
            //{
            //    k++;
            //    item.suppRank = k;
            //    playerDB.Entry(item).State = EntityState.Modified;
            //}
            //int n = 0;
            //foreach (var item in model.MightPlayerList)
            //{
            //    n++;
            //    item.suppRank = n;
            //    playerDB.Entry(item).State = EntityState.Modified;
            //}
            //Set all Failing entrys to rank 0
            //var list = model.DPSPlayerList.Where(x => model.DbList.FirstOrDefault(y => y.ID == x.logID).Sucess == false);
            //foreach (var item in list)
            //{

            //    item.dpsRank = 0;
            //    playerDB.Entry(item).State = EntityState.Modified;
            //}
            playerDB.SaveChanges();
            TempData["Boss"] = boss;
            return View(model);
        }

        public string ShortenWeaponString(string orig) {
            string strNew;
            string set1 = "?,?";
            string set2 = "?,?";
            string[] set = orig.Split('/');
            string[] set1Sp = set[0].Split(',');
            string[] set2Sp = set[1].Split(',');
            for (int i = 0; i <= 1; i++) { 
                switch (set[i]) {
                    case "Greatsword,Greatsword":
                        if(i == 0) set1 = "GS";
                        if(i == 1) set2 = "GS";
                        break;
                    case "Hammer,Hammer":
                        if (i == 0) set1 = "Ham";
                        if (i == 1) set2 = "Ham";
                        break;
                    case "Longbow,Longbow":
                        if (i == 0) set1 = "Lb";
                        if (i == 1) set2 = "Lb";
                        break;
                    case "Shortbow,Shortbow":
                        if (i == 0) set1 = "Sb";
                        if (i == 1) set2 = "Sb";
                        break;
                    case "Rifle,Rifle":
                        if (i == 0) set1 = "Rf";
                        if (i == 1) set2 = "Rf";
                        break;
                    case "Staff,Staff":
                        if (i == 0) set1 = "Staff";
                        if (i == 1) set2 = "Staff";
                        break;
                    default:
                        break;
                }

                for (int j = 0; j <= 1; j++) {
                    string[] wepToUse = new string[1]; ;
                    if (i == 0) { wepToUse = set1Sp; }
                    if (i == 1) { wepToUse = set2Sp; }
                    switch (wepToUse[j]) {
                        case "Sword":
                            if (i == 0) { if (j == 0) { set1 = "Sw" + ","+ set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "Sw"; } }
                            if (i == 1) { if (j == 0) { set2 = "Sw" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "Sw"; } }
                            break;
                        case "Axe":
                            if (i == 0) { if (j == 0) { set1 = "Axe" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "Axe"; } }
                            if (i == 1) { if (j == 0) { set2 = "Axe" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "Axe"; } }
                            break;
                        case "Dagger":
                            if (i == 0) { if (j == 0) { set1 = "D" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "D"; } }
                            if (i == 1) { if (j == 0) { set2 = "D" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "D"; } }
                            break;
                        case "Mace":
                            if (i == 0) { if (j == 0) { set1 = "Mace" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "Mace"; } }
                            if (i == 1) { if (j == 0) { set2 = "Mace" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "Mace"; } }
                            break;
                        case "Pistol":
                            if (i == 0) { if (j == 0) { set1 = "P" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "P"; } }
                            if (i == 1) { if (j == 0) { set2 = "P" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "P"; } }
                            break;
                        case "Scepter":
                            if (i == 0) { if (j == 0) { set1 = "Sce" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "Sce"; } }
                            if (i == 1) { if (j == 0) { set2 = "Sce" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "Sce"; } }
                            break;
                        case "Focus":
                            if (i == 0) { if (j == 0) { set1 = "F" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "F"; } }
                            if (i == 1) { if (j == 0) { set2 = "F" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "F"; } }
                            break;
                        case "Shield":
                            if (i == 0) { if (j == 0) { set1 = "Sh" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "Sh"; } }
                            if (i == 1) { if (j == 0) { set2 = "Sh" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "Sh"; } }
                            break;
                        case "Torch":
                            if (i == 0) { if (j == 0) { set1 = "T" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "T"; } }
                            if (i == 1) { if (j == 0) { set2 = "T" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "T"; } }
                            break;
                        case "Warhorn":
                            if (i == 0) { if (j == 0) { set1 = "Wh" + "," + set1.Split(',')[1]; } else if (j == 1) { set1 = set1.Split(',')[0] + "," + "Wh"; } }
                            if (i == 1) { if (j == 0) { set2 = "Wh" + "," + set2.Split(',')[1]; } else if (j == 1) { set2 = set2.Split(',')[0] + "," + "Wh"; } }
                            break;
                        default:
                            break;
                   
                    }
                }
            }

            strNew = set1 + "/" + set2;
            return strNew;
        }

        public ActionResult Statistics(string boss) {
            Sessionreset();

            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.GroupLogList = model.DbList.Where(y => y.BossName == boss).ToList();
            model.PlList = playerDB.PlayerLogs.Where(y => y.bossName == boss).ToList();
            if ((int)Session["TimeSpan"] > 0)
            {
                model.PlList = model.PlList.Where(x => Int32.Parse(model.DbList.FirstOrDefault(y => y.ID == x.logID).killDate.Substring(0, 8)) > Int32.Parse(DateTime.Now.AddDays(7 * -((int)Session["TimeSpan"])).ToString("yyyyMMdd"))).ToList();
                model.GroupLogList = model.GroupLogList.Where(x => Int32.Parse(x.killDate.Substring(0, 8)) > Int32.Parse(DateTime.Now.AddDays(7 * -((int)Session["TimeSpan"])).ToString("yyyyMMdd")));
            }
            if ((int)Session["PatchStart"] > 0)
            {
                model.PlList = model.PlList.Where(x => Int32.Parse(model.DbList.FirstOrDefault(y => y.ID == x.logID).killDate.Substring(0, 8)) >= (int)Session["PatchStart"]).ToList();
                model.GroupLogList = model.GroupLogList.Where(x => Int32.Parse(x.killDate.Substring(0, 8)) >= (int)Session["PatchStart"]).ToList();
            }
            if ((int)Session["PatchEnd"] > 0)
            {
                model.PlList = model.PlList.Where(x => Int32.Parse(model.DbList.FirstOrDefault(y => y.ID == x.logID).killDate.Substring(0, 8)) < (int)Session["PatchEnd"]).ToList();
                model.GroupLogList = model.GroupLogList.Where(x => Int32.Parse(x.killDate.Substring(0, 8)) < (int)Session["PatchEnd"]).ToList();
            }
            if ((int)Session["GroupID"] > 0)
            {
                model.PlList = model.PlList.Where(x => x.groupID == (int)Session["GroupID"]).ToList();
                //model.GroupLogList = model.GroupLogList.Where(x => Int32.Parse(x.killDate.Substring(0, 8)) < (int)Session["PatchEnd"]).ToList(); this is being used for group avg so dont effect whilst looking at group tab itse;lf
            }
            model.DPSPlayerList = model.PlList.Where(x => model.DbList.FirstOrDefault(z => z.ID == x.logID).Sucess == true).ToList();

            TempData["Boss"] = boss;
            return View(model);
        }

        public ActionResult Leaks()
        {
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
           // model.PlList = playerDB.PlayerLogs.ToList();
            return View(model);
        }

        public ActionResult GroupPerformance(int groupid)
        {
            Sessionreset();
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.GroupLogList = model.DbList.Where(x => x.groupID == groupid).ToList();
            model.PlList = playerDB.PlayerLogs.ToList().Where(x => x.groupID == groupid).ToList();
            List<String> BossList = model.PlList.Select(x => x.bossName).Distinct().ToList();
            List<PlayerLog> tempDPSList = new List<PlayerLog>();
            foreach (string boss in BossList) {
                tempDPSList.Add(model.PlList.Where(x =>x.bossName == boss).OrderByDescending(y =>y.bossDPS).First());
            }
            model.DPSPlayerList = tempDPSList;
            return View(model);
        }

        public ActionResult PlayerTable() {
            Sessionreset();
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            model.PlList = playerDB.PlayerLogs.ToList();//Can be made cleaner when profiles are made a thing
           
            return View(model);
        }

        public ActionResult PlayerPerformance(string acctname) {
            Sessionreset();
            ViewModel model = new ViewModel();
            model.DbList = db.RaidLogs.ToList();
            
            model.PlList = playerDB.PlayerLogs.Where(x=>x.accountName == acctname).ToList();//Can be made cleaner when profiles are made a thing
            model.GroupLogList =  new List<RaidLog>();
            List<RaidLog> tempRLList = new List<RaidLog>();
            foreach (var playentry in model.PlList) {
                tempRLList.Add(model.DbList.FirstOrDefault(x=>x.ID == playentry.logID));
            }
            model.GroupLogList = tempRLList;

            return View(model);
        }
        public void Sessionreset() {
            if (Session["PatchStart"] == null) { Session["PatchStart"] = 0; }
            if (Session["PatchEnd"] == null) { Session["PatchEnd"] = 0; }
            if (Session["TimeSpan"] == null) { Session["TimeSpan"] = 0; }
            if (Session["GroupID"] == null) { Session["GroupID"] = 0; }
        }
    }


}