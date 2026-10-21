using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Desayuno64.TimelineDialogue.Editor
{
    [InitializeOnLoad]
    static class DialogueEditorPreviewAudio
    {
        static readonly MethodInfo PlayPreviewClip = FindPlay();
        static bool warned;

        static DialogueEditorPreviewAudio()
        {
            DialoguePreviewAudio.Played += OnPlayed;
        }

        static void OnPlayed(AudioClip clip)
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

            ParameterInfo[] parameters = PlayPreviewClip.GetParameters();
            var args = new object[parameters.Length];
            args[0] = clip;
            for (int i = 1; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                if (type == typeof(int))
                    args[i] = 0;
                else if (type == typeof(bool))
                    args[i] = false;
                else if (type == typeof(float))
                    args[i] = 1f;
                else if (parameters[i].HasDefaultValue)
                    args[i] = parameters[i].DefaultValue;
            }

            PlayPreviewClip.Invoke(null, args);
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
