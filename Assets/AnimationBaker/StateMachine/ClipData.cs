using UnityEngine;
using AnimationBaker.StateMachine.Nodes;

namespace AnimationBaker.StateMachine
{
    [System.Serializable]
    public class ClipData
    {
        public int Index;
        public float Duration;
        public float FrameRate;
        public StateNode Node;
        public string Name;
        public WrapMode WrapMode;
    }
}
