using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.Assertions;
using System.Text;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Simplex
{
    public struct MorphBisecter : IDisposable
    {
        private Morph m_morph;

        public MorphBisecter(Morph morph)
        {
            m_morph = morph;
        }
        
        public void Bisect(Plane plane)
        {

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public struct MorphFaceExtruder : IDisposable
    {
        private MorphFilter m_morph;
        private Face m_face;
        private List<Point> m_perimeter;
        private List<Point> m_points;

        private Vector3 m_normal;
        private Vector3[] m_initialPositions;

        private float m_currentExtrusion;

        public MorphFaceExtruder(MorphFilter morph, int faceID)
        {
            m_morph = morph;
            m_face = m_morph.Morph.GetFace(faceID);
            m_perimeter = m_face.GetPerimeter();
            m_points = m_face.GetPoints();
            m_normal = m_face.Normal;
            m_initialPositions = new Vector3[m_points.Count];

            m_currentExtrusion = 0f;

            for (int i = 0; i < m_points.Count; i++)
                m_initialPositions[i] = m_points[i].LocalPosition;


            //for (int i = 0; i < m_perimeter.Count; i++)
            //{

            //}
        }

        public void ExtrudeFace(float distance)
        {
            if (distance == m_currentExtrusion)
                return;

            for (int i = 0; i < m_points.Count; i++)
                m_points[i].LocalPosition = m_initialPositions[i] + (distance * m_normal);

            m_morph.Refresh();
        }
        
        public void Dispose()
        {
        }
    }

}