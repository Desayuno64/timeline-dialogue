using System;
using TMPro;
using UnityEngine;
using UnityEngine.Timeline;

namespace Desayuno64.TimelineDialogue
{
    public class DialogueController : MonoBehaviour
    {
        const int AllCharactersVisible = 99999;
        const float WaveAmplitude = 4f;
        const float WavePeriod = 0.5f;
        const float WavePhaseStep = 0.35f;
        const float JitterAmplitude = 1.5f;
        const float JitterHold = 0.08f;
        const float SeekSeconds = 0.5f;
        const int PoolSize = 8;

        [SerializeField] CanvasGroup dialoguePanel;
        [SerializeField] TMP_Text dialogueText;
        [SerializeField] AudioSource audioSource;
        [SerializeField] SpeakerProfile defaultSpeaker;

        int activeContentId;
        string activeAuthored = string.Empty;
        DialogueMarkup markup = DialogueMarkup.Parse(string.Empty);
        char[] wordVowels = Array.Empty<char>();
        float[] appearAt = Array.Empty<float>();
        Vector3[][] cachedBase;
        string cachedRich;
        int cachedVisible = -1;
        int previousVisible;
        double previousTime = -1d;
        int nonSpaceRevealed;
        AudioSource[] pool;
        int poolCursor;

        public void Present(int contentId, DialogueBehaviour behaviour, float curveProgress, double clipTime)
        {
            if (dialoguePanel == null || dialogueText == null || behaviour == null)
                return;

            string authored = Resolve(behaviour.text, behaviour.localizationKey);
            bool contentChanged = contentId != activeContentId || authored != activeAuthored;
            if (contentChanged)
            {
                activeContentId = contentId;
                activeAuthored = authored;
                markup = DialogueMarkup.Parse(authored);
                wordVowels = new char[markup.Plain.Length];
                SpeakerProfile.MapWordVowels(markup.Plain, wordVowels);
                appearAt = new float[markup.Plain.Length];
                for (int i = 0; i < appearAt.Length; i++)
                    appearAt[i] = -1f;
                previousVisible = 0;
                previousTime = -1d;
                nonSpaceRevealed = 0;
                cachedRich = null;
                cachedVisible = -1;
            }

            Show();

            int total = markup.Plain.Length;
            int visible = VisibleCount(behaviour, curveProgress, clipTime, total);
            bool snap = behaviour.instant || IsSnap(clipTime, visible, contentChanged);
            SpeakAndStamp(behaviour, clipTime, visible, snap);
            previousVisible = visible;
            previousTime = clipTime;

            ApplyMesh(behaviour, clipTime, visible);
        }

        public void Hide()
        {
            activeContentId = 0;
            activeAuthored = string.Empty;
            markup = DialogueMarkup.Parse(string.Empty);
            previousVisible = 0;
            previousTime = -1d;
            cachedRich = null;
            cachedVisible = -1;
            cachedBase = null;

            if (dialoguePanel == null)
                return;

            dialoguePanel.alpha = 0f;
            dialoguePanel.interactable = false;
            dialoguePanel.blocksRaycasts = false;
            if (Application.isPlaying)
                dialoguePanel.gameObject.SetActive(false);

            if (dialogueText != null)
                dialogueText.maxVisibleCharacters = AllCharactersVisible;
        }

        public void RegisterPreview(IPropertyCollector driver)
        {
            if (dialoguePanel != null)
            {
                driver.AddFromName<CanvasGroup>(dialoguePanel.gameObject, "m_Alpha");
                driver.AddFromName<CanvasGroup>(dialoguePanel.gameObject, "m_Interactable");
                driver.AddFromName<CanvasGroup>(dialoguePanel.gameObject, "m_BlocksRaycasts");
            }

            if (dialogueText != null)
            {
                driver.AddFromName<TMP_Text>(dialogueText.gameObject, "m_text");
                driver.AddFromName<TMP_Text>(dialogueText.gameObject, "m_maxVisibleCharacters");
            }
        }

        void Awake()
        {
            Hide();
        }

        void Show()
        {
            dialoguePanel.gameObject.SetActive(true);
            dialoguePanel.alpha = 1f;
            dialoguePanel.interactable = true;
            dialoguePanel.blocksRaycasts = true;
            dialogueText.richText = true;
        }

        string Resolve(string authored, string key)
        {
            authored ??= string.Empty;
            if (string.IsNullOrEmpty(key))
                return authored;

            DialogueLineResolver resolver = GetComponent<DialogueLineResolver>();
            if (resolver == null)
                resolver = GetComponentInParent<DialogueLineResolver>();

            return resolver != null ? resolver.Resolve(authored, key) ?? authored : authored;
        }

        static int VisibleCount(DialogueBehaviour behaviour, float curveProgress, double clipTime, int total)
        {
            if (total <= 0)
                return 0;
            if (behaviour.instant)
                return total;
            if (behaviour.revealMode == DialogueRevealMode.CharactersPerSecond)
            {
                float rate = Mathf.Max(1f, behaviour.charactersPerSecond);
                return Mathf.Clamp(Mathf.FloorToInt((float)clipTime * rate), 0, total);
            }

            if (curveProgress >= 1f)
                return total;

            return Mathf.Clamp(Mathf.FloorToInt(total * Mathf.Clamp01(curveProgress)), 0, total);
        }

        bool IsSnap(double clipTime, int visible, bool contentChanged)
        {
            if (previousTime < 0d)
                return contentChanged && (clipTime > 0.1d || visible > 2);

            if (clipTime < previousTime - 0.0001d)
                return true;

            return clipTime - previousTime > SeekSeconds;
        }

        void SpeakAndStamp(DialogueBehaviour behaviour, double clipTime, int visible, bool snap)
        {
            if (snap)
            {
                float settled = (float)clipTime - Mathf.Max(0.01f, behaviour.popDuration) - 1f;
                for (int i = 0; i < visible && i < appearAt.Length; i++)
                    appearAt[i] = settled;
                previousVisible = visible;
                nonSpaceRevealed = CountNonSpace(0, visible);
                return;
            }

            int from = previousVisible;
            for (int i = from; i < visible && i < appearAt.Length; i++)
            {
                if (appearAt[i] < 0f)
                    appearAt[i] = (float)clipTime;
            }

            if (behaviour.mutter && !behaviour.instant)
                BlipRange(behaviour, from, visible);
        }

        void BlipRange(DialogueBehaviour behaviour, int from, int to)
        {
            SpeakerProfile speaker = behaviour.speaker != null ? behaviour.speaker : defaultSpeaker;
            if (speaker == null)
                return;

            int every = Mathf.Max(1, speaker.blipEveryNGlyphs);
            for (int i = from; i < to && i < markup.Plain.Length; i++)
            {
                if (char.IsWhiteSpace(markup.Plain[i]))
                    continue;

                nonSpaceRevealed++;
                if (((nonSpaceRevealed - 1) % every) != 0)
                    continue;

                AudioClip clip = speaker.ClipForGlyph(markup.Plain[i], wordVowels[i], i);
                if (clip == null)
                    continue;

                if (!Application.isPlaying)
                {
                    DialoguePreviewAudio.Play(clip);
                    continue;
                }

                PlayBlip(clip, speaker, behaviour.pitchOffset);
            }
        }

        void PlayBlip(AudioClip clip, SpeakerProfile speaker, float pitchOffset)
        {
            EnsurePool();
            if (pool == null || pool.Length == 0)
                return;

            AudioSource source = pool[poolCursor % pool.Length];
            poolCursor++;
            if (source == null)
                return;

            float jitter = speaker.pitchJitterSemitones;
            float semitones = pitchOffset + UnityEngine.Random.Range(-jitter, jitter);
            source.pitch = Mathf.Pow(2f, semitones / 12f);
            source.PlayOneShot(clip, speaker.volume);
        }

        void EnsurePool()
        {
            if (pool != null)
                return;

            AudioSource main = audioSource != null ? audioSource : GetComponentInParent<AudioSource>();
            if (main == null)
                return;

            audioSource = main;
            main.playOnAwake = false;
            main.spatialBlend = 0f;
            pool = new AudioSource[PoolSize];
            pool[0] = main;
            for (int i = 1; i < PoolSize; i++)
            {
                AudioSource extra = main.gameObject.AddComponent<AudioSource>();
                extra.playOnAwake = false;
                extra.spatialBlend = 0f;
                extra.volume = main.volume;
                extra.outputAudioMixerGroup = main.outputAudioMixerGroup;
                pool[i] = extra;
            }
        }

        int CountNonSpace(int from, int to)
        {
            int count = 0;
            for (int i = from; i < to && i < markup.Plain.Length; i++)
            {
                if (!char.IsWhiteSpace(markup.Plain[i]))
                    count++;
            }

            return count;
        }

        void ApplyMesh(DialogueBehaviour behaviour, double clipTime, int visible)
        {
            string rich = markup.ToRichText();
            bool rebuilt = rich != cachedRich || visible != cachedVisible;
            if (rebuilt)
            {
                dialogueText.text = rich;
                dialogueText.maxVisibleCharacters = visible;
                dialogueText.ForceMeshUpdate();
                CacheBaseVertices();
                cachedRich = rich;
                cachedVisible = visible;
            }

            if (!rebuilt && !NeedsLiveMotion(behaviour, clipTime, visible))
                return;

            if (cachedBase == null)
                return;

            TMP_TextInfo info = dialogueText.textInfo;
            if (info.characterCount != markup.Plain.Length)
            {
                if (rebuilt)
                    dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
                return;
            }

            for (int m = 0; m < info.meshInfo.Length && m < cachedBase.Length; m++)
            {
                Vector3[] dst = info.meshInfo[m].vertices;
                Vector3[] src = cachedBase[m];
                if (dst == null || src == null)
                    continue;

                int n = Mathf.Min(dst.Length, src.Length);
                for (int v = 0; v < n; v++)
                    dst[v] = src[v];
            }

            float popDuration = Mathf.Max(0.0001f, behaviour.popDuration);
            for (int i = 0; i < visible && i < info.characterCount; i++)
            {
                if (char.IsWhiteSpace(markup.Plain[i]))
                    continue;

                TMP_CharacterInfo glyph = info.characterInfo[i];
                if (!glyph.isVisible || glyph.vertexIndex < 0)
                    continue;

                int meshIndex = glyph.materialReferenceIndex;
                if (meshIndex < 0 || meshIndex >= info.meshInfo.Length)
                    continue;

                Vector3[] dst = info.meshInfo[meshIndex].vertices;
                int vi = glyph.vertexIndex;
                if (vi + 3 >= dst.Length)
                    continue;

                Vector3 center = (dst[vi] + dst[vi + 2]) * 0.5f;
                float age = appearAt[i] < 0f ? popDuration : (float)clipTime - appearAt[i];
                float scale = PopScale(age / popDuration, behaviour.popFromScale, behaviour.popOvershoot);
                Vector3 offset = GlyphOffset(markup.Flags[i], i, clipTime);

                for (int k = 0; k < 4; k++)
                    dst[vi + k] = center + (dst[vi + k] - center) * scale + offset;
            }

            dialogueText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        bool NeedsLiveMotion(DialogueBehaviour behaviour, double clipTime, int visible)
        {
            float popDuration = Mathf.Max(0.0001f, behaviour.popDuration);
            for (int i = 0; i < visible && i < markup.Plain.Length; i++)
            {
                if (char.IsWhiteSpace(markup.Plain[i]))
                    continue;

                DialogueGlyphFlags flags = markup.Flags[i];
                if ((flags & (DialogueGlyphFlags.Wave | DialogueGlyphFlags.Jitter)) != 0)
                    return true;

                if (behaviour.instant)
                    continue;

                float age = appearAt[i] < 0f ? 0f : (float)clipTime - appearAt[i];
                if (age < popDuration)
                    return true;
            }

            return false;
        }

        void CacheBaseVertices()
        {
            TMP_TextInfo info = dialogueText.textInfo;
            cachedBase = new Vector3[info.meshInfo.Length][];
            for (int m = 0; m < info.meshInfo.Length; m++)
            {
                Vector3[] src = info.meshInfo[m].vertices;
                cachedBase[m] = src != null ? (Vector3[])src.Clone() : Array.Empty<Vector3>();
            }
        }

        static float PopScale(float t, float fromScale, float overshoot)
        {
            t = Mathf.Clamp01(t);
            fromScale = Mathf.Clamp(fromScale, 0f, 1f);
            overshoot = Mathf.Max(1f, overshoot);
            if (t < 0.7f)
            {
                float u = t / 0.7f;
                float ease = 1f - (1f - u) * (1f - u);
                return Mathf.Lerp(fromScale, overshoot, ease);
            }

            float settle = (t - 0.7f) / 0.3f;
            return Mathf.Lerp(overshoot, 1f, settle);
        }

        static Vector3 GlyphOffset(DialogueGlyphFlags flags, int index, double clipTime)
        {
            Vector3 offset = Vector3.zero;
            if ((flags & DialogueGlyphFlags.Wave) != 0)
            {
                float phase = (float)(clipTime / WavePeriod) + index * WavePhaseStep;
                offset.y += Mathf.Sin(phase * Mathf.PI * 2f) * WaveAmplitude;
            }

            if ((flags & DialogueGlyphFlags.Jitter) != 0)
            {
                int bucket = (int)Math.Floor(clipTime / JitterHold);
                offset.x += HashSigned(index, bucket) * JitterAmplitude;
                offset.y += HashSigned(index, bucket + 17) * JitterAmplitude;
            }

            return offset;
        }

        static float HashSigned(int a, int b)
        {
            uint h = (uint)(a * 374761393 + b * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 1023u) / 1023f * 2f - 1f;
        }
    }
}
