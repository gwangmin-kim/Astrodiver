namespace Astrodiver.Cutscenes.Intro
{
    using UnityEngine;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [TrackColor(0.5f, 0.8f, 0.6f)]
    [TrackClipType(typeof(IntroShotClip))]
    [TrackBindingType(typeof(IntroPresentation))]
    public sealed class IntroShotTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<IntroShotMixer>.Create(graph, inputCount);
        }
    }

    public sealed class IntroShotMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData is not IntroPresentation view)
            {
                return;
            }

            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                if (playable.GetInputWeight(i) <= 0f)
                {
                    continue;
                }

                var clip = (ScriptPlayable<IntroShotBehaviour>)playable.GetInput(i);
                IntroShotBehaviour data = clip.GetBehaviour();
                view.Render(data.Shot, (float)clip.GetTime(), (float)clip.GetDuration(), data.Text);
                break;
            }
        }
    }
}
