using UnityEditor;
using UnityEngine;

namespace OceanTiles
{
#if UNITY_EDITOR
    [CustomEditor(typeof(UIButton), true), CanEditMultipleObjects]
    public class UIButtonEditor : UnityEditor.UI.ButtonEditor
    {
        private SerializedProperty soundNameKeyProperty;
        private SerializedProperty hasHapticKeyProperty;
        private SerializedProperty hapticTypeKeyProperty;
        private SerializedProperty clickAnimationKeyProperty;
        private SerializedProperty pressedScaleProperty;
        private SerializedProperty targetTransformProperty;
        private SerializedProperty pressDownDurationProperty;
        private SerializedProperty pressUpDurationProperty;
        private SerializedProperty pressDownEaseProperty;
        private SerializedProperty pressUpEaseProperty;


        protected override void OnEnable()
        {
            base.OnEnable();
            soundNameKeyProperty = serializedObject.FindProperty("sfxName");
            hasHapticKeyProperty = serializedObject.FindProperty("hasHaptic");
            hapticTypeKeyProperty = serializedObject.FindProperty("hapticType");
            clickAnimationKeyProperty = serializedObject.FindProperty("clickAnimation");
            pressedScaleProperty = serializedObject.FindProperty("pressedScale");
            targetTransformProperty = serializedObject.FindProperty("targetTransform");
            pressDownDurationProperty = serializedObject.FindProperty("pressDownDuration");
            pressUpDurationProperty = serializedObject.FindProperty("pressUpDuration");
            pressDownEaseProperty = serializedObject.FindProperty("pressDownEase");
            pressUpEaseProperty = serializedObject.FindProperty("pressUpEase");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.LabelField("Helper", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(soundNameKeyProperty, new GUIContent("Sfx Name"));
            EditorGUILayout.PropertyField(hasHapticKeyProperty, new GUIContent("Has Haptic"));
            EditorGUILayout.PropertyField(hapticTypeKeyProperty, new GUIContent("Haptic Type"));
            EditorGUILayout.PropertyField(clickAnimationKeyProperty, new GUIContent("Click Animation"));
            if (clickAnimationKeyProperty.boolValue)
            {
                EditorGUILayout.PropertyField(targetTransformProperty, new GUIContent("Target Transform"));
                if (targetTransformProperty.objectReferenceValue == null)
                    EditorGUILayout.HelpBox("If Target Transform is null, the button's own transform will be used.", MessageType.Info);
                EditorGUILayout.PropertyField(pressedScaleProperty, new GUIContent("Pressed Scale"));
                EditorGUIHelper.CreateFoldout("Press Down", () =>
                {
                    EditorGUILayout.PropertyField(pressDownDurationProperty, new GUIContent("Press Down Duration"));
                    EditorGUILayout.PropertyField(pressDownEaseProperty, new GUIContent("Press Down Ease"));
                }, "press-down");
                EditorGUIHelper.CreateFoldout("Press Up", () =>
                {
                    EditorGUILayout.PropertyField(pressUpDurationProperty, new GUIContent("Press Up Duration"));
                    EditorGUILayout.PropertyField(pressUpEaseProperty, new GUIContent("Press Up Ease"));
                }, "press-up");
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}