using System;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Desayuno64.TimelineDialogue.Editor
{
    [InitializeOnLoad]
    static class DialogueEditorPreviewAudio
    {
        const string CacheFolder = "Packages/com.desayuno64.timeline-dialogue/Editor/PitchCache";

        static readonly MethodInfo PlayPreviewClip = FindPlay();
        static bool warned;
        static bool importing;

        static DialogueEditorPreviewAudio()
        {
            DialoguePreviewAudio.Played += OnPlayed;
        }

        static void OnPlayed(AudioClip clip, float pitch)
        {
            if (Application.isPlaying || clip == null)
                return;

            if (PlayPreviewClip == null)
            {
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning("Timeline Dialogue could not find AudioUtil.PlayPreviewClip. Editor scrub will stay silent.");
                }

                return;
            }

            AudioClip playback = Mathf.Abs(pitch - 1f) < 0.001f ? clip : PitchedAsset(clip, pitch);
            if (playback == null)
                return;

            ParameterInfo[] parameters = PlayPreviewClip.GetParameters();
            var args = new object[parameters.Length];
            args[0] = playback;
            for (int i = 1; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                if (type == typeof(int))
                    args[i] = 0;
                else if (type == typeof(bool))
                    args[i] = false;
                else if (parameters[i].HasDefaultValue)
                    args[i] = parameters[i].DefaultValue;
            }

            PlayPreviewClip.Invoke(null, args);
        }

        static AudioClip PitchedAsset(AudioClip source, float pitch)
        {
            if (source.frequency <= 0)
                return source;

            int sampleRate = Mathf.Clamp(Mathf.RoundToInt(source.frequency * pitch), 8000, 192000);
            string path = CacheFolder + "/" + FileName(source, sampleRate) + ".wav";
            AudioClip cached = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (cached != null)
                return cached;
            if (importing)
                return null;

            importing = true;
            try
            {
                if (!WriteWav(source, path, sampleRate))
                    return null;

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                if (importer != null)
                {
                    AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                    settings.loadType = AudioClipLoadType.DecompressOnLoad;
                    settings.compressionFormat = AudioCompressionFormat.PCM;
                    settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
                    importer.defaultSampleSettings = settings;
                    importer.forceToMono = source.channels == 1;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                importing = false;
            }

            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static string FileName(AudioClip source, int sampleRate)
        {
            string assetPath = AssetDatabase.GetAssetPath(source);
            string name = string.IsNullOrEmpty(assetPath)
                ? source.name
                : Path.GetFileNameWithoutExtension(assetPath);
            var safe = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                safe.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return safe + "_" + sampleRate;
        }

        static bool WriteWav(AudioClip source, string assetPath, int sampleRate)
        {
            int channels = Mathf.Max(1, source.channels);
            var data = new float[source.samples * channels];
            if (!source.GetData(data, 0))
                return false;

            string absolute = Path.GetFullPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            using (var stream = new FileStream(absolute, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(stream))
            {
                int dataBytes = data.Length * 2;
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(36 + dataBytes);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataBytes);
                for (int i = 0; i < data.Length; i++)
                {
                    float sample = Mathf.Clamp(data[i], -1f, 1f);
                    writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
                }
            }

            return true;
        }

        static MethodInfo FindPlay()
        {
            Type util = Type.GetType("UnityEditor.AudioUtil, UnityEditor");
            if (util == null)
                return null;

            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (MethodInfo method in util.GetMethods(flags))
            {
                if (method.Name != "PlayPreviewClip")
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(AudioClip))
                    return method;
            }

            return null;
        }
    }
}
