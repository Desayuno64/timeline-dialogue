using UnityEngine;

namespace Desayuno64.TimelineDialogue
{
    [CreateAssetMenu(fileName = "SpeakerProfile", menuName = "Timeline Dialogue/Speaker Profile")]
    public class SpeakerProfile : ScriptableObject
    {
        [SerializeField] AudioClip voice;

        [Range(0f, 1f)] public float volume = 0.75f;
        [Min(0f)] public float pitchJitterSemitones = 2f;
        [Min(1)] public int blipEveryNGlyphs = 1;

        public AudioClip Voice => voice;
    }
}
