#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TrumpTile.LevelEditor.Editor
{
    [CustomPropertyDrawer(typeof(EnumLabeledArrayAttribute))]
    public class EnumLabeledArrayDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (EnumLabeledArrayAttribute)attribute;
            string[] enumNames = System.Enum.GetNames(attr.EnumType);

            // property.propertyPath 끝에서 인덱스 추출: "Array.data[2]" → 2
            int index = -1;
            string path = property.propertyPath;
            int start = path.LastIndexOf('[') + 1;
            int end = path.LastIndexOf(']');
            if (start > 0 && end > start)
                int.TryParse(path.Substring(start, end - start), out index);

            string labelText = (index >= 0 && index < enumNames.Length)
                ? enumNames[index]
                : label.text;

            EditorGUI.PropertyField(position, property, new GUIContent(labelText));
        }
    }    
}
#endif
