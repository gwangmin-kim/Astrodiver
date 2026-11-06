namespace Astrodiver.Cutscenes.Intro
{
    /// <summary>Intro-only input gate. Early/held input cannot dismiss the letter.</summary>
    public sealed class IntroContinueGate
    {
        private double _enableAt;
        private bool _hasLetter;
        private bool _released;

        public bool InputEnabled { get; private set; }
        public bool ContinueRequested { get; private set; }
        public bool TransitionStarted { get; private set; }

        public void ShowLetter(double now, double delay)
        {
            _hasLetter = true;
            _enableAt = now + System.Math.Max(0d, delay);
            _released = false;
            InputEnabled = false;
            ContinueRequested = false;
            TransitionStarted = false;
        }

        public void Tick(double now, bool anyHeld, bool anyPressed)
        {
            if (!_hasLetter || ContinueRequested || TransitionStarted || now < _enableAt)
            {
                return;
            }

            if (!InputEnabled)
            {
                InputEnabled = true;
                // Ignore the activation frame itself, including a press on that frame.
                _released = !anyHeld && !anyPressed;
                return;
            }

            if (!_released)
            {
                _released = !anyHeld && !anyPressed;
                return;
            }

            if (anyPressed)
            {
                ContinueRequested = true;
                InputEnabled = false;
            }
        }

        public bool CanTransition(bool hubReady)
        {
            return ContinueRequested && hubReady && !TransitionStarted;
        }

        public void MarkTransitionStarted()
        {
            TransitionStarted = true;
            InputEnabled = false;
        }
    }
}
