namespace Astrodiver.Cutscenes.Intro
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Astrodiver.Localization;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Localization;
    using UnityEngine.Localization.Settings;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    [DisallowMultipleComponent]
    public sealed class IntroPresentation : MonoBehaviour
    {
        [Header("Shot roots")]
        [SerializeField] private GameObject _terminalRoot;
        [SerializeField] private GameObject _memoryRoot;
        [SerializeField] private GameObject _scientistRoot;
        [SerializeField] private GameObject _spaceRoot;
        [SerializeField] private GameObject _letterRoot;

        [Header("Earth layers")]
        [SerializeField] private RectTransform _memoryComposition;
        [SerializeField] private CanvasGroup _ruins;
        [SerializeField] private CanvasGroup _cockpit;
        [SerializeField] private RectTransform _scientistComposition;

        [Header("Replaceable space objects")]
        [SerializeField] private RectTransform _ship;
        [SerializeField] private RectTransform _nearPlanet;
        [SerializeField] private RectTransform _farPlanet;

        [Header("Procedural graphics")]
        [SerializeField] private IntroProceduralGraphic _progress;
        [SerializeField] private IntroProceduralGraphic _glitch;
        [SerializeField] private IntroProceduralGraphic _stars;
        [SerializeField] private IntroProceduralGraphic _letterFrame;
        [SerializeField] private UnityEngine.UI.Image _fade;

        [Header("Text")]
        [SerializeField] private TMP_Text _programLabel;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private TMP_Text _letter;
        [SerializeField] private TMP_Text _continuePrompt;
        [SerializeField, Min(1f)] private float _charactersPerSecond = 18f;
        [SerializeField] private IntroTextResources _textResources;

        private readonly Dictionary<LocalizedString, string> _textCache = new();
        private readonly List<AsyncOperationHandle> _tableHandles = new();
        private LocalizedString[] _references;
        private LocalizedString _letterReference;
        private LocalizedString _lastReference;
        private IntroShot _lastShot;
        private float _lastTime;
        private float _lastDuration;
        private bool _hasShot;
        private bool _promptEnabled;
        private bool _waitingForHub;
        private Coroutine _refreshRoutine;
        public bool TextReady { get; private set; }

        private void OnEnable() => LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;

        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
            if (_refreshRoutine != null) StopCoroutine(_refreshRoutine);
            _refreshRoutine = null;
        }

        private void OnDestroy()
        {
            foreach (var handle in _tableHandles)
                if (handle.IsValid()) Addressables.Release(handle);
            _tableHandles.Clear();
        }

        public IEnumerator PrepareText(IEnumerable<LocalizedString> clipReferences)
        {
            TextReady = false;
            if (_textResources == null)
            {
                Debug.LogError("Intro text resources are not assigned.", this);
                yield break;
            }
            _references = _textResources.References.Concat(clipReferences.Where(r => r != null && !r.IsEmpty)).Distinct().ToArray();
            if (_textResources.References.Any(r => r == null || r.IsEmpty))
            {
                Debug.LogError("Intro text resources contain an empty reference.", this);
                yield break;
            }
            yield return GameText.Prepare(true, _tableHandles, _references.Select(r => r.TableReference).Distinct().ToArray());
            _textCache.Clear();
            foreach (LocalizedString reference in _references)
            {
                if (!GameText.TryResolve(reference, out string value) || string.IsNullOrWhiteSpace(value))
                {
                    Debug.LogError($"Could not prepare intro text: {reference.TableEntryReference}.", this);
                    yield break;
                }
                _textCache[reference] = value;
            }
            TextReady = true;
        }

        private void HandleLocaleChanged(Locale _)
        {
            _textCache.Clear();
            if (!Application.isPlaying || _references == null || !TextReady) return;
            if (_refreshRoutine != null) StopCoroutine(_refreshRoutine);
            _refreshRoutine = StartCoroutine(RefreshLocale());
        }

        private IEnumerator RefreshLocale()
        {
            yield return PrepareText(_references);
            if (TextReady && _hasShot)
            {
                Render(_lastShot, _lastTime, _lastDuration, _lastReference);
                SetContinueState(_promptEnabled, _waitingForHub);
            }
            _refreshRoutine = null;
        }

        private string Text(LocalizedString reference)
        {
            if (reference == null || reference.IsEmpty) return string.Empty;
            if (_textCache.TryGetValue(reference, out string value)) return value;
            // Also supports Timeline scrubbing in Edit Mode without a blocking table load.
            if (GameText.TryResolve(reference, out value)) _textCache[reference] = value;
            return value;
        }

        public void Render(IntroShot shot, float time, float duration, LocalizedString reference)
        {
            _lastShot = shot;
            _lastTime = time;
            _lastDuration = duration;
            _lastReference = reference;
            _hasShot = true;
            string text = Text(reference);
            float t = Mathf.Clamp01(time / Mathf.Max(0.001f, duration));
            bool programming = shot == IntroShot.Programming || shot == IntroShot.Complete;
            bool memory = shot == IntroShot.EarthMemory;
            bool scientist = shot == IntroShot.Scientist;
            bool space = shot == IntroShot.Space;
            bool letter = shot == IntroShot.Letter;
            bool message = shot == IntroShot.FirstMessage || shot == IntroShot.HopeMessage ||
                shot == IntroShot.LastMessage || space;

            _terminalRoot.SetActive(programming);
            _memoryRoot.SetActive(memory);
            _scientistRoot.SetActive(scientist);
            _spaceRoot.SetActive(space);
            _letterRoot.SetActive(letter);
            _message.gameObject.SetActive(message);

            float glitchStrength = shot == IntroShot.FirstMessage || shot == IntroShot.HopeMessage ? 0.8f :
                shot == IntroShot.LastMessage ? 0.65f :
                shot == IntroShot.Complete ? (1f - Mathf.Clamp01(time / 0.7f)) * 0.6f : 0f;
            _glitch.gameObject.SetActive(glitchStrength > 0f);
            _glitch.SetState(time, glitchStrength);

            float fade = 0f;
            if (programming)
            {
                bool complete = shot == IntroShot.Complete;
                _programLabel.text = Text(complete ? _textResources?.programmingComplete : _textResources?.programmingRunning);
                _progress.SetState(time, complete ? 1f : Mathf.Lerp(0.04f, 0.46f, t));
            }

            if (message)
            {
                SetTypedText(_message, text, space ? float.MaxValue : time);
                if (shot == IntroShot.FirstMessage)
                {
                    fade = Mathf.Clamp01((time - duration + 0.6f) / 0.6f);
                }
            }

            if (memory)
            {
                // All four source layers share the same 320x180 registration.
                // The full-composition reference PNG is deliberately not used.
                float zoom = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.8f));
                _memoryComposition.localScale = Vector3.one * Mathf.Lerp(1.8f, 1f, zoom);
                _ruins.alpha = Mathf.Clamp01(time / 1.8f);
                _cockpit.alpha = Mathf.Clamp01((t - 0.3f) / 0.3f);
                fade = 1f - Mathf.Clamp01(time / 0.7f);
            }

            if (scientist)
            {
                _scientistComposition.localScale = Vector3.one * Mathf.Lerp(1f, 1.07f, t);
                fade = Mathf.Clamp01((time - duration + 0.35f) / 0.35f);
            }

            if (space)
            {
                _stars.SetState(time, 1f);
                _ship.anchoredPosition = new Vector2(Mathf.Lerp(-1200f, 1200f, t), -60f);
                _nearPlanet.anchoredPosition = new Vector2(Mathf.Lerp(-1000f, 1000f, t), -490f);
                _farPlanet.anchoredPosition = new Vector2(Mathf.Lerp(-700f, 700f, t), 390f);
                fade = 1f - Mathf.Clamp01(time / 0.5f);
            }

            if (letter)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    _letterReference = reference;
                }

                _letterFrame.SetState(time, Mathf.Clamp01(time / 1.1f));
                SetTypedText(_letter, Text(_letterReference), Mathf.Max(0f, time - 1.1f));
            }

            _fade.color = new Color(0f, 0f, 0f, fade);
        }

        public void HoldLetter()
        {
            Render(IntroShot.Letter, 1000f, 1000f, _letterReference);
        }

        public void SetContinueState(bool enabled, bool waitingForHub)
        {
            _promptEnabled = enabled;
            _waitingForHub = waitingForHub;
            bool visible = enabled || waitingForHub;
            _continuePrompt.gameObject.SetActive(visible);
            if (visible)
            {
                _continuePrompt.text = Text(waitingForHub ? _textResources?.loadingPrompt : _textResources?.continuePrompt);
            }
        }

        public void ShowError()
        {
            _continuePrompt.gameObject.SetActive(true);
            _continuePrompt.text = Text(_textResources?.hubLoadError);
        }

        private void SetTypedText(TMP_Text target, string text, float seconds)
        {
            text ??= string.Empty;
            if (target.text != text)
            {
                target.text = text;
            }

            target.maxVisibleCharacters = seconds >= text.Length / _charactersPerSecond
                ? int.MaxValue
                : Mathf.FloorToInt(seconds * _charactersPerSecond);
        }
    }
}
