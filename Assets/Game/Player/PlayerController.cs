using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Player
{
    [RequireComponent(typeof(BoxCollider2D), typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform _obstacleRayRight = null;
        [SerializeField] private Transform _obstacleRayLeft = null;
        [Space]
        [SerializeField] private bool _allowDoubleJump, _allowDash, _allowCrouch;

        private Rigidbody2D _rigidbody;
        private BoxCollider2D _collider;
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        private float _currentHorizontalSpeed, _currentVerticalSpeed;
        private int _fixedFrame;
        public bool Grounded => _grounded;
        public float VerticalSpeed => _currentVerticalSpeed;

        public FrameInput Input { get; private set; }
        public Vector3 RawMovement { get; private set; }
        public Vector2 LastDirection { get; private set; }
        public Transform ObstacleRayTransform { get; private set; }


        public event Action<bool> OnGroundedChanged;
        public event Action OnJumping, OnDoubleJumping;
        public event Action<bool> OnDashingChanged;
        public event Action<bool> OnCrouchingChanged;

        void Awake()
        {
            GatherInput();
            _rigidbody = GetComponent<Rigidbody2D>();
            _collider = GetComponent<BoxCollider2D>();

            _defaultColliderSize = _collider.size;
            _defaultColliderOffset = _collider.offset;
            ObstacleRayTransform = _obstacleRayRight;
        }

        private void Update() => GatherInput();

        void FixedUpdate()
        {
            _fixedFrame++;
            _frameClamp = _moveClamp;

            _velocity = (_rigidbody.position - (Vector2)_lastPosition) / Time.fixedDeltaTime;
            _lastPosition = _rigidbody.position;

            RunCollisionChecks();

            CalculateCrouch();
            CalculateHorizontal();
            CalculateJumpApex();
            CalculateGravity();
            CalculateJump();
            CalculateDash(new Vector2(Input.X, _grounded && Input.Y < 0 ? 0 : Input.Y), _dashLength);
            MoveCharacter();
        }

        private void OnEnable()
        {
            //_targetSearcher.OnTargetLocked += OnTargetLocked;   
        }

        private void OnDisable()
        {
           // _targetSearcher.OnTargetLocked -= OnTargetLocked;
        }

        private void GatherInput()
        {
            Input = new FrameInput
            {
                JumpDown = UnityEngine.Input.GetButtonDown("Jump"),
                JumpHeld = UnityEngine.Input.GetButton("Jump"),
                DashDown = UnityEngine.Input.GetButtonDown("Dash"),
                //ConsumeDown = UnityEngine.Input.GetButtonDown("Consume"),

                X = UnityEngine.Input.GetAxisRaw("Horizontal"),
                Y = UnityEngine.Input.GetAxisRaw("Vertical")
            };

            if (Input.DashDown)
            {
                _dashToConsume = true;
            }

            if (Input.JumpDown)
            {
                _lastJumpPressed = _fixedFrame;
                _jumpToConsume = true;
            }

            //if(Input.ConsumeDown)
            //{
                //ConsumeEnemy();
            //}

            if(Input.X == 1)
            {
                ObstacleRayTransform = _obstacleRayRight;
                LastDirection = new Vector2(1, 0);
            }

            if(Input.X == -1)
            {
                ObstacleRayTransform = _obstacleRayLeft;
                LastDirection = new Vector2(-1, 0);
            }
        }

        #region Collisions

        [Header("COLLISION")]
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private float _detectionRayLength = 0.1f;
        [SerializeField] private ContactFilter2D _filter;
        private readonly RaycastHit2D[] _hitsDown = new RaycastHit2D[1];
        private readonly RaycastHit2D[] _hitsUp = new RaycastHit2D[1];
        private readonly RaycastHit2D[] _hitsLeft = new RaycastHit2D[1];
        private readonly RaycastHit2D[] _hitsRight = new RaycastHit2D[1];

        private bool _hittingCeiling, _grounded, _colRight, _colLeft;
        private float _timeLeftGrounded;

        private void RunCollisionChecks()
        {
            var b = _collider.bounds;

            var groundedCheck = RunDetection(Vector2.down, _hitsDown, true);

            if (_grounded && !groundedCheck)
            {
                _timeLeftGrounded = _fixedFrame;
                OnGroundedChanged?.Invoke(false);
            }
            else if (!_grounded && groundedCheck)
            {
                _coyoteUsable = true;
                _executedBufferedJump = false;
                _doubleJumpUsable = true;
                _canDash = true;
                OnGroundedChanged?.Invoke(true);
            }

            _grounded = groundedCheck;
            _colLeft = RunDetection(Vector2.left, _hitsLeft);
            _colRight = RunDetection(Vector2.right, _hitsRight);

            _hittingCeiling = RunDetection(Vector2.up, _hitsUp);

            bool RunDetection(Vector2 dir, RaycastHit2D[] hits, bool clearBeforeUse = false)
            {
                if (clearBeforeUse) Array.Clear(hits, 0, hits.Length);
                return Physics2D.BoxCastNonAlloc(b.center, b.size, 0, dir, hits, _detectionRayLength, _groundLayer) > 0;
            }
        }

        private void OnDrawGizmos()
        {
            if (!_collider) _collider = GetComponent<BoxCollider2D>();

            Gizmos.color = Color.blue;
            var b = _collider.bounds;
            b.Expand(_detectionRayLength);

            Gizmos.DrawWireCube(b.center, b.size);
        }

        #endregion

        #region Crouch

        [Header("CROUCH")]
        [SerializeField] private float _crouchSizeModifier = 0.5f;
        [SerializeField] private float _crouchSpeedModifier = 0.1f;
        [SerializeField] private int _crouchSlowdownFrames = 50;
        [SerializeField] private float _immediateCrouchSlowdownThreshold = 0.1f;

        private Vector2 _defaultColliderSize, _defaultColliderOffset;
        private float _velocityOnCrouch;
        private bool _crouching;
        private int _frameStartedCrouching;

        private bool CanStand => Physics2D.OverlapBox((Vector2)transform.position + _defaultColliderOffset, _defaultColliderSize * 0.95f, 0, _groundLayer) == null;

        void CalculateCrouch()
        {
            if (!_allowCrouch)
                return;

            if (_crouching)
            {
                var immediate = _velocityOnCrouch <= _immediateCrouchSlowdownThreshold ? _crouchSlowdownFrames : 0;
                var crouchPoint = Mathf.InverseLerp(0, _crouchSlowdownFrames, _fixedFrame - _frameStartedCrouching + immediate);
                _frameClamp *= Mathf.Lerp(1, _crouchSpeedModifier, crouchPoint);
            }

            if (_grounded && Input.Y < 0 && !_crouching)
            {
                _crouching = true;
                OnCrouchingChanged?.Invoke(true);
                _velocityOnCrouch = Mathf.Abs(_velocity.x);
                _frameStartedCrouching = _fixedFrame;

                _collider.size = _defaultColliderSize * new Vector2(1, _crouchSizeModifier);

                var difference = _defaultColliderSize.y - (_defaultColliderSize.y * _crouchSizeModifier);
                _collider.offset = -new Vector2(0, difference * 0.5f);
            }
            else if (!_grounded || (Input.Y >= 0 && _crouching))
            {
                if (!CanStand)
                    return;

                _crouching = false;
                OnCrouchingChanged?.Invoke(false);

                _collider.size = _defaultColliderSize;
                _collider.offset = _defaultColliderOffset;
            }
        }

        #endregion

        #region Horizontal

        [Header("WALKING")]
        [SerializeField] private float _acceleration = 120;
        [SerializeField] private float _moveClamp = 13;
        [SerializeField] private float _deceleration = 60f;
        [SerializeField] private float _apexBonus = 100;

        private float _frameClamp;

        private void CalculateHorizontal()
        {
            if (Input.X != 0)
            {
                _currentHorizontalSpeed += Input.X * _acceleration * Time.fixedDeltaTime;
                //Debug.Log("1" + _currentHorizontalSpeed);

                _currentHorizontalSpeed = Mathf.Clamp(_currentHorizontalSpeed, -_frameClamp, _frameClamp);
                
                //Debug.Log("2" + _currentHorizontalSpeed);
                var apexBonus = Mathf.Sign(Input.X) * _apexBonus * _apexPoint;
                _currentHorizontalSpeed += apexBonus * Time.fixedDeltaTime;
                //Debug.Log("3" + _currentHorizontalSpeed);
            }
            else
            {
                _currentHorizontalSpeed = Mathf.MoveTowards(_currentHorizontalSpeed, 0, _deceleration * Time.fixedDeltaTime);
            }

            if (_currentHorizontalSpeed > 0 && _colRight || _currentHorizontalSpeed < 0 && _colLeft)
            {
                //_currentHorizontalSpeed = 0;
            }
        }

        #endregion

        #region Gravity

        [Header("GRAVITY")]
        [SerializeField] private float _fallClamp = -60f;
        [SerializeField] private float _minFallSpeed = 80f;
        [SerializeField] private float _maxFallSpeed = 160f;
        [SerializeField] private float _groundedSpeed = -5;
        private float _fallSpeed;

        private void CalculateGravity()
        {
            if (_grounded)
            {
                if (_currentVerticalSpeed < 0)
                {
                    _currentVerticalSpeed = 0;
                }
            }
            else
            {
                var fallSpeed = _endedJumpEarly && _currentVerticalSpeed > 0 ? _fallSpeed * _jumpEndEarlyGravityModifier : _fallSpeed;

                _currentVerticalSpeed -= fallSpeed * Time.fixedDeltaTime;

                if (_currentVerticalSpeed < _fallClamp)
                {
                    _currentVerticalSpeed = _fallClamp;
                }
            }
        }

        #endregion

        #region Jump

        [Header("JUMPING")] [SerializeField] private float _jumpHeight = 35;
        [SerializeField] private float _jumpApexThreshold = 40f;
        [SerializeField] private int _coyoteTimeThreshold = 7;
        [SerializeField] private int _jumpBuffer = 7;
        [SerializeField] private float _jumpEndEarlyGravityModifier = 3;

        private bool _jumpToConsume;
        private bool _coyoteUsable;
        private bool _executedBufferedJump;
        private bool _endedJumpEarly = true;
        private float _apexPoint;
        private float _lastJumpPressed = Single.MinValue;
        private bool _doubleJumpUsable;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _timeLeftGrounded + _coyoteTimeThreshold > _fixedFrame;
        private bool HasBufferedJump => ((_grounded && !_executedBufferedJump) || _cornerStuck) && _lastJumpPressed + _jumpBuffer > _fixedFrame;
        private bool CanDoubleJump => _allowDoubleJump && _doubleJumpUsable && !_coyoteUsable;

        private void CalculateJumpApex()
        {
            if (!_grounded)
            {

                _apexPoint = Mathf.InverseLerp(_jumpApexThreshold, 0, Mathf.Abs(_velocity.y));
                _fallSpeed = Mathf.Lerp(_minFallSpeed, _maxFallSpeed, _apexPoint);
            }
            else
            {
                _apexPoint = 0;
            }
        }

        private void CalculateJump()
        {
            //Debug.Log("jumping");
            if (_crouching && !CanStand) return;

            if (_jumpToConsume && CanDoubleJump)
            {
                _currentVerticalSpeed = _jumpHeight;
                _doubleJumpUsable = false;
                _endedJumpEarly = false;
                _jumpToConsume = false;
                OnDoubleJumping?.Invoke();
            }

            if ((_jumpToConsume && CanUseCoyote) || HasBufferedJump)
            {
                _currentVerticalSpeed = _jumpHeight;
                _endedJumpEarly = false;
                _coyoteUsable = false;
                _jumpToConsume = false;
                _timeLeftGrounded = _fixedFrame;
                _executedBufferedJump = true;
                OnJumping?.Invoke();
            }

            if (!_grounded && !Input.JumpHeld && !_endedJumpEarly && _velocity.y > 0)
                _endedJumpEarly = true;

           // if (_hittingCeiling && _currentVerticalSpeed > 0)
            //    _currentVerticalSpeed = 0;
        }

        #endregion

        #region Dash

        [Header("DASH")]
        [SerializeField] private float _dashPower = 30;
        [SerializeField] private int _dashLength = 6;
        [SerializeField] private float _dashEndHorizontalMultiplier = 0.25f;
        private float _startedDashing;
        private bool _canDash;
        private Vector2 _dashVel;

        private bool _dashing;
        private bool _dashToConsume;

        void CalculateDash(Vector2 vel, int dashLength)
        {
            if (!_allowDash)
                return;

            if (_dashToConsume && _canDash && !_crouching)
            {
                if (vel == Vector2.zero) return;
                _dashVel = vel * _dashPower;
                _dashing = true;
                OnDashingChanged?.Invoke(true);
                _canDash = false;
                _startedDashing = _fixedFrame;
            }

            if (_dashing)
            {
                //_currentHorizontalSpeed = _dashVel.x;
                _currentVerticalSpeed = _dashVel.y;

                if (_startedDashing + dashLength < _fixedFrame)
                {
                    _dashing = false;
                    OnDashingChanged?.Invoke(false);

                    if (_currentVerticalSpeed > 0)
                    {
                        _currentVerticalSpeed = 0;
                    }

                    //_currentHorizontalSpeed *= _dashEndHorizontalMultiplier;

                    if (_grounded)
                    {
                        _canDash = true;
                    }
                }
            }

            _dashToConsume = false;
        }

        #endregion

        #region Move

        private void MoveCharacter()
        {
            RawMovement = new Vector3(_currentHorizontalSpeed, _currentVerticalSpeed);
            var move = RawMovement * Time.fixedDeltaTime;
            
            move -= EvaluateEffectors();

            _rigidbody.MovePosition(_rigidbody.position + (Vector2)move);

            RunCornerPrevention();
        }

        #region Corner Stuck Prevention

        private Vector2 _lastPos;
        private bool _cornerStuck;

        void RunCornerPrevention()
        {
            _cornerStuck = !_grounded && _lastPos == _rigidbody.position && _lastJumpPressed + 1 < _fixedFrame;
            _currentVerticalSpeed = _cornerStuck ? 0 : _currentVerticalSpeed;
            _lastPos = _rigidbody.position;
        }

        #endregion

        #endregion

        #region Effectors

        //private readonly List<IPlayerEffector> _usedEffectors = new List<IPlayerEffector>();

        private Vector3 EvaluateEffectors()
        {
            var effectorDirection = Vector3.zero;

            effectorDirection += Process(_hitsDown);

            //_usedEffectors.Clear();
            return effectorDirection;

            Vector3 Process(IEnumerable<RaycastHit2D> hits)
            {
                foreach (var hit2D in hits)
                {
                    if (!hit2D.transform) return Vector3.zero;
                    //if (hit2D.transform.TryGetComponent(out IPlayerEffector effector))
                    //{
                    //    if (_usedEffectors.Contains(effector)) continue;
                     //   _usedEffectors.Add(effector);
                     //   return effector.EvaluateEffector();
                   // }
                }

                return Vector3.zero;
            }
        }

        #endregion

        #region EnemyInteraction

        [Header("ConsumeEnemy")]
        //[SerializeField] private TargetSearcher _targetSearcher;
        [SerializeField] private float _distanceToAttack;
        [SerializeField] private float _upForce = 0.1f;
        [SerializeField] private Image _bloodScreen;
        [SerializeField] private int _bonusDistanceToKill;

        //private Enemy _target;
        private float _distanceToTarget;
        private WaitForSeconds _bleachBloodTimer = new WaitForSeconds(0.05f);
        private float _bleachAmount;

        /*private void OnTargetLocked(Enemy target, float distance)
        {
            _target = target;
            _distanceToTarget = distance;
        }

        private void ConsumeEnemy()
        {
            _bleachAmount = 1;
        
            if (_target != null && _allowDash && _crouching == false)
            {
                if(_distanceToTarget > _distanceToAttack)
                {
                    _dashToConsume = true;
                    CalculateDash(LastDirection, (int)_distanceToTarget + _bonusDistanceToKill);
                    _rigidbody.AddForce(new Vector2(0, _upForce));
                }
                else
                {
                    // Attack Enemy
                }

                StartCoroutine(BleachBlood());

                if (_target.TryGetComponent<EnemyScript>(out EnemyScript enemy))
                {
                    enemy.Die();
                }
            }
        }*/

        private IEnumerator BleachBlood()
        {
            yield return _bleachBloodTimer;
            _bleachAmount -= 0.05f;
            Color bleacher = new Color(1, 0.55f, 0.55f, _bleachAmount);
            _bloodScreen.material.color = bleacher;
            if (_bleachAmount >0 )
                StartCoroutine(BleachBlood());
        }

        #endregion

        public void StopPlayer()
        {
            _rigidbody.velocity = Vector2.zero;
        }
    }
}