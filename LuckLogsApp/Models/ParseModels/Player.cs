using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckLogsApp.Models.ParseModels
{
    public class Player
    {
        // Fields
        private int instid;
        private String account;
        private String character;
        private String group;
        private String prof;
        private int toughness;
        private int healing;
        private int condition;
        private List<DamageLog> damage_logs = new List<DamageLog>();
        private List<BoonMap> boon_map = new List<BoonMap>();

        // Constructors
        public Player(AgentItem agent)
        {
            this.instid = agent.getInstid();
            String[] name = agent.getName().Split('\0');
            this.character = name[0];
            this.account = name[1];
            this.group = name[2];
            this.prof = agent.getProf();
            this.toughness = agent.getToughness();
            this.healing = agent.getHealing();
            this.condition = agent.getCondition();
        }

        // Getters
        public int getInstid()
        {
            return instid;
        }

        public String getAccount()
        {
            return account;
        }

        public String getCharacter()
        {
            return character;
        }

        public String getGroup()
        {
            return group;
        }

        public String getProf()
        {
            return prof;
        }

        public int getToughness()
        {
            return toughness;
        }

        public int getHealing()
        {
            return healing;
        }

        public int getCondition()
        {
            return condition;
        }

        public List<DamageLog> getDamageLogs(BossData bossData, List<CombatItem> combatList)
        {
            if (damage_logs.Count == 0)
            {
                setDamageLogs(bossData, combatList);
            }
            return damage_logs;
        }

        public List<BoonMap> getBoonMap(BossData bossData, SkillData skillData, List<CombatItem> combatList)
        {
            if (boon_map.Count == 0)
            {
                setBoonMap(bossData, skillData, combatList);
            }
            return boon_map;
        }

        // Private Methods
        private void setDamageLogs(BossData bossData, List<CombatItem> combatList)
        {
            int time_start = bossData.getFirstAware();
            foreach (CombatItem c in combatList)
            {
                if (instid == c.getSrcInstid() || instid == c.getSrcMasterInstid())
                {
                    LuckLogsApp.Models.ParseEnums.StateChange state = c.isStateChange();
                    int time = c.getTime() - time_start;
                    if (bossData.getInstid() == c.getDstInstid() && c.getIFF().getEnum() == "FOE")
                    {
                        if (state.getEnum() == "NORMAL")
                        {
                            if (c.isBuff() == 1 && c.getBuffDmg() != 0)
                            {
                                damage_logs.Add(new DamageLog(time, c.getBuffDmg(), c.getSkillID(), c.isBuff(),
                                        c.getResult(), c.isNinety(), c.isMoving(), c.isFlanking()));
                            }
                            else if (c.isBuff() == 0 && c.getValue() != 0)
                            {
                                damage_logs.Add(new DamageLog(time, c.getValue(), c.getSkillID(), c.isBuff(),
                                        c.getResult(), c.isNinety(), c.isMoving(), c.isFlanking()));
                            }
                        }
                    }
                }
            }
        }

        public void setBoonMap(BossData bossData, SkillData skillData, List<CombatItem> combatList)
        {

            // Initialize Boon Map with every Boon
            foreach (Boon boon in Boon.getList())
            {
                BoonMap map = new BoonMap(boon.getName(), new List<BoonLog>());
                boon_map.Add(map);
               // boon_map.put(boon.getName(), new ArrayList<BoonLog>());
            }

            // Fill in Boon Map
            int time_start = bossData.getFirstAware();
            int fight_duration = bossData.getLastAware() - time_start;
            foreach (CombatItem c in combatList)
            {
                if (instid == c.getDstInstid())
                {
                    String skill_name = skillData.getName(c.getSkillID());
                    if (c.isBuff() == 1 && c.getValue() > 0)
                    {
                        int count = 0;
                        foreach (BoonMap bm in boon_map)
                        {
                            if (bm.getName() == skill_name)
                            {
                                int time = c.getTime() - time_start;
                                if (time < fight_duration)
                                {
                                    List<BoonLog> loglist = bm.getBoonLog();
                                    loglist.Add(new BoonLog(time, c.getValue()));
                                    bm.setBoonLog(loglist);
                                    boon_map[count] = bm;
                                }
                                else
                                {
                                    break;
                                }

                            }
                            count++;
                        }
                    }
                }
            }

        }
    }
}