namespace Astrodiver.Player
{
    using UnityEngine;
    using System.Collections.Generic;

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputHandler))]
    public sealed class PlayerSpriteAnimationController : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private PlayerInputHandler _inputHandler;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private PlayerOutline _outline;
        [SerializeField] private Rigidbody2D _rigidbody;

        [Header("Animation")]
        [SerializeField] private Sprite[] _frames;
        [SerializeField, Min(0.1f)] private float _framesPerSecond = 10f;
        [SerializeField, Min(0)] private int _idleFrame;
        [SerializeField, Min(0)] private int _jumpFrame;
        [Tooltip("탐사: 공중에서 좌우 입력이 있을 때 재생하고 기울입니다. 허브: 공중에서 Jump Frame으로 고정합니다.")]
        [SerializeField] private bool _animateWhileThrusting;
        [SerializeField, Min(0f)] private float _inputThreshold = 0.1f;

        [Header("Airborne Tilt")]
        [SerializeField, Min(0f)] private float _rotationAngle = 10f;
        [SerializeField, Range(0f, 1f)] private float _groundNormalThreshold = 0.5f;

        private float _frameTimer;
        private int _frameIndex;
        private readonly List<ContactPoint2D> _contacts = new();
        private Quaternion _defaultLocalRotation;

        private void Awake()
        {
            if (_inputHandler == null) _inputHandler = GetComponent<PlayerInputHandler>();
            if (_outline == null) _outline = GetComponent<PlayerOutline>();
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody2D>();
            if (_spriteRenderer != null) _defaultLocalRotation = _spriteRenderer.transform.localRotation;
        }

        private void OnEnable()
        {
            ResetAnimation();
        }

        private void LateUpdate()
        {
            if (_spriteRenderer == null || _frames == null || _frames.Length == 0) return;

            bool isGrounded = IsGrounded();
            UpdateAnimation(Time.deltaTime, isGrounded);
            float horizontal = _inputHandler != null && _inputHandler.InputEnabled
                ? _inputHandler.MoveInput.x : 0f;
            float angle = _animateWhileThrusting && !isGrounded && Mathf.Abs(horizontal) > _inputThreshold
                ? -Mathf.Sign(horizontal) * _rotationAngle : 0f;
            _spriteRenderer.transform.localRotation = _defaultLocalRotation * Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDisable()
        {
            if (_spriteRenderer != null) _spriteRenderer.transform.localRotation = _defaultLocalRotation;
        }

        private bool IsGrounded()
        {
            if (_rigidbody == null || !_rigidbody.simulated) return false;
            // Jump velocity changes before the next physics step removes the old floor contacts.
            if (_rigidbody.linearVelocity.y > 0.1f) return false;
            _rigidbody.GetContacts(_contacts);
            foreach (ContactPoint2D contact in _contacts)
            {
                if (contact.normal.y >= _groundNormalThreshold) return true;
            }
            return false;
        }

        private void UpdateAnimation(float deltaTime, bool isGrounded)
        {
            if (!_animateWhileThrusting && !isGrounded)
            {
                _frameTimer = 0f;
                _frameIndex = Mathf.Clamp(_jumpFrame, 0, _frames.Length - 1);
                SetFrame(_frameIndex);
                return;
            }

            bool isMoving = _inputHandler != null && _inputHandler.InputEnabled &&
                (Mathf.Abs(_inputHandler.MoveInput.x) > _inputThreshold ||
                 (_animateWhileThrusting && isGrounded && _inputHandler.JumpHeld));
            if (!isMoving)
            {
                ResetAnimation();
                return;
            }

            _frameTimer += deltaTime * Mathf.Max(0.1f, _framesPerSecond);
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
