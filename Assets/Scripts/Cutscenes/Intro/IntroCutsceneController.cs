using Astrodiver.SceneManagement;

namespace Astrodiver.Cutscenes.Intro
{
    using System.Collections;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.InputSystem.Controls;
    using UnityEngine.Playables;
    using UnityEngine.Timeline;

    [DisallowMultipleComponent]
    public sealed class IntroCutsceneController : MonoBehaviour
    {
        [SerializeField] private string _hubSceneName = "Hub";
        [SerializeField] private PlayableDirector _director;
        [SerializeField] private IntroPresentation _presentation;
        [Tooltip("마지막 장면 이후 입력 허용까지의 시간(unscaledTime으로 적용)")]
        [SerializeField, Min(0f)] private float _letterInputDelay = 2f;

        private readonly IntroContinueGate _gate = new();
        private SceneTransitionManager _transitions;
        private bool _playing;
        private bool _letterShown;
        private bool _failed;

        public bool InputEnabled => _gate.InputEnabled;
        public bool ContinueRequested => _gate.ContinueRequested;

        private IEnumerator Start()
        {
            if (_director == null || _director.playableAsset == null || _presentation == null)
            {
                Fail("Intro Timeline and presentation references must be assigned.");
                yield break;
            }

            _presentation.Render(IntroShot.Programming, 0f, 3f, null);
            _presentation.SetContinueState(false, false);
            _transitions = SceneTransitionManager.Instance;
            if (_transitions == null)
            {
                Fail("SceneTransitionManager is not available.");
                yield break;
            }

            // Let the menu -> intro transition finish before reserving its load queue.
            while (_transitions.IsLoading)
            {
                yield return null;
            }

            var timeline = _director.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                Fail("Intro requires a TimelineAsset.");
                yield break;
            }
            var clips = timeline.GetOutputTracks().OfType<IntroShotTrack>().SelectMany(t => t.GetClips())
                .Select(c => c.asset).OfType<IntroShotClip>().ToArray();
            if (clips.Any(c => c.textReference.IsEmpty && c.shot is IntroShot.FirstMessage or IntroShot.HopeMessage or
                IntroShot.LastMessage or IntroShot.Space or IntroShot.Letter))
            {
                Fail("Intro message clip has an empty text reference.");
                yield break;
            }
            yield return _presentation.PrepareText(clips.Select(c => c.textReference));
            if (!_presentation.TextReady)
            {
                Fail("Intro text could not be prepared.");
                yield break;
            }

            if (!_transitions.PreloadHubForIntro(_hubSceneName))
            {
                Fail($"Could not start preloading Hub scene '{_hubSceneName}'.");
                yield break;
            }

            _director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            _director.extrapolationMode = DirectorWrapMode.Hold;
            _director.time = 0d;
            _playing = true;
            _director.Play();
        }

        private void Update()
        {
            if (!_playing || _failed)
            {
                return;
            }

            // Hold keeps the final shot visible. Do not depend on stopped (Hold doesn't stop).
            if (!_letterShown && _director.time >= _director.duration - 0.001d)
            {
                _director.Pause();
                _presentation.HoldLetter();
                _gate.ShowLetter(Time.unscaledTimeAsDouble, _letterInputDelay);
                _letterShown = true;
            }

            ReadButtons(out bool held, out bool pressed);
            _gate.Tick(Time.unscaledTimeAsDouble, held, pressed);
            _presentation.SetContinueState(_gate.InputEnabled, _gate.ContinueRequested);

            if (_gate.CanTransition(_transitions.IsIntroHubReady) && _transitions.ActivateIntroHub())
            {
                _gate.MarkTransitionStarted();
                _presentation.SetContinueState(false, false);
                _playing = false;
            }
        }

        private static void ReadButtons(out bool held, out bool pressed)
        {
            held = false;
            pressed = false;
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is not Keyboard && device is not Mouse && device is not Gamepad)
                {
                    continue;
                }

                foreach (InputControl control in device.allControls)
                {
                    // Synthetic stick directions and mouse motion must not count as a key.
                    if (control is not ButtonControl button || button.synthetic)
                    {
                        continue;
                    }

                    held |= button.isPressed;
                    pressed |= button.wasPressedThisFrame;
                }
            }
        }

        private void Fail(string message)
        {
            _failed = true;
            Debug.LogError($"IntroCutsceneController: {message}", this);
            if (_presentation != null)
            {
                _presentation.ShowError();
            }
        }
    }
}
