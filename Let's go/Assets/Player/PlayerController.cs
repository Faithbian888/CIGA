using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace Player
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D playerRigidbody2D;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Collider2D playerCollider;
        [SerializeField] private Vector3 targetScaleS;
        [SerializeField] private Vector3 targetScaleL;
        [SerializeField] private float maxUpSpeed;//向上移动最大速度
        [SerializeField] private float maxDownSpeed;//向下移动最大速度
        [SerializeField] private float upAddSpeed;//向上加速度
        [SerializeField] private float downAddSpeed;//向下加速度
        [SerializeField] private float verticalSpeed;//水平速度
        [SerializeField] private bool isBoosting;
        [Header("高级设置")]
        [SerializeField] private bool applyGravity = false; // 是否应用重力
        [SerializeField] private float dragFactor = 0.95f; // 无输入时的阻力系数
        [SerializeField] private AnimationCurve forceCurve; // 力应用曲线
        
        private Tween _scaleTween;
        private bool _isFacingRight;
        private float inputHor;
        private PlayerMoveType _playerMoveType;
        private bool _isLimitSize;
        private bool _isScaling;
        private Coroutine _sizeChangeRoutine;
        
        public enum  PlayerMoveType
        {
            SizeS,
            SizeM,
            SizeL
        }

        private void Awake()
        {
            _isLimitSize = false;
            _playerMoveType = PlayerMoveType.SizeS;
            playerRigidbody2D = GameObject.FindWithTag("Player").GetComponent<Rigidbody2D>();
            playerTransform = GameObject.FindWithTag("Player").transform;
            playerCollider = GameObject.FindWithTag("Player").GetComponent<Collider2D>();
            _isFacingRight = true;
            _isScaling = false;
            
            verticalSpeed = 11.5f;
            maxUpSpeed = 5.5f;
            maxDownSpeed = 5.5f;
            upAddSpeed = 9.5f;
            downAddSpeed = 9.5f;
            
            playerRigidbody2D.gravityScale = applyGravity ? 1f : 0f;
            playerRigidbody2D.drag = 0f;
            
            if(forceCurve == null || forceCurve.length == 0)
            {
                forceCurve = new AnimationCurve(
                    new Keyframe(0, 0.8f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(1, 0.6f)
                );
                forceCurve.preWrapMode = WrapMode.Clamp;
                forceCurve.postWrapMode = WrapMode.Clamp;
            }
            
            isBoosting = true;
        }

        private void Update()
        {
            ChangeSize();
            /*if (Input.GetKeyDown(KeyCode.L))
            {
                _isLimitSize = true;
            }*/
            PlayerController();
        }

        private void FixedUpdate()
        {
            playerRigidbody2D.velocity = new Vector2(inputHor*verticalSpeed, playerRigidbody2D.velocity.y);
            InterruptScaleChange();
        }

        private void ChangeSize()
        {
            if (_isScaling) return;

            if (_playerMoveType == PlayerMoveType.SizeL && Input.GetKeyDown(KeyCode.E))
            {
                _playerMoveType = PlayerMoveType.SizeM;
                ChangeSizeToS();
            }
            else if (_playerMoveType == PlayerMoveType.SizeS&& Input.GetKeyDown(KeyCode.E))
            {
                _playerMoveType = PlayerMoveType.SizeM;
                ChangeSizeToL();
            }
        }

        private void ChangeSizeToS()
        {
            StartCoroutine(SizeChange_Small());
            _playerMoveType = PlayerMoveType.SizeS;
        }

        private void ChangeSizeToL()
        {
            StartCoroutine(SizeChange_Large());
            _playerMoveType = PlayerMoveType.SizeL;
        }

        private IEnumerator SizeChange_Small()
        {
            _isLimitSize = false;
            _scaleTween = playerTransform.DOScale(targetScaleS, 2f);
            yield return new WaitForSeconds(2f);
        }
        private IEnumerator SizeChange_Large()
        {
            _isLimitSize = false;
            _scaleTween = playerTransform.DOScale(targetScaleL, 2f);
            yield return new WaitForSeconds(2f);
        }
        
        private void InterruptScaleChange()
        {
            // 仅在需要限制大小且不处于缩放状态时执行
            if (_isLimitSize && !_isScaling)
            {
                // 安全终止现有动画
                if (_scaleTween != null && _scaleTween.IsActive())
                {
                    _scaleTween.Kill(false);
                }
                
                // 创建新动画并保存引用
                _scaleTween = playerTransform.DOScale(targetScaleS, 2f)
                    .OnStart(() =>
                    {
                        _playerMoveType = PlayerMoveType.SizeS; // 更新状态
                    });
            }
        }

        private void PlayerController()
        {
            switch (_playerMoveType)
            {
                case PlayerMoveType.SizeS:
                    playerRigidbody2D.gravityScale = 3f;
                    float speedRatio = Mathf.Clamp01(Mathf.Abs(playerRigidbody2D.velocity.y) / 
                                                     (playerRigidbody2D.velocity.y > 0 ? maxUpSpeed : maxDownSpeed));
                    
                    float curveMultiplier = forceCurve.Evaluate(speedRatio);
                    
                    float verticalInput = Input.GetAxis("Vertical");
                    if (verticalInput > 0.1f) 
                    {
                        float forceMultiplier = isBoosting ? 1.5f : 1f;
                        playerRigidbody2D.AddForce(Vector2.up * upAddSpeed * curveMultiplier * forceMultiplier);
                        LimitMaxSpeed();
                    }
                    else if (verticalInput < -0.1f) 
                    {
                        float forceMultiplier = isBoosting ? 1.5f : 1f;
                        playerRigidbody2D.AddForce(Vector2.down * downAddSpeed * curveMultiplier * forceMultiplier);
                        LimitMaxSpeed();
                    }
                    else 
                    {
                        playerRigidbody2D.velocity = new Vector2(playerRigidbody2D.velocity.x, playerRigidbody2D.velocity.y * dragFactor);
                    }
                    
                    inputHor = Input.GetAxis("Horizontal");
                    if (inputHor>0&&_isFacingRight)
                    {
                        Flip();
                    }
                    else if (inputHor < 0 && !_isFacingRight)
                    {
                        Flip();
                    }
                    break;
                case PlayerMoveType.SizeM:
                    break;
                case PlayerMoveType.SizeL:
                    playerRigidbody2D.gravityScale = -3f;
                    float speedRatio2 = Mathf.Clamp01(Mathf.Abs(playerRigidbody2D.velocity.y) / 
                                                     (playerRigidbody2D.velocity.y > 0 ? maxUpSpeed : maxDownSpeed));
                    
                    float curveMultiplier2 = forceCurve.Evaluate(speedRatio2);
                    
                    float verticalInput2 = Input.GetAxis("Vertical");
                    if (verticalInput2 > 0.1f) 
                    {
                        float forceMultiplier = isBoosting ? 1.5f : 1f;
                        playerRigidbody2D.AddForce(Vector2.up * upAddSpeed * curveMultiplier2 * forceMultiplier);
                        LimitMaxSpeed();
                    }
                    else if (verticalInput2 < -0.1f) 
                    {
                        float forceMultiplier = isBoosting ? 1.5f : 1f;
                        playerRigidbody2D.AddForce(Vector2.down * downAddSpeed * curveMultiplier2 * forceMultiplier);
                        LimitMaxSpeed();
                    }
                    else 
                    {
                        playerRigidbody2D.velocity = new Vector2(playerRigidbody2D.velocity.x, playerRigidbody2D.velocity.y * dragFactor);
                    }
                    
                    inputHor = Input.GetAxis("Horizontal");
                    if (inputHor>0&&_isFacingRight)
                    {
                        Flip();
                    }
                    else if (inputHor < 0 && !_isFacingRight)
                    {
                        Flip();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void Flip()
        {
            _isFacingRight = !_isFacingRight;
        }

        private void LimitMaxSpeed()
        {
            if (playerRigidbody2D.velocity.y > maxUpSpeed)
            {
                playerRigidbody2D.velocity = new Vector2(playerRigidbody2D.velocity.x, maxUpSpeed);
            }
            else if (playerRigidbody2D.velocity.y < -maxDownSpeed)
            {
                playerRigidbody2D.velocity = new Vector2(playerRigidbody2D.velocity.x, -maxDownSpeed);
            }
        }
        void OnCollisionEnter2D(Collision2D other)
        {
            HandleCollisions(other.gameObject);
        }

        private void OnCollisionExit2D(Collision2D other)
        {
            if (other.gameObject.CompareTag("Block"))
            {
                StartCoroutine(DelayedLimitReset());
            }
        }

        private void HandleCollisions(GameObject otherObj)
        {
            string otherTag = otherObj.tag;
            switch (otherTag)
            {
                case "Block":
                    _isLimitSize = true;
                    // 立即中断当前缩放
                    if (_isScaling && _scaleTween != null && _scaleTween.IsActive())
                    {
                        _scaleTween.Kill(false);
                        _isScaling = false;
                    }
                    break;
            }
        }
        
        private IEnumerator DelayedLimitReset()
        {
            yield return new WaitForSeconds(2.1f); // 稍长于动画时间
            _isLimitSize = false;
        }
    }
}