using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Simplex
{
    [CustomPropertyDrawer(typeof(MagicFloat))]
    public class MagicFloatPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect contentPosition = EditorGUI.PrefixLabel(position, label);
            
            DrawMagicFloat(contentPosition, property);
        }

        public static bool DrawMagicFloat(Rect position, SerializedProperty property)
        {
            UniversalControlSettings.Setting setting = (UniversalControlSettings.Setting)property.FindPropertyRelative("m_setting").enumValueIndex;

            float fixedVal = property.FindPropertyRelative("m_fixedValue").floatValue;

            EditorGUI.BeginChangeCheck();

            if (setting == UniversalControlSettings.Setting.FIXED)
            {
                Rect left = new Rect(position.x, position.y, position.width / 2f, position.height);
                Rect right = new Rect(position.x + position.width / 2f, position.y, position.width / 2f, position.height);
                
                setting = (UniversalControlSettings.Setting)EditorGUI.EnumPopup(left, setting);
                fixedVal = EditorGUI.FloatField(right, fixedVal);
            }
            else
            {
                setting = (UniversalControlSettings.Setting)EditorGUI.EnumPopup(position, setting);
            }

            if (EditorGUI.EndChangeCheck())
            {
                property.FindPropertyRelative("m_fixedValue").floatValue = fixedVal;
                property.FindPropertyRelative("m_setting").enumValueIndex = (int)setting;

                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
                Undo.RecordObject(property.serializedObject.targetObject, "Set a magic float.");

                return true;
            }

            return false;
        }
    }
}


