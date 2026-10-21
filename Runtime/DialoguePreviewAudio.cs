using System;
using UnityEngine;

namespace Desayuno64.TimelineDialogue
{
    public static class DialoguePreviewAudio
    {
        public static event Action<AudioClip, float> Played;

        public static void Play(AudioClip clip, float pitch)
        {
            if (clip != null)
                Played?.Invoke(clip, Mathf.Max(0.05f, pitch));
        }
    }

    public static class DialogueEditorRefresh
    {
        public static event Action Requested;

        public static void Request()
        {
            if (!Application.isPlaying)
                Requested?.Invoke();
        }
    }
}
