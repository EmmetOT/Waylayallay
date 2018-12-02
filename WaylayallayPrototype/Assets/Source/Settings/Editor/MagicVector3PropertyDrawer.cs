using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Simplex
{
    [CustomPropertyDrawer(typeof(MagicVector3))]
    public class MagicVector3PropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position = EditorGUI.PrefixLabel(position, label);
            
            Rect left = new Rect(position.x, position.y, position.width / 3f, position.height);
            Rect middle = new Rect(position.x + position.width / 3f, position.y, position.width / 3f, position.height);
            Rect right = new Rect(position.x + 2f * (position.width / 3f), position.y, position.width / 3f, position.height);
            
            bool changed = false;

            changed |= MagicFloatPropertyDrawer.DrawMagicFloat(left, property.FindPropertyRelative("m_x"));
            changed |= MagicFloatPropertyDrawer.DrawMagicFloat(middle, property.FindPropertyRelative("m_y"));
            changed |= MagicFloatPropertyDrawer.DrawMagicFloat(right, property.FindPropertyRelative("m_z"));

            if (changed)
            {
                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                Undo.RecordObject(property.serializedObject.targetObject, "Set a magic Vector3.");
            }
        }
    }
}


