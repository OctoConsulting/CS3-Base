#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;
using System;

namespace SensorsSDK.UnityUtilities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class RequireInterfaceAttribute : PropertyAttribute
    {
        public Type RequiredInterface { get; private set; }

        public RequireInterfaceAttribute(Type inheritsFromType)
        {
            RequiredInterface = inheritsFromType;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RequireInterfaceAttribute))]
    public class TypeRestrictionPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var o = property.objectReferenceValue;

            GameObject go = null;
            if (o is GameObject)
            {
                go = o as GameObject;
            }
            else if (o is MonoBehaviour)
            {
                go = ((MonoBehaviour)o).gameObject;
            }

            // Look for a component that fulfuls our requirement.
            var ria = this.attribute as RequireInterfaceAttribute;
            Component c = null;
            if (go != null)
            {
                Component[] cos = go.GetComponents(ria.RequiredInterface);
                c = (cos.Length > 0) ? cos[0] : null;
                if (cos.Length > 1)
                {
                    Debug.LogWarning($"Found multiple interfaces that match interface requirement ({ria.RequiredInterface.ToString()}) for field '{label.text}'. Choosing the first one.");
                }
            }

            // If we couldn't find a component that matches our requirement but an object was set,
            // log an error to let the user know something has gone very wrong.
            if (property.objectReferenceValue != null && c == null)
            {
                Debug.LogError($"Field '{label.text}' requires an object that implements the {ria.RequiredInterface.ToString()} interface. The given object ({(go == null ? "null" : go.name)}) does not fulfill this requirement.");
            }

            // Always clear the objectReferenceValue if we couldn't find a match, but only update
            // it if we were given a MonoBehavior. If it's a gameObject, we want to leave it alone.
            if (c == null || o is MonoBehaviour)
            {
                property.objectReferenceValue = c;
            }

            EditorGUI.PropertyField(position, property, label);
        }
    }
#endif
}
