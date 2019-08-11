using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simplex
{

    /// <summary>
    /// Represents a vertex of the mesh. A class
    /// so a single vertex can be referenced in lots of places without being copied,
    /// and unlike Vector3 it has a hash code which is guaranteed to be the same for points in the same position.
    /// 
    /// This is the building block of all morphs.
    /// </summary>
    [System.Serializable]
    public class Point
    {
        [System.NonSerialized]
        private Data m_data;

        [SerializeField]
        private Vector3 m_localPosition;
        public Vector3 LocalPosition
        {
            get { return m_localPosition; }
            set
            {
                m_localPosition = value;
                m_data.OnRelocatePoint(ID);
            }
        }

        public Vector2 UV { get; private set; }
        public Vector3 Normal { get; private set; }
        public Vector4 Tangent { get; private set; }

        /// <summary>
        /// Used to uniquely identify a particular point object, even after transformations.
        /// </summary>
        public int ID { get; private set; } = -1;

        public bool HasDataOtherThanPosition
        {
            get
            {
                return UV != Vector2.zero || Normal != Vector3.zero || Tangent != Vector4.zero;
            }
        }

        public Point(Data data, Vector3 vector, Vector2 uv = default, Vector3 normal = default, Vector4 tangent = default)
        {
            SetData(data);
            LocalPosition = vector;
            UV = uv;
            Normal = normal;
            Tangent = tangent;
        }

        /// <summary>
        /// Set the value used to uniquely identify this point. Should not change over the lifetime
        /// of the point.
        /// </summary>
        public void SetID(int id)//
        {
            ID = id;
        }

        public void SetData(Data data)
        {
            m_data = data;
        }

        /// <summary>
        /// Take the normal, uv, and tangent data from the other point.
        /// </summary>
        public void CopyNonPositionData(Point other)
        {
            UV = other.UV;
            Normal = other.Normal;
            Tangent = other.Tangent;
        }

        /// <summary>
        /// Returns an integer which is unique to the point's current location. This ID
        /// is not liable to floating point errors which makes it a very useful way to compare
        /// vectors.
        /// </summary>
        public int GetLocationID()
        {
            return LocalPosition.HashVector3();
        }

        /// <summary>
        /// Transforms this point as if it's on the 2d plane and its z component doesn't matter,
        /// accorsding to the given normal.
        /// </summary>
        public Vector2 To2D(Vector3 normal)
        {
            return RotateBy(Quaternion.FromToRotation(normal, -Vector3.forward));
        }

        public Vector3 GetWorldPosition(Matrix4x4 localToWorldMatrix)
        {
            return localToWorldMatrix * LocalPosition;
        }

        /// <summary>
        /// Transforms this point by the given rotation.
        /// </summary>
        public Vector3 RotateBy(Quaternion rotation)
        {
            return rotation * LocalPosition;
        }

        public override string ToString()
        {
            return "[" + ID + "] " + LocalPosition.ToString();
        }

        public static bool operator ==(Point point1, Point point2)
        {
            if (point1 is null)
                return point2 is null;
            else if (point2 is null)
                return false;

            return point1.GetLocationID() == point2.GetLocationID();
        }

        public static bool operator !=(Point point1, Point point2)
        {
            return !(point1 == point2);
        }

        public override bool Equals(object obj)
        {
            Point other = obj as Point;

            if (other == null)
                return false;

            return ID == other.ID;
        }

        public override int GetHashCode()
        {
            return ID;
        }

#if UNITY_EDITOR

        public void DrawNormalGizmo(Color col, Transform transform = null)
        {
            Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
            Gizmos.matrix = transform == null ? originalGizmoMatrix : transform.localToWorldMatrix;

            Gizmos.color = col;
            Gizmos.DrawLine(LocalPosition, LocalPosition + Normal * 0.6f);

            Gizmos.matrix = originalGizmoMatrix;
        }

        public void DrawPointLabels(Color col, Transform transform = null)
        {
            Matrix4x4 originalHandleMatrix = Handles.matrix;
            Handles.matrix = transform == null ? originalHandleMatrix : transform.localToWorldMatrix;

            GUIStyle handleStyle = new GUIStyle();
            handleStyle.normal.textColor = Color.red;
            handleStyle.fontSize = 9;

            Handles.Label(LocalPosition + Normal * 0.15f + new Vector3(0f, ID * 0.01f, 0f), ID.ToString(), handleStyle);

            Handles.matrix = originalHandleMatrix;
        }

        public void DrawPointGizmo(Color col, float radius = 0.025f, Transform transform = null)
        {
            Matrix4x4 originalGizmoMatrix = Gizmos.matrix;
            Gizmos.matrix = transform == null ? originalGizmoMatrix : transform.localToWorldMatrix;

            Matrix4x4 originalHandleMatrix = Handles.matrix;
            Handles.matrix = transform == null ? originalHandleMatrix : transform.localToWorldMatrix;

            Gizmos.DrawSphere(LocalPosition, radius);

            Gizmos.matrix = originalGizmoMatrix;
            Handles.matrix = originalHandleMatrix;
#endif
        }
    }
}