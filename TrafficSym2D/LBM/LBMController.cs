using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TrafficSym2D.Enums;
using System.Threading.Tasks;

namespace TrafficSym2D.LBM
{
    public class LBMController
    {
        private readonly float FLOW_MAX = 0.05f;
        private readonly float COEF = 0.24f;

        private static Random random = new Random();
        public static int maxParticleDensity = 1;

        private int countX;
        private int countY;
        private int elementSize;

        private float[,] fx;
        private float[,] fy;

        public LBMController(int countX, int countY, int elementSize)
        {
            this.countX = countX;
            this.countY = countY;
            this.elementSize = elementSize;

            fx = new float[countX, countY];
            fy = new float[countX, countY];
        }

        public void Update(LBMElement[,] tabLBM)
        {
            Array.Clear(fx, 0, fx.Length);
            Array.Clear(fy, 0, fy.Length);

            //LBM flow
            Parallel.For(0, (countY - 1) * (countX - 1), i =>
            {
                int y = i / (countX - 1);
                int x = i % (countX - 1);

                if (!tabLBM[x, y].isWall)
                {
                    if (!tabLBM[x + 1, y].isWall)
                        fx[x, y] = MathHelper.Clamp(COEF * (tabLBM[x, y].density - tabLBM[x + 1, y].density), -FLOW_MAX, FLOW_MAX);
                    else
                        fx[x, y] = 0f;

                    if (!tabLBM[x, y + 1].isWall)
                        fy[x, y] = MathHelper.Clamp(COEF * (tabLBM[x, y].density - tabLBM[x, y + 1].density), -FLOW_MAX, FLOW_MAX);
                    else
                        fy[x, y] = 0f;
                }
                else
                {
                    fx[x, y] = 0f;
                    fy[x, y] = 0f;
                }
            });

            //LBM density
            Parallel.For(0, (countY) * (countX), i =>
            {
                int y = i / (countX);
                int x = i % (countX);

                if (!tabLBM[x, y].isWall)
                {
                    float dens = tabLBM[x, y].density;

                    if (x > 0) dens += fx[x - 1, y];
                    if (y > 0) dens += fy[x, y - 1];

                    dens -= fx[x, y];
                    dens -= fy[x, y];

                    tabLBM[x, y].density = MathHelper.Clamp(dens, -10f, 10f);
                }
            });

            //Graphical LBM flow
            Parallel.For(0, (countY - 1) * (countX - 1), i =>
            {
                int y = i / (countX - 1);
                int x = i % (countX - 1);

                if (!tabLBM[x, y].isWall)
                {
                    float vx = 0f;
                    float vy = 0f;
                    if (!tabLBM[x - 1, y].isWall) vx += (tabLBM[x - 1, y].density - tabLBM[x, y].density);
                    if (!tabLBM[x + 1, y].isWall) vx += (tabLBM[x, y].density - tabLBM[x + 1, y].density);
                    tabLBM[x, y].x = MathHelper.Clamp(vx * 100f, ((float)-elementSize), ((float)elementSize));
                    if (!tabLBM[x, y - 1].isWall) vy += (tabLBM[x, y - 1].density - tabLBM[x, y].density);
                    if (!tabLBM[x, y + 1].isWall) vy += (tabLBM[x, y].density - tabLBM[x, y + 1].density);
                    tabLBM[x, y].y = MathHelper.Clamp(vy * 100f, ((float)-elementSize), ((float)elementSize));
                }
            });
        }

        /// <summary>
        /// Returns true if all starting points from routeConfig have a road cell with big enough vector in direct neighbourhood
        /// </summary>
        public bool HasRouteVectorMapGenerated(LBMElement[,] tabLBM, RouteConfig routeConfig)
        {
            foreach(var rs in routeConfig.routeStart)
            {
                if (
                    !HasFullyPropagatedLBMCellNear(tabLBM, rs.x1 / elementSize, rs.y1 / elementSize) ||
                    !HasFullyPropagatedLBMCellNear(tabLBM, rs.x2 / elementSize, rs.y2 / elementSize)
                    )
                    return false;
            }

            return true;
        }

        private bool HasFullyPropagatedLBMCellNear(LBMElement[,] tabLBM, int checkX, int checkY)
        {
            bool normalCellEncountered = false;
            for(int x = Math.Max(0,checkX-1); x<=Math.Min(countX-1,checkX+1); x++)
                for(int y = Math.Max(0,checkY-1); y<=Math.Min(countY-1,checkY+1); y++)
                {
                    normalCellEncountered |= tabLBM[x, y].isNormal;
                    if(
                        tabLBM[x,y].isNormal &&
                        Math.Sqrt((tabLBM[x,y].x * tabLBM[x,y].x) + (tabLBM[x,y].y * tabLBM[x,y].y))>0.5
                        )
                    return true;
                }
            return !normalCellEncountered;
        }


        public static bool isWall(byte b)
        {
            return ((b & 1) == 1);
        }

        public static byte DirectionPower(byte direction)
        {
            if (direction > 6 || direction < 1) throw new Exception("wtf@DirectionPower");
            //return Convert.ToByte(Math.Pow(2.0, Convert.ToDouble(direction)));
            switch (direction)
            {
                case 1: return 2;
                case 2: return 4;
                case 3: return 8;
                case 4: return 16;
                case 5: return 32;
                case 6: return 64;
            }
            return 0;
        }

        public static byte RandomDirection()
        {
            return DirectionPower((byte)random.Next(1, 7));
        }

        public static byte GetByteFromDir(byte[,] tab, int x, int y, byte dir)
        {
            if (dir == 1) return tab[x + 1, y];
            if (dir == 4) return tab[x - 1, y];

            if ((y & 1) == 0)//parzyste
            {
                if (dir == 2) return tab[x + 1, y + 1];
                if (dir == 3) return tab[x, y + 1];
                if (dir == 5) return tab[x, y - 1];
                if (dir == 6) return tab[x + 1, y - 1];
            }
            else //nieparzyste
            {
                if (dir == 2) return tab[x, y + 1];
                if (dir == 3) return tab[x - 1, y + 1];
                if (dir == 5) return tab[x - 1, y - 1];
                if (dir == 6) return tab[x, y - 1];
            }
            return 0;
        }

        public static void AddByteFromDir(byte[,] tab, int x, int y, byte dir, byte value)
        {
            if (dir == 1) tab[x + 1, y] += value;
            if (dir == 4) tab[x - 1, y] += value;

            if ((y & 1) == 0)//parzyste
            {
                if (dir == 2) tab[x + 1, y + 1] += value;
                if (dir == 3) tab[x, y + 1] += value;
                if (dir == 5) tab[x, y - 1] += value;
                if (dir == 6) tab[x + 1, y - 1] += value;
            }
            else //nieparzyste
            {
                if (dir == 2) tab[x, y + 1] += value;
                if (dir == 3) tab[x - 1, y + 1] += value;
                if (dir == 5) tab[x - 1, y - 1] += value;
                if (dir == 6) tab[x, y - 1] += value;
            }
        }

        public static List<byte> GetDirections(byte b)
        {
            List<byte> l = new List<byte>();
            if (b > 1)
                for (byte i = 1; i <= 6; i++ )
                {
                    byte dirpow = DirectionPower(i);
                    if ((b & dirpow) == dirpow)
                        l.Add(i);
                }
            return l;
        }

        public static byte negDirection(byte b)
        {
            switch (b)
            {
                case 1: return 4;
                case 2: return 5;
                case 3: return 6;
                case 4: return 1;
                case 5: return 2;
                case 6: return 3;
            }
            throw new Exception("wtf@negDirection");
            return 0;
        }

        public static int getParticleSum(byte b)
        {
            int sum = 0;
            if ((b & 2) == 2) sum++;
            if ((b & 4) == 4) sum++;
            if ((b & 8) == 8) sum++;
            if ((b & 16) == 16) sum++;
            if ((b & 32) == 32) sum++;
            if ((b & 64) == 64) sum++;
            return sum;
        }

        public static Color getParticleColor(byte b)
        {
            int sum = getParticleSum(b);

            switch (sum)
            {
                case 1: return new Color(128, 128, 128);
                case 2: return new Color(141, 141, 141);
                case 3: return new Color(170, 170, 170);
                case 4: return new Color(191, 191, 191);
                case 5: return new Color(212, 212, 212);
                case 6: return Color.White;
            }
            return Color.Black;
        }

        public static void DrawLineLBM(LBMElement[,] tabLBM, int elementSize, int x1, int y1, int x2, int y2, LBMNodeType nodeType)
        {
            float xDiff = (x2 - x1);
            float yDiff = (y2 - y1);

            float x = x1;
            float y = y1;

            float minSteps = Math.Max(Math.Abs(xDiff), Math.Abs(yDiff));
            minSteps = minSteps == 0 ? 1 : minSteps;

            for(int i=0;i<=minSteps;i++)
            {
                x+=xDiff/minSteps;
                y+=yDiff/minSteps;
                tabLBM[(int)Math.Round(x/elementSize),(int)Math.Round(y/elementSize)].nodeType=nodeType;
            }
        }
    }
}
