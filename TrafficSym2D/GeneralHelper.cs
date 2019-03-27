﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TrafficSym2D
{
    public static class GeneralHelper
    {
        public static Game1 parent;
        public static Vector2 NormalizeVector(Vector2 vec)
        {
            if (vec.X < parent.elementSize2) vec.X = parent.elementSize2;
            if (vec.Y < parent.elementSize2) vec.Y = parent.elementSize2;
            if (vec.X > parent.resNotStaticX - parent.elementSize2) vec.X = parent.resNotStaticX - parent.elementSize2;
            if (vec.Y > parent.resNotStaticY - parent.elementSize2) vec.Y = parent.resNotStaticY - parent.elementSize2;

            return vec;
        }

        public static float NormalizeAngle(float angle)
        {
            while (angle > (float)(2.0 * Math.PI))
                angle -= (float)(2.0 * Math.PI);
            while (angle < 0f)
                angle += (float)(2.0 * Math.PI);

            return angle;
        }

        public static string settingsxml
        {
            get
            {
                return "<?xml version=\"1.0\" encoding=\"utf-8\" ?><TrafficSym2D><routeConfigs><config id=\"1\" timeBetweenCarsMs=\"9000\" comment=\"rightIn+upEnd\"><start x1=\"1020\" y1=\"94\" x2=\"1020\" y2=\"100\" direction=\"180\"/>      <end  x1=\"568\" y1=\"1\" x2=\"642\" y2=\"1\"/>      <wall  x1=\"961\" y1=\"136\" x2=\"836\" y2=\"94\"/>    </config>    <config id=\"2\" timeBetweenCarsMs=\"9000\" comment=\"rightIn+leftEnd\">      <start x1=\"1020\" y1=\"94\" x2=\"1020\" y2=\"100\" direction=\"180\"/>      <start x1=\"1020\" y1=\"120\" x2=\"1020\" y2=\"126\" direction=\"180\"/>      <end  x1=\"2\" y1=\"546\" x2=\"1\" y2=\"495\"/>      <wall  x1=\"924\" y1=\"77\" x2=\"825\" y2=\"93\"/>      <wall  x1=\"673\" y1=\"114\" x2=\"603\" y2=\"144\"/>      <wall  x1=\"685\" y1=\"166\" x2=\"491\" y2=\"292\"/>      <wall  x1=\"391\" y1=\"286\" x2=\"194\" y2=\"394\"/>      <wall  x1=\"445\" y1=\"323\" x2=\"296\" y2=\"407\"/>    </config>    <config id=\"2\" timeBetweenCarsMs=\"9000\" comment=\"rightIn+downEnd\">      <start x1=\"1020\" y1=\"94\" x2=\"1020\" y2=\"100\" direction=\"180\"/>      <start x1=\"1020\" y1=\"120\" x2=\"1020\" y2=\"126\" direction=\"180\"/>      <end  x1=\"439\" y1=\"766\" x2=\"490\" y2=\"765\"/>      <wall  x1=\"924\" y1=\"77\" x2=\"825\" y2=\"93\"/>      <wall  x1=\"673\" y1=\"114\" x2=\"603\" y2=\"144\"/>      <wall  x1=\"685\" y1=\"166\" x2=\"491\" y2=\"292\"/>      <wall  x1=\"446\" y1=\"322\" x2=\"380\" y2=\"386\"/>      <wall  x1=\"391\" y1=\"286\" x2=\"337\" y2=\"321\"/>      <wall  x1=\"337\" y1=\"321\" x2=\"296\" y2=\"406\"/>      <wall  x1=\"340\" y1=\"540\" x2=\"419\" y2=\"745\"/>      <wall  x1=\"408\" y1=\"521\" x2=\"494\" y2=\"760\"/>    </config>    <config id=\"3\" timeBetweenCarsMs=\"9000\" comment=\"rightIn+rightEnd\">      <start x1=\"1020\" y1=\"120\" x2=\"1020\" y2=\"126\" direction=\"180\"/>      <end x1=\"1022\" y1=\"144\" x2=\"1022\" y2=\"166\"/>      <wall  x1=\"832\" y1=\"95\" x2=\"968\" y2=\"80\"/>      <wall  x1=\"674\" y1=\"116\" x2=\"604\" y2=\"145\"/>      <wall  x1=\"685\" y1=\"166\" x2=\"620\" y2=\"203\"/>      <wall  x1=\"561\" y1=\"238\" x2=\"478\" y2=\"296\"/>      <wall  x1=\"390\" y1=\"287\" x2=\"379\" y2=\"386\"/>      <wall  x1=\"425\" y1=\"516\" x2=\"541\" y2=\"540\"/>      <wall  x1=\"624\" y1=\"497\" x2=\"736\" y2=\"382\"/>      <wall  x1=\"612\" y1=\"410\" x2=\"712\" y2=\"296\"/>    </config>    <config id=\"4\" timeBetweenCarsMs=\"9000\" comment=\"downIn+rightEnd\">      <start  x1=\"749\" y1=\"765\" x2=\"753\" y2=\"765\" direction=\"265\"/>      <end  x1=\"1021\" y1=\"165\" x2=\"1020\" y2=\"142\"/>      <wall  x1=\"670\" y1=\"485\" x2=\"716\" y2=\"289\"/>    </config>    <config id=\"5\" timeBetweenCarsMs=\"9000\" comment=\"downIn+upEnd\">      <start  x1=\"704\" y1=\"765\" x2=\"706\" y2=\"765\" direction=\"265\"/>     <start  x1=\"727\" y1=\"765\" x2=\"729\" y2=\"765\" direction=\"265\"/>      <start  x1=\"749\" y1=\"765\" x2=\"753\" y2=\"765\" direction=\"265\"/>      <end  x1=\"641\" y1=\"1\" x2=\"569\" y2=\"1\"/>      <wall  x1=\"733\" y1=\"424\" x2=\"713\" y2=\"291\"/>      <wall  x1=\"688\" y1=\"167\" x2=\"674\" y2=\"114\"/>      <wall  x1=\"660\" y1=\"70\" x2=\"648\" y2=\"6\"/>      <wall  x1=\"670\" y1=\"488\" x2=\"645\" y2=\"322\"/>      <wall  x1=\"621\" y1=\"204\" x2=\"602\" y2=\"141\"/>    </config>    <config id=\"6\" timeBetweenCarsMs=\"9000\" comment=\"downIn+leftEnd\">      <start  x1=\"658\" y1=\"762\" x2=\"662\" y2=\"762\" direction=\"265\"/>      <start  x1=\"680\" y1=\"762\" x2=\"683\" y2=\"762\" direction=\"265\"/>      <end  x1=\"1\" y1=\"546\" x2=\"1\" y2=\"496\"/>      <wall  x1=\"670\" y1=\"484\" x2=\"647\" y2=\"329\"/>      <wall  x1=\"624\" y1=\"496\" x2=\"612\" y2=\"406\"/>      <wall  x1=\"564\" y1=\"239\" x2=\"472\" y2=\"231\"/>      <wall  x1=\"390\" y1=\"287\" x2=\"353\" y2=\"310\"/>      <wall  x1=\"336\" y1=\"320\" x2=\"193\" y2=\"393\"/>      <wall  x1=\"447\" y1=\"321\" x2=\"296\" y2=\"405\"/>      <wall  x1=\"637\" y1=\"649\" x2=\"648\" y2=\"764\"/>    </config>    <config id=\"7\" timeBetweenCarsMs=\"9000\" comment=\"downIn+downEnd\">      <start x1=\"645\" y1=\"765\" x2=\"639\" y2=\"765\" direction=\"265\"/>      <end x1=\"489\" y1=\"766\" x2=\"439\" y2=\"766\"/>      <wall  x1=\"650\" y1=\"765\" x2=\"635\" y2=\"649\"/>      <wall x1=\"456\" y1=\"657\" x2=\"449\" y2=\"700\"/>      <wall x1=\"449\" y1=\"700\" x2=\"466\" y2=\"768\"/>    </config>    <config id=\"8\" timeBetweenCarsMs=\"9000\" comment=\"leftIn+downEnd\">      <start  x1=\"1\" y1=\"703\" x2=\"1\" y2=\"706\" direction=\"0\"/>      <start  x1=\"1\" y1=\"735\" x2=\"1\" y2=\"742\" direction=\"0\"/>      <end  x1=\"440\" y1=\"764\" x2=\"485\" y2=\"762\"/>      <wall  x1=\"116\" y1=\"670\" x2=\"1\" y2=\"694\"/>      <wall x1=\"381\" y1=\"645\" x2=\"442\" y2=\"682\"/>      <wall x1=\"442\" y1=\"682\" x2=\"474\" y2=\"768\"/>    </config>    <config id=\"9\" timeBetweenCarsMs=\"9000\" comment=\"leftIn+rightEnd\">      <start  x1=\"1\" y1=\"650\" x2=\"1\" y2=\"655\" direction=\"0\"/>      <start  x1=\"1\" y1=\"675\" x2=\"1\" y2=\"683\" direction=\"0\"/>      <end  x1=\"1022\" y1=\"166\" x2=\"1022\" y2=\"140\"/>      <wall  x1=\"341\" y1=\"539\" x2=\"407\" y2=\"519\"/>      <wall  x1=\"424\" y1=\"516\" x2=\"534\" y2=\"472\"/>      <wall  x1=\"611\" y1=\"410\" x2=\"712\" y2=\"291\"/>      <wall  x1=\"0\" y1=\"698\" x2=\"115\" y2=\"668\"/>      <wall  x1=\"362\" y1=\"592\" x2=\"430\" y2=\"571\"/>      <wall  x1=\"624\" y1=\"498\" x2=\"736\" y2=\"382\"/>    </config>    <config id=\"10\" timeBetweenCarsMs=\"9000\" comment=\"leftIn+upEnd\">      <start  x1=\"1\" y1=\"650\" x2=\"1\" y2=\"655\" direction=\"0\"/>      <start  x1=\"1\" y1=\"675\" x2=\"1\" y2=\"683\" direction=\"0\"/>      <end  x1=\"568\" y1=\"0\" x2=\"641\" y2=\"0\"/>      <wall  x1=\"115\" y1=\"669\" x2=\"-1\" y2=\"694\"/>      <wall  x1=\"361\" y1=\"592\" x2=\"430\" y2=\"572\"/>      <wall  x1=\"624\" y1=\"497\" x2=\"737\" y2=\"380\"/>      <wall  x1=\"737\" y1=\"380\" x2=\"713\" y2=\"293\"/>      <wall  x1=\"686\" y1=\"165\" x2=\"673\" y2=\"113\"/>      <wall  x1=\"659\" y1=\"69\" x2=\"648\" y2=\"8\"/>      <wall  x1=\"339\" y1=\"539\" x2=\"408\" y2=\"519\"/>      <wall  x1=\"425\" y1=\"516\" x2=\"533\" y2=\"473\"/>      <wall  x1=\"611\" y1=\"409\" x2=\"647\" y2=\"331\"/>      <wall  x1=\"621\" y1=\"204\" x2=\"604\" y2=\"144\"/>    </config>    <config id=\"11\" timeBetweenCarsMs=\"9000\" comment=\"leftIn+leftEnd\">      <start  x1=\"1\" y1=\"650\" x2=\"1\" y2=\"655\" direction=\"0\"/>      <start  x1=\"1\" y1=\"675\" x2=\"1\" y2=\"683\" direction=\"0\"/>      <end  x1=\"1\" y1=\"545\" x2=\"1\" y2=\"495\"/>      <wall  x1=\"118\" y1=\"670\" x2=\"1\" y2=\"695\"/>      <wall  x1=\"362\" y1=\"592\" x2=\"431\" y2=\"572\"/>      <wall  x1=\"624\" y1=\"498\" x2=\"647\" y2=\"329\"/>      <wall  x1=\"563\" y1=\"238\" x2=\"463\" y2=\"237\"/>      <wall  x1=\"390\" y1=\"285\" x2=\"352\" y2=\"311\"/>      <wall  x1=\"336\" y1=\"318\" x2=\"194\" y2=\"391\"/>      <wall  x1=\"339\" y1=\"538\" x2=\"410\" y2=\"519\"/>      <wall  x1=\"424\" y1=\"515\" x2=\"536\" y2=\"471\"/>      <wall  x1=\"434\" y1=\"338\" x2=\"295\" y2=\"406\"/>    </config>    <config id=\"12\" timeBetweenCarsMs=\"9000\" comment=\"upIn+leftEnd\">      <start x1=\"134\" y1=\"2\" x2=\"137\" y2=\"2\" direction=\"75\"/>      <start  x1=\"159\" y1=\"3\" x2=\"164\" y2=\"3\" direction=\"75\"/>      <end  x1=\"1\" y1=\"495\" x2=\"1\" y2=\"543\"/>      <wall x1=\"220\" y1=\"1\" x2=\"265\" y2=\"301\"/>      <wall x1=\"265\" y1=\"301\" x2=\"295\" y2=\"407\"/>    </config>    <config id=\"13\" timeBetweenCarsMs=\"9000\" comment=\"upIn+downEnd\">      <start x1=\"158\" y1=\"2\" x2=\"163\" y2=\"2\" direction=\"75\"/>      <start x1=\"182\" y1=\"3\" x2=\"189\" y2=\"3\" direction=\"75\"/>      <start x1=\"203\" y1=\"3\" x2=\"212\" y2=\"3\" direction=\"75\"/>      <end x1=\"438\" y1=\"765\" x2=\"489\" y2=\"764\"/>      <wall x1=\"141\" y1=\"0\" x2=\"296\" y2=\"406\"/>      <wall x1=\"339\" y1=\"538\" x2=\"361\" y2=\"593\"/>      <wall x1=\"378\" y1=\"642\" x2=\"418\" y2=\"744\"/>      <wall x1=\"336\" y1=\"320\" x2=\"372\" y2=\"419\"/>      <wall x1=\"408\" y1=\"519\" x2=\"430\" y2=\"572\"/>      <wall x1=\"455\" y1=\"656\" x2=\"490\" y2=\"744\"/>    </config>    <config id=\"14\" timeBetweenCarsMs=\"9000\" comment=\"upIn+rightEnd\">      <start x1=\"242\" y1=\"0\" x2=\"248\" y2=\"0\" direction=\"75\"/>      <start x1=\"265\" y1=\"1\" x2=\"270\" y2=\"1\" direction=\"75\"/>      <end x1=\"1020\" y1=\"144\" x2=\"1020\" y2=\"166\"/>      <wall x1=\"353\" y1=\"311\" x2=\"379\" y2=\"387\"/>      <wall x1=\"425\" y1=\"516\" x2=\"520\" y2=\"546\"/>      <wall x1=\"624\" y1=\"498\" x2=\"736\" y2=\"380\"/>      <wall x1=\"391\" y1=\"286\" x2=\"429\" y2=\"369\"/>      <wall x1=\"611\" y1=\"410\" x2=\"713\" y2=\"293\"/>    </config>    <config id=\"15\" timeBetweenCarsMs=\"9000\" comment=\"upIn+upEnd\">      <start x1=\"241\" y1=\"1\" x2=\"246\" y2=\"1\" direction=\"75\"/>      <start x1=\"266\" y1=\"2\" x2=\"272\" y2=\"2\" direction=\"75\"/>      <end x1=\"569\" y1=\"1\" x2=\"639\" y2=\"1\"/>      <wall x1=\"353\" y1=\"310\" x2=\"381\" y2=\"390\"/>      <wall x1=\"425\" y1=\"514\" x2=\"534\" y2=\"541\"/>      <wall x1=\"622\" y1=\"498\" x2=\"736\" y2=\"380\"/>      <wall x1=\"736\" y1=\"380\" x2=\"714\" y2=\"295\"/>      <wall x1=\"686\" y1=\"165\" x2=\"674\" y2=\"114\"/>      <wall x1=\"659\" y1=\"70\" x2=\"650\" y2=\"10\"/>      <wall x1=\"390\" y1=\"285\" x2=\"428\" y2=\"371\"/>      <wall x1=\"610\" y1=\"411\" x2=\"645\" y2=\"326\"/>      <wall x1=\"620\" y1=\"204\" x2=\"602\" y2=\"145\"/>    </config>  </routeConfigs>  <lightConfig>    <lights>      <light x1=\"805\" y1=\"144\" x2=\"789\" y2=\"47\" comment=\"rightIn\"/>      <light x1=\"679\" y1=\"590\" x2=\"753\" y2=\"593\" comment=\"downIn\"/>      <light x1=\"259\" y1=\"624\" x2=\"235\" y2=\"569\" comment=\"leftIn\"/>      <light x1=\"257\" y1=\"686\" x2=\"231\" y2=\"634\" comment=\"leftIn turnRight\"/>      <light x1=\"194\" y1=\"198\" x2=\"351\" y2=\"161\" comment=\"upIn\"/>      <light x1=\"618\" y1=\"204\" x2=\"696\" y2=\"195\" comment=\"roundaboutRight\"/>      <light x1=\"625\" y1=\"497\" x2=\"576\" y2=\"439\" comment=\"roundaboutDown\"/>      <light x1=\"425\" y1=\"513\" x2=\"530\" y2=\"472\" comment=\"roundaboutLeft\"/>      <light x1=\"334\" y1=\"536\" x2=\"410\" y2=\"515\" comment=\"roundaboutLeft2\"/>      <light x1=\"429\" y1=\"370\" x2=\"390\" y2=\"284\" comment=\"roundaboutUp\"/>      <light x1=\"575\" y1=\"681\" x2=\"601\" y2=\"631\" comment=\"downTurnAround\"/>      <light x1=\"625\" y1=\"585\" x2=\"679\" y2=\"589\" comment=\"downIn turnLeft\"/>    </lights>    <configs>      <config timeToWaitMs=\"15000\" comment=\"VerticalFlow\">        <light id=\"1\"/>        <light id=\"4\"/>        <light id=\"5\"/>        <light id=\"7\"/>        <light id=\"8\"/>      </config>      <config timeToWaitMs=\"5000\" comment=\"VerticalFlow+downTurnLeft\">        <light id=\"1\"/>        <light id=\"4\"/>        <light id=\"5\"/>        <light id=\"7\"/>        <light id=\"8\"/>        <light id=\"11\"/>      </config>      <config timeToWaitMs=\"5000\" comment=\"VerticalFlowRelax\">        <light id=\"5\"/>        <light id=\"7\"/>        <light id=\"8\"/>      </config>      <config timeToWaitMs=\"20000\" comment=\"HorizonalFlow\">        <light id=\"0\"/>        <light id=\"2\"/>        <light id=\"3\"/>        <light id=\"6\"/>        <light id=\"9\"/>        <light id=\"10\"/>      </config>      <config timeToWaitMs=\"5000\" comment=\"HorizonalFlowRelax\">        <light id=\"6\"/>        <light id=\"9\"/>      </config>    </configs>  </lightConfig></TrafficSym2D>";
            }
        }
    }
}