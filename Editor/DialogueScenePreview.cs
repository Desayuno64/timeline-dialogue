using UnityEditor;
using UnityEngine;

namespace Desayuno64.TimelineDialogue.Editor
{
    [InitializeOnLoad]
    static class DialogueScenePreview
    {
        static bool repaintQueued;

        static DialogueScenePreview()
        {
            DialogueEditorRefresh.Requested += QueueRepaint;
        }

        static void QueueRepaint()
        {
            if (repaintQueued || Application.isPlaying)
                return;

            repaintQueued = true;
            EditorApplication.delayCall += RepaintGameView;
        }

        static void RepaintGameView()
        {
            repaintQueued = false;
            if (Application.isPlaying)
                return;

            var gameView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView == null)
                return;

            foreach (var window in Resources.FindObjectsOfTypeAll(gameView))
                ((EditorWindow)window).Repaint();
        }
    }
}
