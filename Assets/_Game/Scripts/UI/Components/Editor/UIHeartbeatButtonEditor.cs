using UnityEditor;
using UnityEngine;

namespace OceanTiles
{
#if UNITY_EDITOR
    [CustomEditor(typeof(UIHeartbeatButton), true), CanEditMultipleObjects]
    public class UIHeartbeatButtonEditor : UIButtonEditor
    {
        private SerializedProperty heartbeatEnabledProperty;
        private SerializedProperty heartbeatTargetProperty;
        private SerializedProperty heartbeatScaleProperty;
        private SerializedProperty beatUpDurationProperty;
        private SerializedProperty beatDownDurationProperty;
        private SerializedProperty secondBeatUpDurationProperty;
        private SerializedProperty secondBeatDownDurationProperty;
        private SerializedProperty secondBeatScaleProperty;
        private SerializedProperty beatUpEaseProperty;
        private SerializedProperty beatDownEaseProperty;
        private SerializedProperty useContractBeatProperty;
        private SerializedProperty contractScaleProperty;
        private SerializedProperty contractDurationProperty;
        private SerializedProperty contractEaseProperty;
        private SerializedProperty useStartDelayProperty;
        private SerializedProperty startDelayTimeProperty;
        private SerializedProperty useIntervalDelayProperty;
        private SerializedProperty intervalDelayTimeProperty;

        protected override void OnEnable()
        {
            base.OnEnable();
            heartbeatEnabledProperty = serializedObject.FindProperty("heartbeatEnabled");
            heartbeatTargetProperty = serializedObject.FindProperty("heartbeatTarget");
            heartbeatScaleProperty = serializedObject.FindProperty("heartbeatScale");
            beatUpDurationProperty = serializedObject.FindProperty("beatUpDuration");
            beatDownDurationProperty = serializedObject.FindProperty("beatDownDuration");
            secondBeatUpDurationProperty = serializedObject.FindProperty("secondBeatUpDuration");
            secondBeatDownDurationProperty = serializedObject.FindProperty("secondBeatDownDuration");
            secondBeatScaleProperty = serializedObject.FindProperty("secondBeatScale");
            beatUpEaseProperty = serializedObject.FindProperty("beatUpEase");
            beatDownEaseProperty = serializedObject.FindProperty("beatDownEase");
            useContractBeatProperty = serializedObject.FindProperty("useContractBeat");
            contractScaleProperty = serializedObject.FindProperty("contractScale");
            contractDurationProperty = serializedObject.FindProperty("contractDuration");
            contractEaseProperty = serializedObject.FindProperty("contractEase");
            useStartDelayProperty = serializedObject.FindProperty("useStartDelay");
            startDelayTimeProperty = serializedObject.FindProperty("startDelayTime");
            useIntervalDelayProperty = serializedObject.FindProperty("useIntervalDelay");
            intervalDelayTimeProperty = serializedObject.FindProperty("intervalDelayTime");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Heartbeat Animation", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(heartbeatEnabledProperty, new GUIContent("Enabled"));

            if (heartbeatEnabledProperty.boolValue)
            {
                EditorGUILayout.PropertyField(heartbeatTargetProperty, new GUIContent("Target Transform"));
                if (heartbeatTargetProperty.objectReferenceValue == null)
                    EditorGUILayout.HelpBox(
                        "If Target Transform is null, the button's own transform will be used.",
                        MessageType.Info);

                EditorGUIHelper.CreateFoldout("First Beat (Strong)", () =>
                {
                    EditorGUILayout.PropertyField(heartbeatScaleProperty, new GUIContent("Scale"));
                    EditorGUILayout.PropertyField(beatUpDurationProperty, new GUIContent("Up Duration"));
                    EditorGUILayout.PropertyField(beatDownDurationProperty, new GUIContent("Down Duration"));
                }, "heartbeat-first-beat");

                EditorGUIHelper.CreateFoldout("Second Beat (Soft)", () =>
                {
                    EditorGUILayout.PropertyField(secondBeatScaleProperty, new GUIContent("Scale"));
                    EditorGUILayout.PropertyField(secondBeatUpDurationProperty, new GUIContent("Up Duration"));
                    EditorGUILayout.PropertyField(secondBeatDownDurationProperty, new GUIContent("Down Duration"));
                }, "heartbeat-second-beat");

                EditorGUIHelper.CreateFoldout("Contract Beat", () =>
                {
                    EditorGUILayout.PropertyField(useContractBeatProperty, new GUIContent("Use Contract Beat"));
                    if (useContractBeatProperty.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(contractScaleProperty, new GUIContent("Contract Scale"));
                        EditorGUILayout.PropertyField(contractDurationProperty, new GUIContent("Contract Duration"));
                        EditorGUILayout.PropertyField(contractEaseProperty, new GUIContent("Contract Ease"));
                        EditorGUILayout.HelpBox(
                            "Contract → Strong → Contract → Soft → Original",
                            MessageType.Info);
                        EditorGUI.indentLevel--;
                    }
                }, "heartbeat-contract");

                EditorGUIHelper.CreateFoldout("Easing", () =>
                {
                    EditorGUILayout.PropertyField(beatUpEaseProperty, new GUIContent("Beat Up Ease"));
                    EditorGUILayout.PropertyField(beatDownEaseProperty, new GUIContent("Beat Down Ease"));
                }, "heartbeat-easing");

                EditorGUIHelper.CreateFoldout("Delay Settings", () =>
                {
                    EditorGUILayout.PropertyField(useStartDelayProperty, new GUIContent("Use Start Delay"));
                    if (useStartDelayProperty.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(startDelayTimeProperty, new GUIContent("Start Delay (s)"));
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Space(4);
                    EditorGUILayout.PropertyField(useIntervalDelayProperty, new GUIContent("Use Interval Delay"));
                    if (useIntervalDelayProperty.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(intervalDelayTimeProperty,
                            new GUIContent("Interval Delay (s)"));
                        EditorGUI.indentLevel--;
                    }
                }, "heartbeat-delay");
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
