﻿using System;
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
            if (vec.X < parent.elementSize) vec.X = parent.elementSize;
            if (vec.Y < parent.elementSize) vec.Y = parent.elementSize;
            if (vec.X > parent.resX - parent.elementSize) vec.X = parent.resX - parent.elementSize;
            if (vec.Y > parent.resY - parent.elementSize) vec.Y = parent.resY - parent.elementSize;

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

        public static float NormalizeAngleSteering(float angle)
        {
            while (angle > (float)(Math.PI))
                angle -= (float)(2.0 * Math.PI);
            while (angle < -(float)(Math.PI))
                angle += (float)(2.0 * Math.PI);

            return angle;
        }

        public static float Min(params float[] values)
        {
            return Enumerable.Min(values);
        }

        /// <summary>
        /// Random with normal distribution
        /// https://stackoverflow.com/questions/18807812/adding-an-average-parameter-to-nets-random-next-to-curve-results
        /// </summary>
        public static double NextDoubleNormalDist(this Random rand, double min=0.0, double max=1.0, int tightness=10)
        {
            double total = 0.0;
            for (int i = 1; i <= tightness; i++)
            {
                total += rand.NextDouble();
            }

            return ((total / tightness) * (max - min)) + min;
        }
    }
}
