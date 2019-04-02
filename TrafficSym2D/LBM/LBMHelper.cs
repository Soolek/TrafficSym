using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TrafficSym2D.Enums;

namespace TrafficSym2D.LBM
{
    public static class LBMHelper
    {
        public static Random r = new Random();
        public static TrafficSymGame parent;
        public static int maxParticleDensity = 1;

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
            return DirectionPower((byte)r.Next(1, 7));
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
