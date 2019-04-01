using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TrafficSym2D
{
    public static class GeneralHelper
    {
        public static TrafficSymGame parent;
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
    }
}
