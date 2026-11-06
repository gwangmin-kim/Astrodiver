namespace Astrodiver.Cutscenes.Intro
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;
    using UnityEngine.Localization;

    public enum IntroShot
    {
        Programming,
        FirstMessage,
        EarthMemory,
        Scientist,
        HopeMessage,
        LastMessage,
        Space,
        Complete,
        Letter
    }

    public sealed class IntroShotClip : PlayableAsset, ITimelineClipAsset
    {
        public IntroShot shot;
        public LocalizedString textReference = new();

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<IntroShotBehaviour>.Create(graph);
            playable.GetBehaviour().Shot = shot;
            playable.GetBehaviour().Text = textReference;
            return playable;
        }
    }

    public sealed class IntroShotBehaviour : PlayableBehaviour
    {
        public IntroShot Shot;
        public LocalizedString Text;
    }
}
