using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _playerInputReader;
    [SerializeField] private PlayerMover _playerMover;
    [SerializeField] private ThirdPersonCamera _thirdPersonCamera;
    [SerializeField] private WeightController _weightController;
    [SerializeField] private ChainController _chainController;
    [SerializeField] Transform _shoulderTransform;
    [Header("設定")]
    [SerializeField] float _wallCheckDistance = 0.5f;
    [SerializeField] float _grabReach = 1f;
    [SerializeField] float _climbRequiredHoldTime = 0.2f;
    [SerializeField] LayerMask _wallLayers;
    private Transform _cameraTransform;
    private bool _isCursorLock;
    private float _ledgeHoldTime;
    private const float _climbLedgeTlerance = 0.1f;


    void Awake()
    {
        if (
            _playerInputReader == null ||
            _playerMover == null ||
            _thirdPersonCamera == null ||
            _weightController == null ||
            _chainController == null
        )
        {
            Debug.LogError("アタッチされていないSerializeFieldがあります。", this);
            enabled = false;
            return;
        }
        _cameraTransform = _thirdPersonCamera.transform;
        LockCursor();
    }
    void Update()
    {
        //カーソルのロック・アンロック関数
        CursolHandling();
        if (!_isCursorLock)
        {
            _chainController.SetReelButton(false);
            _chainController.SetWalkDirection(Vector3.zero);
            _ledgeHoldTime = 0f;
            return;
        }
        //カメラ移動
        _thirdPersonCamera.MoveCamera(_playerInputReader.GetCameraMoveInput());
        //重さ変更ボタン
        if (_playerInputReader.CheckHeavyButtonPressed())
        {
            _weightController.IncreaseWeight();
        }
        if (_playerInputReader.ChackLightButtonPressed())
        {
            _weightController.DecreaseWeight();
        }

        //移動距離取得（移動ボタン、カメラの向き、鎖による移動速度）
        Vector3 walkDirection = _playerMover.MovePlayer(_playerInputReader.GetMoveInput(), _cameraTransform, _chainController.PlayerPullVelocity, _playerInputReader.CheckJumpPressed());
        _chainController.SetWalkDirection(walkDirection);

        //崖登り処理
        LedgeHaveAndHoldCheck(walkDirection, out Vector3 targetPoint);
        if (_ledgeHoldTime >= _climbRequiredHoldTime)
        {
            _playerMover.BeginClimbLedge(targetPoint);
            _ledgeHoldTime = 0f;
        }

        //重り投擲/回収ボタン
        if (_playerInputReader.CheckWeightButtonPressed())
        {
            //
            bool beforIsHold = _weightController.IsHold;
            _weightController.WeightAction(_cameraTransform);
            if (beforIsHold != _weightController.IsHold)
            {
                //投げる時は上限リセット。キャッチの時は鎖リセット
                if(beforIsHold)
                {
                    _chainController.AllowLengthReset();   
                }
                else
                {
                    _chainController.ResetChainAfterCatch();
                }
            }
        }
        //鎖巻き取りボタン
        _chainController.SetReelButton(_playerInputReader.IsReelButtonHeld());
    }
    private void LedgeHaveAndHoldCheck(Vector3 walkDirection, out Vector3 targetPoint)
    {
        targetPoint = default;
        //崖登り処理
        Ray wallRay = new Ray(_shoulderTransform.position, walkDirection.normalized);

        if (Physics.Raycast(wallRay, out RaycastHit wallHit, _wallCheckDistance, _wallLayers, QueryTriggerInteraction.Ignore))
        {
            wallHit.collider.TryGetComponent<ClimbableLedge>(out var ledge);
            if (ledge == null)
            {
                _ledgeHoldTime = 0f;
                return;
            }
            //届くか、届いたら崖端の座標取得
            if (!ledge.TryGetLedgePoints(wallHit, out Vector3 grabPoint))
            {
                _ledgeHoldTime = 0f;
                return;
            }
            targetPoint = grabPoint + Vector3.up * _climbLedgeTlerance;
            float heightFromShoulder = grabPoint.y - _shoulderTransform.position.y;
            //段差と肩の高さがリーチより短い（届く時）
            if (heightFromShoulder >= 0f && heightFromShoulder <= _grabReach)
            {
                _ledgeHoldTime += Time.deltaTime;
                return;
            }
        }
        _ledgeHoldTime = 0f;
    }

    private void CursolHandling()
    {
        //カーソルのロック処理
        //Escキーで解除
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
        // 左クリックで再ロック
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }
    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked; // 画面中央に固定
        Cursor.visible = false;                  // カーソルを非表示
        _isCursorLock = true;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;   // 固定解除
        Cursor.visible = true;                   // カーソルを表示
        _isCursorLock = false;
    }
}
