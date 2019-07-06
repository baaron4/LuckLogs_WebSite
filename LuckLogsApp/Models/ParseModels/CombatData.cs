using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckLogsApp.Models.ParseModels
{
    public class CombatData
    {
        // Fields
        private List<CombatItem> combat_list;

        // Constructors
        public CombatData()
        {
            this.combat_list = new List<CombatItem>();
        }

        // Public Methods
        public void addItem(CombatItem item)
        {
            combat_list.Add(item);
        }

        //public List<Point> getStates(int src_instid, StateChange change)
        //{
        //    List<Point> states = new ArrayList<Point>();
        //    for (CombatItem c : combat_list)
        //    {
        //        if (c.getSrcInstid() == src_instid && c.isStateChange().equals(change))
        //        {
        //            states.add(new Point(c.getTime(), (int)c.getDstAgent()));
        //        }
        //    }
        //    return states;
        //}

        public int getSkillCount(int src_instid, int skill_id)
        {
            int count = 0;
            foreach (CombatItem c in combat_list)
            {
                if (c.getSrcInstid() == src_instid && c.getSkillID() == skill_id)
                {
                    count++;
                }
            }
            return count;
        }

        // Getters
        public List<CombatItem> getCombatList()
        {
            return combat_list;
        }
    }
}