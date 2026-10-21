using UnityEngine;

namespace Desayuno64.TimelineDialogue
{
    public abstract class DialogueLineResolver : MonoBehaviour
    {
        public abstract string Resolve(string authored, string key);
    }
}
