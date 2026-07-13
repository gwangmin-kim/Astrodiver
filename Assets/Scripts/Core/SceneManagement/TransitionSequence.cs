using System;
using UnityEngine;
using UnityEngine.Events;

public class TransitionSequence : MonoBehaviour
{
    [SerializeField] private UnityEvent _transitionStarted;

    private Action _onCompleted;
    private bool _isPlaying;

    public void Play(Action onCompleted)
    {
        if (_isPlaying)
        {
            return;
        }

        _isPlaying = true;
        _onCompleted = onCompleted;
        _transitionStarted?.Invoke();
    }

    // Call from an Animation Event, Timeline signal, or another transition effect callback.
    public void CompleteTransition()
    {
        if (!_isPlaying)
        {
            return;
        }

        _isPlaying = false;
        Action callback = _onCompleted;
        _onCompleted = null;
        callback?.Invoke();
    }
}
