using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Astrodiver.UI.MainMenu
{
    [RequireComponent(typeof(Button))]
    public sealed class QuitButtonFocusHandler : MonoBehaviour, IPointerExitHandler, IDeselectHandler
    {
        [SerializeField] private MainMenuController _mainMenuController;

        public void OnPointerExit(PointerEventData eventData)
        {
            CancelQuitConfirmation();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            CancelQuitConfirmation();
        }

        private void CancelQuitConfirmation()
        {
            if (_mainMenuController != null)
            {
                _mainMenuController.CancelQuitConfirmation();
            }
        }
    }
}
