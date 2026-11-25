using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    private const float correctionValue = 1f;

    [Header("参照")]
    [SerializeField] private CharacterController _controller;


    [Header("設定値")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpHeight = 0.4f;
    [SerializeField] private float _climbVelocity = 3f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _defaultGravityVelocity = -1f;
    [SerializeField] private float _maxFallVelocity = -20f;
    [SerializeField] private float _climbGiveUpTime = 0.15f;

    private float _verticalVelocity;
    private bool _isLedgeClimbing;
    private Vector3 _targetPoint;
    private float _stoppingDistance = 0.01f;
    private float _minClimbDistance = 0.0001f;
    private float _climbStopTime;


    void Awake()
    {
        if (
            _controller == null
        )
        {
            Debug.LogError("アタッチされていないSerializeFieldがあります。", this);
            enabled = false;
            return;
        }
    }
    public Vector3 MovePlayer(Vector2 moveInput, Transform cameraTransform, Vector3 chainPullVelocity, bool isJump)
    {
        if (_isLedgeClimbing)
        {
            float previousHeight = _controller.bounds.min.y;
            //上昇量の計算
            float climbMove = Mathf.Min(_climbVelocity * Time.deltaTime,
            _targetPoint.y - _controller.bounds.min.y);
            _controller.Move(new Vector3(0, climbMove, 0));
            //移動できているか判定。（一定時間で崖登りキャンセル）
            if (_controller.bounds.min.y - previousHeight < _minClimbDistance)
            {
                _climbStopTime += Time.deltaTime;
            }
            else
            {
                //1フレームでも最小移動量を上回ればタイマーリセット
                _climbStopTime = default;
            }
            //目標地点に辿りついたか判定
            if (_targetPoint.y - _controller.bounds.min.y <= _stoppingDistance)
            {
                _isLedgeClimbing = false;
                Debug.Log("崖登り完了");
            }
            //崖登りキャンセル判定
            if (_climbStopTime > _climbGiveUpTime)
            {
                _isLedgeClimbing = false;
                Debug.Log("崖登りキャンセル");
            }
            return Vector3.zero;
        }
        if (!_controller.isGrounded)
        {
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, _maxFallVelocity);
        }
        else
        {
            _verticalVelocity = _defaultGravityVelocity;
            if (isJump)
            {
                _verticalVelocity += Mathf.Sqrt(2f * -Physics.gravity.y - _jumpHeight);
            }
        }
        //カメラの方向に移動をセットする。
        Vector3 inputMoveVector = SetDirectionToFaceCamera(moveInput, cameraTransform);
        //進行方向を向く
        FaceDirection(inputMoveVector);
        //斜め方向の移動ベクトルを補正
        inputMoveVector = Vector3.ClampMagnitude(inputMoveVector, correctionValue);
        //プレイヤーの座標更新処理(速度 * 時間)
        _controller.Move(CalculateMoveVelocity(inputMoveVector, chainPullVelocity) * Time.deltaTime);
        //鎖を伸ばす処理に使うためリターン
        return inputMoveVector;

    }
    public void BeginClimbLedge(Vector3 targetPoint)
    {
        _isLedgeClimbing = true;
        _targetPoint = targetPoint;
        _verticalVelocity = 0f;
    }
    private Vector3 CalculateMoveVelocity(Vector3 inputVector, Vector3 chainPullVelocity)
    {
        //移動ベクトルを移動速度に変換
        Vector3 inputMoveVelocity = inputVector * _moveSpeed;
        //input,重力,鎖の全ての移動速度を加算
        return inputMoveVelocity + (Vector3.up * _verticalVelocity) + chainPullVelocity;
    }
    private Vector3 SetDirectionToFaceCamera(Vector2 moveInput, Transform cameraTransform)
    {
        //カメラからの前方と右方向を取得
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        //上下成分除去
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        return forward * moveInput.y + right * moveInput.x;
    }
    private void FaceDirection(Vector3 direction)
    {
        //進行方向を向く
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime
            );
        }
    }
}
