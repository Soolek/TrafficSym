using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrafficSym2D
{
    public class RouteStart
    {
        public int x1, x2, y1, y2;
        public float directionDeg;
        public RouteStart(int x1, int y1, int x2, int y2, float dir)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
            this.directionDeg = dir;
        }
    }

    public class RouteEnd
    {
        public int x1, x2, y1, y2;
        public RouteEnd(int x1, int y1, int x2, int y2)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
        }
    }

    public class RouteWall
    {
        public int x1, x2, y1, y2;
        public RouteWall(int x1, int y1, int x2, int y2)
        {
            this.x1 = x1;
            this.x2 = x2;
            this.y1 = y1;
            this.y2 = y2;
        }
    }

    public class RouteConfig
    {
        public int timeBetweenCarsMs;
        public float initialSpeed;
        public TimeSpan lastCarOutTime;
        public List<RouteStart> routeStart = new List<RouteStart>();
        public List<RouteEnd> routeEnd = new List<RouteEnd>();
        public List<RouteWall> routeWall = new List<RouteWall>();
    }
}
