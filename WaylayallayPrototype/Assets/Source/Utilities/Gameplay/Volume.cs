using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simplex;
using System.Linq;
using Event = Simplex.Event;

namespace Simplex
{
    /// <summary>
    /// A Volume is a cubic region of space attached to a Base object,
    /// which can invoke callbacks when objects enter the region.
    /// </summary>
    [System.Serializable]
    public class Volume
    {
        [SerializeField]
        private MagicVector3 m_size;
        private MagicVector3 m_halfSize;
        public MagicVector3 Size { get { return m_size; } }

        [SerializeField]
        private MagicVector3 m_centre;
        public MagicVector3 Centre { get { return m_centre; } }

        [SerializeField]
        private Base m_parent;

        [SerializeField]
        private bool m_drawWireFrame = false;

        [SerializeField]
        private bool m_drawFill = false;

        [SerializeField]
        private bool m_drawCollisions = false;

        [SerializeField]
        private bool m_drawBounds = false;

        [SerializeField]
        private LayerMask m_layer;
        
        private System.Action<Volume, GameObject> m_onEnterCallback;
        private System.Action<Volume, GameObject> m_onExitCallback;

        private HashSet<Collider> m_currentColliders = new HashSet<Collider>();

        public int Count
        {
            get
            {
                if (m_currentColliders == null)
                    return 0;

                return m_currentColliders.Count;
            }
        }

        private Collider[] m_cols = new Collider[1];

        private bool m_collided = false;

        /// <summary>
        /// Construct a volume!
        /// </summary>
        /// <param name="parent">The base this volume will be 'parented' to.</param>
        /// <param name="size">The size of this volume in world coordinates.</param>
        /// <param name="centre">The centre of the volume relative to its base.</param>
        /// <param name="layer">The collision layer this volume is watching.</param>
        /// <param name="maxCollisions">The maximum number of colliders this volume can track at a time. Defaults to 1.</param>
        /// <param name="drawWireFrame">Whether to draw the wireframe of this volume as a gizmo.</param>
        /// <param name="drawFill">Whether to draw the full volume as a gizmo.</param>
        /// <param name="drawCollisions">Whether to draw the a wireframe around each collider as a gizmo.</param>
        /// <param name="drawCollisions">Whether to draw the the bounds around all colliders inside the volume.</param>
        public Volume(Base parent, MagicVector3 size, MagicVector3 centre, LayerMask layer, int maxCollisions = 1, bool drawWireFrame = true, bool drawFill = true, bool drawCollisions = true, bool drawBounds = true)
        {
            m_parent = parent;
            m_size = size;
            m_halfSize = size * 0.5f;
            m_centre = centre;
            m_cols = new Collider[maxCollisions];
            m_layer = layer;

            m_drawWireFrame = drawWireFrame;
            m_drawFill = drawFill;
            m_drawCollisions = drawCollisions;
            m_drawBounds = drawBounds;
        }

        /// <summary>
        /// Sets the position of this volume relative to its base.
        /// </summary>
        public void SetCentre(MagicVector3 centre)
        {
            m_centre = centre;
        }

        /// <summary>
        /// Sets the size of this volume.
        /// </summary>
        public void SetSize(MagicVector3 size)
        {
            m_size  = size;
        }

        /// <summary>
        /// Set the layer mask this volume considers for collisions.
        /// </summary>
        public void SetLayer(LayerMask layer)
        {
            m_layer = layer;
        }

        /// <summary>
        /// Return the bounds surrounding all colliders in the volume.
        /// </summary>
        public Bounds GetColliderBounds()
        {
            Vector3 pos = Vector3.zero;

            foreach (Collider col in m_currentColliders)
                pos += col.transform.position;

            pos /= m_currentColliders.Count;

            Bounds bounds = new Bounds(pos, Vector3.zero);

            foreach (Collider col in m_currentColliders)
                bounds.Encapsulate(col.bounds);
            
            return new Bounds(bounds.center, bounds.extents * 4f);
        }

        /// <summary>
        /// Get the top of the bounds of the colliders in the volume.
        /// </summary>
        public Vector3 GetBoundsTop()
        {
            Bounds bounds = GetColliderBounds();

            return Vector3.zero.AddToY(bounds.extents.y);
        }

        /// <summary>
        /// Check whether colliders have entered or left this volume.
        /// Returns the number of colliders in the volume.
        /// </summary>
        public int Check()
        {
            if (m_cols != null)
                for (int i = 0; i < m_cols.Length; i++)
                    m_cols[i] = null;
            
            int colliderCount = Physics.OverlapBoxNonAlloc(m_parent.Transform.TransformPoint(m_centre), 
                m_halfSize, m_cols, m_parent.Transform.rotation, m_layer);

            m_collided = (colliderCount > 0);

            if (m_debug)
            {
                Debug.Log("colliderCount = " + colliderCount);

                for (int i = 0; i < m_cols.Length; i++)
                {
                    if (m_cols[i] != null)
                        Debug.Log("---" + m_cols[i].name, m_cols[i]);
                }

                Debug.Break();
            }

            UpdateRegisteredColliders(colliderCount);

            return m_currentColliders.Count;
        }
        
        /// <summary>
        /// Returns whether the given GameObject is in the volume. 
        /// 
        /// Note: the only GameObjects ever 'contained' in this volume are
        /// those with colliders attached.
        /// </summary>
        public bool IsInVolume(GameObject gO)
        {
            Collider col = gO.GetComponent<Collider>();

            if (col == null)
                return false;

            return IsInVolume(col);
        }

        /// <summary>
        /// Returns whether the given collider is in the volume.
        /// </summary>
        public bool IsInVolume(Collider col)
        {
            return m_currentColliders.Contains(col);
        }

        [SerializeField]
        private bool m_debug = false;

        /// <summary>
        /// Determines whether the current colliders in this volume have changed, 
        /// i.e. whether one has entered or left.
        /// </summary>
        private void UpdateRegisteredColliders(int colliderCount)
        {
            if (m_currentColliders == null)
                m_currentColliders = new HashSet<Collider>();

            if (m_collided || colliderCount != m_currentColliders.Count)
            {
                bool updateColliderHashset = false;

                if (colliderCount == m_currentColliders.Count)
                {
                    for (int i = 0; i < m_cols.Length; i++)
                    {
                        if (m_cols[i] == null)
                            continue;

                        if (!m_currentColliders.Contains(m_cols[i]))
                        {
                            updateColliderHashset = true;
                            break;
                        }
                    }
                }
                else
                {
                    updateColliderHashset = true;
                }

                if (updateColliderHashset)
                {
                    if (m_debug)
                    {
                        Debug.Log("Updating hashset.");

                        for (int i = 0; i < m_cols.Length; i++)
                        {
                            if (m_cols[i] != null)
                                Debug.Log(m_cols[i].name, m_cols[i]);
                        }

                        Debug.Break();
                    }



                    // first, find all the gameobjects which are in the true collider
                    // array but not in the hashset we use to track them
                    for (int i = 0; i < m_cols.Length; i++)
                    {
                        if (m_cols[i] != null && !m_currentColliders.Contains(m_cols[i]))
                        {
                            m_currentColliders.Add(m_cols[i]);
                            OnColliderEnter(m_cols[i]);
                        }
                    }

                    // now, find all the colliders which are 'missing' from the tracking hashset
                    Collider[] except = m_currentColliders.Except(m_cols).ToArray();

                    for (int i = except.Length - 1; i >= 0; --i)
                    {
                        m_currentColliders.Remove(except[i]);
                        OnColliderExit(except[i]);
                    }
                }
            }
        }

        #region Events
        
        /// <summary>
        /// Invoked whenever a gameObject enters the volume.
        /// </summary>
        private void OnColliderEnter(Collider col)
        {
            m_parent.ShoutLocally(Event.OnVolumeEntered, this, col.gameObject);
        }

        /// <summary>
        /// Invoked whenever a gameObject exits the volume.
        /// </summary>
        private void OnColliderExit(Collider col)
        {
            m_parent.ShoutLocally(Event.OnVolumeEntered, this, col.gameObject);
        }

        #endregion

        #region Debug

        /// <summary>
        /// Draw this volume for debug purposes. Provide two colours: one for no colliders in the volume,
        /// one for colliders in the volume.
        /// </summary>
        public void DrawGizmo(Color normalColour = default(Color), Color collidedColour = default(Color))
        {
            if (m_parent == null || !Application.isPlaying)
                return;

#if UNITY_EDITOR
            Color displayColor = m_collided ? (collidedColour == default(Color) ? Color.red : collidedColour) : (collidedColour == default(Color) ? Color.blue : collidedColour);

            Gizmos.matrix = Matrix4x4.TRS(m_parent.Transform.position, m_parent.Transform.rotation, m_parent.Transform.lossyScale);

            if (m_drawWireFrame && m_drawFill)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(m_centre, m_size);

                Gizmos.color = displayColor.SetAlpha(0.6f);
                Gizmos.DrawCube(m_centre, m_size);
            }
            else if (m_drawWireFrame)
            {
                Gizmos.color = displayColor;
                Gizmos.DrawWireCube(m_centre, m_size);
            }
            else if (m_drawFill)
            {
                Gizmos.color = displayColor.SetAlpha(0.6f);
                Gizmos.DrawCube(m_centre, m_size);
            }

            if (m_drawCollisions && !m_cols.IsNullOrEmpty())
            {
                for (int i = 0; i < m_cols.Length; i++)
                {
                    if (m_cols[i] == null)
                        continue;

                    Gizmos.matrix = Matrix4x4.TRS(m_cols[i].transform.position, m_cols[i].transform.rotation, m_cols[i].transform.lossyScale * 1.1f);
                    
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                }
            }

            if (m_drawBounds && !m_currentColliders.IsNullOrEmpty())
            {
                Bounds bounds = GetColliderBounds();

                Gizmos.matrix = Matrix4x4.TRS(bounds.center, Quaternion.identity, bounds.extents * 1.15f);

                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            }
#endif
        }
        #endregion
    }
}
