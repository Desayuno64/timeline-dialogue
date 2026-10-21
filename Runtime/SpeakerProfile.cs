using System;
using UnityEngine;

namespace Desayuno64.TimelineDialogue
{
    [CreateAssetMenu(fileName = "SpeakerProfile", menuName = "Timeline Dialogue/Speaker Profile")]
    public class SpeakerProfile : ScriptableObject
    {
        public enum Vowel
        {
            None,
            A,
            E,
            I,
            O,
            U
        }

        [SerializeField] AudioClip[] vowelA = Array.Empty<AudioClip>();
        [SerializeField] AudioClip[] vowelE = Array.Empty<AudioClip>();
        [SerializeField] AudioClip[] vowelI = Array.Empty<AudioClip>();
        [SerializeField] AudioClip[] vowelO = Array.Empty<AudioClip>();
        [SerializeField] AudioClip[] vowelU = Array.Empty<AudioClip>();

        [Range(0f, 1f)] public float volume = 0.75f;
        [Min(0f)] public float pitchJitterSemitones = 2f;
        [Min(1)] public int blipEveryNGlyphs = 1;

        public AudioClip ClipForGlyph(char glyph, char wordVowel, int glyphIndex)
        {
            char spoken = VowelKind(glyph) != Vowel.None ? glyph : wordVowel;
            AudioClip[] clips = ClipsFor(VowelKind(spoken));
            if (!HasClip(clips))
                clips = vowelA;
            if (!HasClip(clips))
                return null;

            int index = PositiveMod(glyphIndex * 31 + spoken, clips.Length);
            for (int n = 0; n < clips.Length; n++)
            {
                AudioClip clip = clips[(index + n) % clips.Length];
                if (clip != null)
                    return clip;
            }

            return null;
        }

        public static Vowel VowelKind(char c)
        {
            switch (char.ToLowerInvariant(c))
            {
                case 'a':
                case 'á':
                case 'à':
                case 'â':
                case 'ä':
                    return Vowel.A;
                case 'e':
                case 'é':
                case 'è':
                case 'ê':
                case 'ë':
                    return Vowel.E;
                case 'i':
                case 'í':
                case 'ì':
                case 'î':
                case 'ï':
                    return Vowel.I;
                case 'o':
                case 'ó':
                case 'ò':
                case 'ô':
                case 'ö':
                    return Vowel.O;
                case 'u':
                case 'ú':
                case 'ù':
                case 'û':
                case 'ü':
                    return Vowel.U;
                default:
                    return Vowel.None;
            }
        }

        public static void MapWordVowels(string plain, char[] wordVowels)
        {
            int i = 0;
            while (i < plain.Length)
            {
                while (i < plain.Length && char.IsWhiteSpace(plain[i]))
                    i++;

                int start = i;
                char last = '\0';
                while (i < plain.Length && !char.IsWhiteSpace(plain[i]))
                {
                    if (VowelKind(plain[i]) != Vowel.None)
                        last = plain[i];
                    i++;
                }

                for (int k = start; k < i && k < wordVowels.Length; k++)
                    wordVowels[k] = last;
            }
        }

        AudioClip[] ClipsFor(Vowel vowel)
        {
            switch (vowel)
            {
                case Vowel.A: return vowelA;
                case Vowel.E: return vowelE;
                case Vowel.I: return vowelI;
                case Vowel.O: return vowelO;
                case Vowel.U: return HasClip(vowelU) ? vowelU : vowelO;
                default: return vowelA;
            }
        }

        static bool HasClip(AudioClip[] clips)
        {
            if (clips == null)
                return false;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                    return true;
            }

            return false;
        }

        static int PositiveMod(int value, int length)
        {
            int mod = value % length;
            return mod < 0 ? mod + length : mod;
        }
    }
}
