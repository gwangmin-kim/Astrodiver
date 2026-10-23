namespace Astrodiver.Player
{
    using UnityEngine;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class PlayerSpriteAnimationController : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private PlayerInputHandler _inputHandler;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private PlayerOutline _outline;

        [Header("Animation")]
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(0.1f)] private float _framesPerSecond = 10f;
        [SerializeField, Min(0)] private int _idleFrame;
        [Tooltip("탐사 플레이어의 상승 입력도 이동 애니메이션에 포함합니다.")]
        [SerializeField] private bool _animateWhileThrusting;
        [SerializeField, Min(0f)] private float _inputThreshold = 0.1f;

        private float _frameTimer;
        private int _frameIndex;

        private void Awake()
        {
            if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
            if (_outline == null) _outline = GetComponent<PlayerOutline>();
        }

        private void OnEnable()
        {
            ResetAnimation();
        }

        private void LateUpdate()
        {
            if (_spriteRenderer == null || _frames == null || _frames.Length == 0) return;

            bool isMoving = _inputHandler != null && _inputHandler.InputEnabled &&
                (Mathf.Abs(_inputHandler.MoveInput.x) > _inputThreshold ||
                 (_animateWhileThrusting && _inputHandler.JumpHeld));
            if (!isMoving)
            {
                ResetAnimation();
                return;
            }

            _frameTimer += Time.deltaTime * Mathf.Max(0.1f, _framesPerSecond);
            int elapsedFrames = Mathf.FloorToInt(_frameTimer);
            if (elapsedFrames == 0) return;

            _frameTimer -= elapsedFrames;
            _frameIndex = (_frameIndex + elapsedFrames) % _frames.Length;
            SetFrame(_frameIndex);
        }

        private void ResetAnimation()
        {
            _frameTimer = 0f;
            if (_frames == null || _frames.Length == 0) return;
            _frameIndex = Mathf.Clamp(_idleFrame, 0, _frames.Length - 1);
            SetFrame(_frameIndex);
        }

        private void SetFrame(int index)
        {
            if (_spriteRenderer == null || _frames[index] == null ||
                _spriteRenderer.sprite == _frames[index]) return;

            _spriteRenderer.sprite = _frames[index];
            if (_outline != null) _outline.RefreshSpriteBounds(_spriteRenderer);
        }
    }
}
