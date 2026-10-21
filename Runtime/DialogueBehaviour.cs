using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Desayuno64.TimelineDialogue
{
    public enum DialogueRevealMode
    {
        ClipCurve = 0,
        CharactersPerSecond = 1
    }

    [Serializable]
    public class DialogueBehaviour : PlayableBehaviour
    {
        [TextArea(3, 8)] public string text = string.Empty;
        public SpeakerProfile speaker;
        public bool instant;
        public DialogueRevealMode revealMode = DialogueRevealMode.ClipCurve;
        public float charactersPerSecond = 30f;
        public AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float popDuration = 0.06f;
        public float popFromScale = 0.25f;
        public float popOvershoot = 1.12f;
        public bool mutter = true;
        public float pitchOffset;
        public string localizationKey = string.Empty;
    }
}
