using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace Desayuno64.TimelineDialogue.Editor
{
    [CustomEditor(typeof(DialogueClip))]
    public class DialogueClipInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            SerializedProperty template = serializedObject.FindProperty("template");
            SerializedProperty text = template.FindPropertyRelative("text");
            string before = text.stringValue;

            EditorGUILayout.LabelField("Line", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(text, GUIContent.none);
            EditorGUILayout.PropertyField(template.FindPropertyRelative("speaker"), new GUIContent("Speaker Profile"));
            EditorGUILayout.PropertyField(template.FindPropertyRelative("instant"));
            EditorGUILayout.PropertyField(template.FindPropertyRelative("mutter"));
            EditorGUILayout.PropertyField(template.FindPropertyRelative("pitchOffset"), new GUIContent("Pitch Offset", "Semitones. 12 is one octave."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reveal", EditorStyles.boldLabel);
            SerializedProperty mode = template.FindPropertyRelative("revealMode");
            EditorGUILayout.PropertyField(mode);
            if (mode.enumValueIndex == (int)DialogueRevealMode.CharactersPerSecond)
                EditorGUILayout.PropertyField(template.FindPropertyRelative("charactersPerSecond"));
            else
                EditorGUILayout.PropertyField(template.FindPropertyRelative("revealCurve"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Pop", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(template.FindPropertyRelative("popDuration"));
            EditorGUILayout.PropertyField(template.FindPropertyRelative("popFromScale"));
            EditorGUILayout.PropertyField(template.FindPropertyRelative("popOvershoot"));
            EditorGUILayout.PropertyField(template.FindPropertyRelative("popOutDuration"), new GUIContent("Pop Out Duration", "Seconds at each end of the strip. The panel fades in, then fades out as the line shrinks."));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Localization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(template.FindPropertyRelative("localizationKey"));

            serializedObject.ApplyModifiedProperties();

            if (before != text.stringValue)
                RenameSelectedClip(text.stringValue);
        }

        static void RenameSelectedClip(string authored)
        {
            TimelineClip clip = TimelineEditor.selectedClip;
            if (clip == null || clip.asset == null)
                return;

            string plain = DialogueMarkup.Strip(authored).Replace('\n', ' ').Trim();
            if (plain.Length == 0)
                return;

            clip.displayName = plain.Length <= 32 ? plain : plain.Substring(0, 32);
        }
    }

    [CustomTimelineEditor(typeof(DialogueClip))]
    public class DialogueClipTimelineEditor : ClipEditor
    {
        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            ClipDrawOptions options = base.GetClipOptions(clip);
            var asset = clip.asset as DialogueClip;
            if (asset != null)
                options.tooltip = DialogueMarkup.Strip(asset.template.text);
            return options;
        }

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (clonedFrom != null)
                return;

            var asset = clip.asset as DialogueClip;
            if (asset == null)
                return;

            string plain = DialogueMarkup.Strip(asset.template.text).Replace('\n', ' ').Trim();
            if (plain.Length > 0)
                clip.displayName = plain.Length <= 32 ? plain : plain.Substring(0, 32);
        }
    }
}
