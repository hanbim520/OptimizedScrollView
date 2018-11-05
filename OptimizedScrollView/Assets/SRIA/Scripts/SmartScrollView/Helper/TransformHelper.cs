using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI.Extension
{
    public static class TransformHelper
    {
        public static void GetComponentAtPath<T>(
            this Transform transform,
            string path,
            out T foundComponent) where T : Component
        {
            Transform t = null;
            if (path == null)
            {
                foreach (Transform child in transform)
                {
                    T comp = child.GetComponent<T>();
                    if (comp != null)
                    {
                        foundComponent = comp;
                        return;
                    }
                }
            }
            else
                t = transform.Find(path);

            if (t == null)
                foundComponent = default(T);
            else
                foundComponent = t.GetComponent<T>();
        }

        public static T GetComponentAtPath<T>(
            this Transform transform,
            string path) where T : Component
        {
            T foundComponent;
            transform.GetComponentAtPath(path, out foundComponent);

            return foundComponent;
        }

        public static Transform[] GetChildren(this Transform tr)
        {
            int childCount = tr.childCount;
            Transform[] result = new Transform[childCount];
            for (int i = 0; i < childCount; ++i)
                result[i] = tr.GetChild(i);

            return result;
        }

        public static void GetEnoughChildrenToFitInArray(this Transform tr, Transform[] array)
        {
            int numToReturn = array.Length;
            for (int i = 0; i < numToReturn; ++i)
                array[i] = tr.GetChild(i);
        }

        public static List<Transform> GetDescendants(this Transform tr)
        {
            Transform[] children = tr.GetChildren();

            List<Transform> hierarchy = new List<Transform>();
            hierarchy.AddRange(children);

            int childCount = children.Length;
            for (int i = 0; i < childCount; ++i)
                hierarchy.AddRange(children[i].GetDescendants());

            return hierarchy;
        }

        public static void GetDescendantsAndRelativePaths(this Transform tr, ref Dictionary<Transform, string> mapDescendantToPath)
        {
            tr.GetDescendantsAndRelativePaths("", ref mapDescendantToPath);
        }

        static void GetDescendantsAndRelativePaths(this Transform tr, string currentPath, ref Dictionary<Transform, string> mapDescendantToPath)
        {
            Transform[] children = tr.GetChildren();


            int childCount = children.Length;
            string path;
            for (int i = 0; i < childCount; ++i)
            {
                var ch = children[i];
                path = currentPath + "/" + ch.name;
                mapDescendantToPath[ch] = path;
                ch.GetDescendantsAndRelativePaths(path, ref mapDescendantToPath);
            }
        }

        public static bool IsRectOverlap(Rect r1, Rect r2)
        {
            return !(((r1.xMax < r2.xMin) || (r1.yMax > r2.yMin)) ||
                      ((r2.xMax < r1.xMin) || (r2.yMax > r1.yMin))
                    );
        }

        public static bool IsRectIntersect(float x01, float x02, float y01, float y02, float x11, float x12, float y11, float y12)
        {
            float zx = Mathf.Abs(x01 + x02 - x11 - x12);
            float x = Mathf.Abs(x01 - x02) + Mathf.Abs(x11 - x12);
            float zy = Mathf.Abs(y01 + y02 - y11 - y12);
            float y = Mathf.Abs(y01 - y02) + Mathf.Abs(y11 - y12);
            if (zx <= x && zy <= y)
                return true;
            else
                return false;
        }

        public static int GetNumberOfAncestors(this Transform tr)
        {
            int num = 0;
            while (tr = tr.parent)
                ++num;

            return num;
        }
    }
}

