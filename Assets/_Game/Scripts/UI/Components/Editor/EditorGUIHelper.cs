using System;
using UnityEditor;

namespace OceanTiles
{
    public static class EditorGUIHelper
    {
        public static void CreateFoldout(string label, Action content, string stateKey)
        {
            bool isExpanded = EditorPrefs.GetBool(stateKey, true);
            bool newState = EditorGUILayout.Foldout(isExpanded, label, true);
            if (newState != isExpanded)
                EditorPrefs.SetBool(stateKey, newState);
            if (newState)
            {
                EditorGUI.indentLevel++;
                content();
                EditorGUI.indentLevel--;
            }
        }
    }
}
