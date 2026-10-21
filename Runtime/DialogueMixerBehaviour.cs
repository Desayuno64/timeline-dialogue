using UnityEngine;
using UnityEngine.Playables;

namespace Desayuno64.TimelineDialogue
{
    public class DialogueMixerBehaviour : PlayableBehaviour
    {
        DialogueController controller;
        bool visible;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            controller = playerData as DialogueController;
            if (controller == null)
                return;

            int activeIndex = -1;
            float maxWeight = 0f;
            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= maxWeight)
                    continue;

                maxWeight = weight;
                activeIndex = i;
            }

            if (activeIndex < 0 || maxWeight <= 0f)
            {
                if (visible)
                {
                    controller.Hide();
                    visible = false;
                }

                return;
            }

            var input = (ScriptPlayable<DialogueBehaviour>)playable.GetInput(activeIndex);
            DialogueBehaviour behaviour = input.GetBehaviour();
            controller.Present(activeIndex + 1, behaviour, GetRevealProgress(input, behaviour), input.GetTime(), input.GetDuration());
            visible = true;
        }

        public override void OnGraphStop(Playable playable)
        {
            if (controller != null && Application.isPlaying)
                controller.Hide();

            visible = false;
        }

        static float GetRevealProgress(ScriptPlayable<DialogueBehaviour> input, DialogueBehaviour behaviour)
        {
            if (behaviour.instant || behaviour.revealMode == DialogueRevealMode.CharactersPerSecond)
                return 1f;

            double duration = input.GetDuration();
            float normalized = duration > 0.0001d
                ? Mathf.Clamp01((float)(input.GetTime() / duration))
                : 1f;

            if (behaviour.revealCurve == null || behaviour.revealCurve.length == 0)
                return normalized;

            return Mathf.Clamp01(behaviour.revealCurve.Evaluate(normalized));
        }
    }
}
