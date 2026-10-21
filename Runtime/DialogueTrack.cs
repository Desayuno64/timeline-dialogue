using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Desayuno64.TimelineDialogue
{
    [TrackColor(0.2f, 0.7f, 0.95f)]
    [TrackClipType(typeof(DialogueClip))]
    [TrackBindingType(typeof(DialogueController))]
    public class DialogueTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<DialogueMixerBehaviour>.Create(graph, inputCount);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var controller = director.GetGenericBinding(this) as DialogueController;
            if (controller != null)
                controller.RegisterPreview(driver);
        }
    }
}
