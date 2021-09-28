using System.Collections.Generic;
using UnityEngine;

public static class Geometry
{
    public static IList<Vector2> ComputeConvexHull(List<Vector2> points, bool sortInPlace = false)
    {
        if (!sortInPlace)
            points = new List<Vector2>(points);
        points.Sort((a, b) =>
            a.x == b.x ? a.y.CompareTo(b.y) : (a.x > b.x ? 1 : -1));

        // Importantly, DList provides O(1) insertion at beginning and end
        CircularList<Vector2> hull = new CircularList<Vector2>();
        int L = 0, U = 0; // size of lower and upper hulls

        // Builds a hull such that the output polygon starts at the leftmost Vector2.
        for (int i = points.Count - 1; i >= 0; i--)
        {
            Vector2 p = points[i], p1;

            // build lower hull (at end of output list)
            while (L >= 2 && (p1 = hull.Last).Sub(hull[hull.Count - 2]).Cross(p.Sub(p1)) >= 0)
            {
                hull.PopLast();
                L--;
            }
            hull.PushLast(p);
            L++;

            // build upper hull (at beginning of output list)
            while (U >= 2 && (p1 = hull.First).Sub(hull[1]).Cross(p.Sub(p1)) <= 0)
            {
                hull.PopFirst();
                U--;
            }
            if (U != 0) // when U=0, share the Vector2 added above
                hull.PushFirst(p);
            U++;
            Debug.Assert(U + L == hull.Count + 1);
        }
        hull.PopLast();
        return hull;
    }

    private static Vector2 Sub(this Vector2 a, Vector2 b)
    {
        return a - b;
    }

    private static float Cross(this Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    private class CircularList<T> : List<T>
    {
        public T Last
        {
            get
            {
                return this[this.Count - 1];
            }
            set
            {
                this[this.Count - 1] = value;
            }
        }

        public T First
        {
            get
            {
                return this[0];
            }
            set
            {
                this[0] = value;
            }
        }

        public void PushLast(T obj)
        {
            this.Add(obj);
        }

        public T PopLast()
        {
            T retVal = this[this.Count - 1];
            this.RemoveAt(this.Count - 1);
            return retVal;
        }

        public void PushFirst(T obj)
        {
            this.Insert(0, obj);
        }

        public T PopFirst()
        {
            T retVal = this[0];
            this.RemoveAt(0);
            return retVal;
        }
    }

    public static bool PointOnLine(Vector2 p, Vector2 p1, Vector2 p2)
    {
        if (p2.x == p1.x)
            return p.x == p1.x;
        else if (p2.y == p1.y)
            return p.y == p1.y;

        return ((p.x - p1.x) / (p2.x - p1.x)) == ((p.y - p1.y) / (p2.y - p1.y));
    }

    public static List<Vector2> BorderPoints(List<Vector2> points2d, IList<Vector2> cornerPoints)
    {
        List<Vector2> borderPoints = new List<Vector2>();
        foreach (Vector2 p in points2d)
        {
            //Debug.Log("Geometry: " + p);
            for (int i = 0; i < cornerPoints.Count; ++i)
            {
                Vector2 p1 = cornerPoints[i];
                Vector2 p2 = cornerPoints[(i + 1) % cornerPoints.Count];
                if (PointOnLine(p, p1, p2))
                {
                    //Debug.Log("point on line");
                    borderPoints.Add(p);
                    break;
                }
                else
                {
                    //Debug.Log("point not on line");
                }
            }
        }
        return borderPoints;
    }

    public static float[] DistanceAndRotationFromBorder(Vector2 p, List<Vector2> borderPoints)
    {
        float[] distance_rotation = new float[2] { 100, 0 };
        foreach (Vector2 borderP in borderPoints)
        {
            float dist = Mathf.Sqrt(Mathf.Pow(borderP.x - p.x, 2) + Mathf.Pow(borderP.y - p.y, 2));
            if(dist < distance_rotation[0])
            {
                distance_rotation[0] = dist;
                Vector2 directionVec = new Vector2(p.x - borderP.x, p.y - borderP.y);
                float angle = Mathf.Atan2(directionVec.y, directionVec.x);
                //Debug.Log("UUtils:" + p.ToString() + " " + borderP.ToString() + " " + directionVec.ToString() + " " + angle);
                distance_rotation[1] = angle;
            }
        }

        return distance_rotation;
    }

    public static bool PointInPolygon(Vector2 p, Vector2[] polyPoints)
    {
        var j = polyPoints.Length - 1;
        var inside = false;
        for (int i = 0; i < polyPoints.Length; j = i++)
        {
            var pi = polyPoints[i];
            var pj = polyPoints[j];
            if (((pi.y <= p.y && p.y < pj.y) || (pj.y <= p.y && p.y < pi.y)) &&
                (p.x < (pj.x - pi.x) * (p.y - pi.y) / (pj.y - pi.y) + pi.x))
                inside = !inside;
        }
        return inside;
    }
}