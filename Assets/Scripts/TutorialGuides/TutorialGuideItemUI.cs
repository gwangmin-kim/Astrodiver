using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TutorialGuideItemUI : MonoBehaviour
{
    [Header("Prefab References")]
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Animator _animator;

    [Header("Animation")]
    [SerializeField, Min(0.01f)] private float _completionDuration = 0.3f;

    private static readonly int CompleteTrigger = Animator.StringToHash("Complete");

    public void Bind(string text)
    {
        if (_text != null) _text.text = text;
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;
        if (_animator != null && _animator.isActiveAndEnabled)
        {
            _animator.Rebind();
            _animator.Update(0f);
        }
    }

    public IEnumerator PlayCompletionAnimation()
    {
        _animator?.SetTrigger(CompleteTrigger);
        yield return new WaitForSecondsRealtime(_completionDuration);
    }
}
