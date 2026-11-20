using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _playerInputReader;
    [SerializeField] private PlayerMover _playerMover;
    [SerializeField] private ThirdPersonCamera _thirdPersonCamera;
    [SerializeField] private WeightController _weightController;
    private Transform _cameraTransform;
    private bool _isCursorLock;
    
    void Awake()
    {
        if(
            _playerInputReader == null ||
            _playerMover == null ||
            _thirdPersonCamera == null ||
            _weightController == null
        )
        {
            Debug.LogError("アタッチされていないSerializeFieldがあります。",this);
        }
        _cameraTransform = _thirdPersonCamera.transform;
        LockCursor();
    }
    void Update()
    {
        //カーソルのロック・アンロック関数
        CursolHandling();
        //移動取得
        _playerMover.MovePlayer(_playerInputReader.GetMoveInput(),_cameraTransform);
        //カメラ移動
        _thirdPersonCamera.MoveCamera(_playerInputReader.GetCameraMoveInput());
        //重さ変更ボタン
        if(_playerInputReader.CheckHeavyButtonPressed())
        {
            _weightController.IncreaseWeight();
        }
        if(_playerInputReader.ChackLightButtonPressed())
        {
            _weightController.DecreaseWeight();
        }
        //重り投擲ボタン
        if(_playerInputReader.CheckWeightButtonPressed() && _isCursorLock)
        {
            _weightController.WeightAction(_cameraTransform);
        }
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
