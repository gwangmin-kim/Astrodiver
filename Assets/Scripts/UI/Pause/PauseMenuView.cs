using UnityEngine;
using UnityEngine.UI;

namespace Astrodiver.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _exitButton;

        public Button ContinueButton => _continueButton;
        public Button ExitButton => _exitButton;
    }
}
