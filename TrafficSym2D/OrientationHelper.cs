using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace TrafficSym2D
{
    public static class OrientationHelper
    {
        /// <summary>
        /// zwraca orientacje 3 punktow.
        /// jak jest >0 to wierzcholki "skrecaja w lewo"
        /// jak jest &lt;0 to wierzcholki "skrecaja w prawo"
        /// </summary>
        public static float Orientation(Vector2 p, Vector2 q, Vector2 r)
        {
            return ((p.X * q.Y + p.Y * r.X + q.X * r.Y) - (r.X * q.Y + r.Y * p.X + q.X * p.Y));
        }

        public static bool Intersection(Vector2 p, Vector2 q, Vector2 r, Vector2 s)
        {
            //odcinek pq to jest nasz sprawdzajacy
            double o1 = Orientation(p, q, r);
            double o2 = Orientation(p, q, s);
            double o3 = Orientation(r, s, p);
            double o4 = Orientation(r, s, q);

            if ((o1 != 0) && (o2 != 0) && (o3 != 0) && (o4 != 0))
            {
                if ((Math.Sign(o1) != Math.Sign(o2)) && (Math.Sign(o3) != Math.Sign(o4)))
                    return true;//przecinają się
                else
                    return false;//nieprzecinaja sie
            }
            return false;
            //else // sprawdzamy czy p lezy na odcinku czy nie
            //{
            //    if (o1 == 0) //jeden z kawalkow lezy na sprawdzajacej...
            //    {
            //        if (p.X < r.X) return false;
            //        if (s.Y < p.Y)
            //        {
            //            return true;
            //        }
            //        else if (s.Y == p.Y) //caly odcinek lezy na sprawdzajacej
            //        {
            //            return false;
            //        }
            //        else
            //        {
            //            return false;
            //        }
            //    }
            //    else if (o2 == 0) //drugi z kawalkow lezy na sprawdzajacej...
            //    {
            //        if (p.X < s.X) return false;
            //        if (r.Y < p.Y)
            //        {
            //            return true;
            //        }
            //        else if (r.Y == p.Y) //caly odcinek lezy na sprawdzajacej
            //        {
            //            return false;
            //        }
            //        else
            //        {
            //            return false;
            //        }
            //    }
            //    else if (o3 == 0) //punkt poczatkowy sprawdzajacej lezy na prostej odcinka
            //    {
            //        bool nalezy = false;
            //        //sprawdzam czy p nalezy do odcinka rs
            //        if (r.X > s.X)
            //        {
            //            if ((s.X < p.X) && (p.X < r.X)) nalezy = true;
            //        }
            //        else if (r.X < s.X)
            //            if ((s.X > p.X) && (p.X > r.X)) nalezy = true;

            //        if (nalezy)
            //        {
            //            if (r.Y > s.Y)
            //            {
            //                if ((s.Y < p.Y) && (p.Y < r.Y))
            //                {
            //                    return true;
            //                }
            //            }
            //            else if (r.Y < s.Y)
            //            {
            //                if ((s.Y > p.Y) && (p.Y > r.Y))
            //                {
            //                    return true;
            //                }
            //            }
            //        }
            //        else
            //            return false;

            //        throw new Exception("cos nie sprawdzone...");
            //    } //o4 nas nie obchodzi, zawsze bedzie poza odcinkiem

            //    return false;
            //}
        }
    }
}
