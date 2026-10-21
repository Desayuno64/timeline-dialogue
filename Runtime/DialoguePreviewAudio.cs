using System;
using UnityEngine;

namespace Desayuno64.TimelineDialogue
{
    public static class DialoguePreviewAudio
    {
        public static event Action<AudioClip> Played;

        public static void Play(AudioClip clip)
        {
            if (clip != null)
                Played?.Invoke(clip);
        }
    }
}
