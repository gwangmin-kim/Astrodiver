using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class PauseMenuView : MonoBehaviour
{
    [SerializeField] private Button _continueButton;
    [SerializeField] private Button _exitButton;

    public Button ContinueButton => _continueButton;
    public Button ExitButton => _exitButton;
}
