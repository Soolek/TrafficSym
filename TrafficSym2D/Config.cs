﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrafficSym2D
{
    public static class Config
    {
        public static string ConfigDir = "default";
        public static bool GiveWayToRight = false;
        public static int EndAfterCarsSpawned = -1;

        public static void SetParameters(Dictionary<string, string> arguments)
        {
            string temp;
            if (arguments.TryGetValue("configdir", out temp))
            {
                ConfigDir = temp;
            }
            if(arguments.TryGetValue("givewaytoright", out temp))
            {
                GiveWayToRight = bool.Parse(temp);
            }
            if (arguments.TryGetValue("endaftercarsspawned", out temp))
            {
                EndAfterCarsSpawned = int.Parse(temp);
            }
        }
    }
}
