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
        [Tooltip("Voice for this strip. Empty uses the controller's default speaker.")]
        public SpeakerProfile speaker;
        public bool instant;
        public DialogueRevealMode revealMode = DialogueRevealMode.ClipCurve;
        public float charactersPerSecond = 30f;
        public AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public float popDuration = 0.06f;
        public float popFromScale = 0.25f;
        public float popOvershoot = 1.12f;
        [Tooltip("Seconds at each end of the strip. The panel fades in, then at the end the line pops and shrinks away. 0 leaves a hard cut.")]
        public float popOutDuration = 0.2f;
        public bool mutter = true;
        public float pitchOffset;
        public string localizationKey = string.Empty;
    }
}
