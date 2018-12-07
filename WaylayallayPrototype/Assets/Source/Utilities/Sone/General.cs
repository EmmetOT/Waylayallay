using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace Sone
{
    public static class General
    {
        private static System.Random m_rng = new System.Random();

        #region Colours

        /// <summary>
        /// Sets the alpha value of this colour.
        /// </summary>
        public static Color SetAlpha(this Color color, float alpha)
        {
            Color col = color;
            col.a = alpha;
            return col;
        }

        #endregion

        #region Enums

        /// <summary>
        /// Converts an enums integer value to its position as declared in the enum. By default, these
        /// values are the same, but sometimes (such as in flag enums), they don't correspond.
        /// </summary>
        public static int ConvertEnumValueToPosition(Enum enumType, int val)
        {
            int index = 0;

            foreach (int enumValue in Enum.GetValues(enumType.GetType()))
            {
                if (val == enumValue)
                    return index;

                ++index;
            }

            return val;
        }

        #endregion

        #region Containers
        
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
                return true;

            // If this is a collection, use the Count property for efficiency. 
            // The Count property is O(1) while counting is O(N).
            ICollection<T> collection = enumerable as ICollection<T>;

            if (collection != null)
                return collection.Count == 0;

            return !enumerable.Any();
        }

        public static int Count<T>(this IEnumerable<T> enumerable)
        {
            // If this is a collection, use the Count property for efficiency. 
            // The Count property is O(1) while counting is O(N).
            ICollection<T> collection = enumerable as ICollection<T>;
            if (collection != null)
            {
                return collection.Count;
            }

            int count = 0;

            foreach (T t in enumerable)
                ++count;

            return count;
        }

        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        /// <summary>
        /// Perform an in-place fisher-yates shuffle.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = m_rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        #endregion

        #region Input

        /// <summary>
        /// Given an object, try to cast it to the given type,
        /// and return true if successful.
        /// </summary>
        public static bool ProcessArg<T>(object arg, out T t)
        {
            t = default(T);

            if (arg != null && typeof(T).IsAssignableFrom(arg.GetType()))
            {
                t = (T)arg;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Given a camera and a LayerMask, return a raycast from the mouse position.
        /// </summary>
        public static bool ShootRaycastFromMouse(LayerMask layerMask, Camera camera, out RaycastHit rayHit)
        {
            return ShootRaycastFromScreenPoint(layerMask, camera, Input.mousePosition, out rayHit);
        }

        /// <summary>
        /// Given a camera and a LayerMask, return a raycast from the given screen point.
        /// </summary>
        public static bool ShootRaycastFromScreenPoint(LayerMask layerMask, Camera camera, Vector3 screenPoint, out RaycastHit rayHit)
        {
            return Physics.Raycast(camera.ScreenPointToRay(screenPoint), out rayHit, Mathf.Infinity, layerMask);
        }

        #endregion

        #region Unity

        /// <summary>
        /// Checks if a layer is in a LayerMask
        /// </summary>
        /// <returns><c>true</c>, if layer is in layermask, <c>false</c> otherwise.</returns>
        /// <param name="layer">Layer</param>
        /// <param name="layerMask">LayerMask</param>
        public static bool CompareLayers(int layer, LayerMask layerMask)
        {
            return ((1 << layer) & layerMask) > 0;
        }

        /// <summary>
        /// Given a point, search in concentric layers up to the max search radius, looking for the closest point to on the navmesh.
        /// </summary>
        public static Vector3 FindClosestPointOnNavMesh(Vector3 destinationPoint, float maxSearchRadius = 10f)
        {
            NavMeshHit navMeshHit;
            float distance = 0f;

            while (!NavMesh.SamplePosition(destinationPoint, out navMeshHit, distance, NavMesh.AllAreas) || distance < maxSearchRadius)
            {
                distance++;
            }
            destinationPoint = navMeshHit.position;

            return destinationPoint;
        }

        /// <summary>
        /// Return all components attached to this GameObject and its children.
        /// </summary>
        public static T[] FindAll<T>(this GameObject obj) where T : Component
        {
            return obj.GetComponentsInChildren<T>(includeInactive: true);
        }

        /// <summary>
        /// Return all components attached to this GameObject and its children.
        /// </summary>
        public static T[] FindAll<T>(this MonoBehaviour mono) where T : Component
        {
            return FindAll<T>(mono.gameObject);
        }

        /// <summary>
        /// Return a component attached either to this GameObject, one of its children, or its parent.
        /// </summary>
        public static T Find<T>(this GameObject obj) where T : Component
        {
            T t = obj.GetComponent<T>();

            return t ?? obj.GetComponentInChildren<T>(includeInactive: true) ?? obj.GetComponentInParent<T>();
        }

        /// <summary>
        /// Return a component attached either to this GameObject, one of its children, or its parent.
        /// </summary>
        public static T Find<T>(this MonoBehaviour mono) where T : Component
        {
            return Find<T>(mono.gameObject);
        }

        #endregion

        #region Vector Extensions

        public static Vector3 SetX(this Vector3 vec, float x)
        {
            return new Vector3(x, vec.y, vec.z);
        }

        public static Vector2 SetX(this Vector2 vec, float x)
        {
            return new Vector2(x, vec.y);
        }

        public static Vector3 SetY(this Vector3 vec, float y)
        {
            return new Vector3(vec.x, y, vec.z);
        }

        public static Vector2 SetY(this Vector2 vec, float y)
        {
            return new Vector2(vec.x, y);
        }

        public static Vector3 SetZ(this Vector3 vec, float z)
        {
            return new Vector3(vec.x, vec.y, z);
        }

        public static Vector3 AddToX(this Vector3 vec, float x)
        {
            return new Vector3(vec.x + x, vec.y, vec.z);
        }

        public static Vector3 AddToY(this Vector3 vec, float y)
        {
            return new Vector3(vec.x, vec.y + y, vec.z);
        }

        public static Vector3 AddToZ(this Vector3 vec, float z)
        {
            return new Vector3(vec.x, vec.y, vec.z + z);
        }

        public static Vector2 AddToX(this Vector2 vec, float x)
        {
            return new Vector2(vec.x + x, vec.y);
        }

        public static Vector2 AddToY(this Vector2 vec, float y)
        {
            return new Vector2(vec.x, vec.y + y);
        }

        public static Vector3 SetZ(this Vector2 vec, float z)
        {
            return new Vector3(vec.x, vec.y, z);
        }

        /// <summary>
        /// Sets the y ordinate to 0.
        /// </summary>
        public static Vector3 Flatten(this Vector3 vec)
        {
            return new Vector3(vec.x, 0f, vec.z);
        }

        public static int HashableVector3(this Vector3 vector)
        {
            int hash = 17;

            hash = hash * 31 + Mathf.RoundToInt(vector.x * 10000f);
            hash = hash * 31 + Mathf.RoundToInt(vector.y * 10000f);
            hash = hash * 31 + Mathf.RoundToInt(vector.z * 10000f);

            return hash;
        }

        #endregion
    }
}
