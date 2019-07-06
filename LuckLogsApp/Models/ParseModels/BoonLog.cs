using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuckLogsApp.Models.ParseModels
{
    public class BoonLog
    {
        // Fields
        private int time = 0;
        private int value = 0;

        // Constructor
        public BoonLog(int time, int value)
        {
            this.time = time;
            this.value = value;
        }

        // Getters
        public int getTime()
        {
            return time;
        }

        public int getValue()
        {
            return value;
        }
    }
}