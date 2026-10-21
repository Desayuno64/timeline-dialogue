using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Desayuno64.TimelineDialogue.Editor
{
    public static class DialoguePanelMenu
    {
        [MenuItem("GameObject/Timeline Dialogue/Create Panel")]
        static void CreatePanel()
        {
            var canvasGo = new GameObject("Dialogue", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create Dialogue Panel");

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.08f);
            panelRect.anchorMax = new Vector2(0.9f, 0.28f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(panelGo.transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(24f, 16f);
            textRect.offsetMax = new Vector2(-24f, -16f);
            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "Hello /w there/.";
            tmp.fontSize = 36f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
#if UNITY_2023_2_OR_NEWER
            tmp.textWrappingMode = TextWrappingModes.Normal;
#else
            tmp.enableWordWrapping = true;
#endif
            tmp.color = Color.white;

            var audio = canvasGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;

            var controller = canvasGo.AddComponent<DialogueController>();
            var so = new SerializedObject(controller);
            so.FindProperty("dialoguePanel").objectReferenceValue = panelGo.GetComponent<CanvasGroup>();
            so.FindProperty("dialogueText").objectReferenceValue = tmp;
            so.FindProperty("audioSource").objectReferenceValue = audio;
            so.FindProperty("defaultSpeaker").objectReferenceValue = FindDefaultSpeaker();
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = canvasGo;
        }

        static SpeakerProfile FindDefaultSpeaker()
        {
            string[] guids = AssetDatabase.FindAssets("t:SpeakerProfile");
            if (guids.Length == 0)
                return null;

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<SpeakerProfile>(path);
        }
    }
}
