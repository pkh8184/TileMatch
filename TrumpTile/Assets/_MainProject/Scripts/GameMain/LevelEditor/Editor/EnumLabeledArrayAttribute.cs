using UnityEngine;

namespace TrumpTile.LevelEditor.Editor
{
    public class EnumLabeledArrayAttribute : PropertyAttribute
    {
        public System.Type EnumType;
        public EnumLabeledArrayAttribute(System.Type enumType)
        {
            EnumType = enumType;
        }
    }
}
