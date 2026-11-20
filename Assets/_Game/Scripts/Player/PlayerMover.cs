using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMover : MonoBehaviour
{
    private const float correctionValue = 1f;

    [Header("参照")]
    [SerializeField] private CharacterController _controller;
    [SerializeField] private PlayerController _playerController;
    

    [Header("設定値")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _rotationSpeed = 10f;

    void Awake()
    {
        if(
            _controller == null ||
            _playerController == null
        )
        {
            Debug.LogError("アタッチされていないSerializeFieldがあります。",this);
        }
    }
    public void MovePlayer(Vector2 moveInput,Transform cameraTransform)
    {
        //カメラの方向に移動をセットする。
        Vector3 direction = SetDirectionToFaceCamera(moveInput, cameraTransform);
        //進行方向を向く
        FaceDirection(direction);
        //斜め方向の移動を補正
        direction = Vector3.ClampMagnitude(direction, correctionValue);
        //プレイヤーの座標更新処理
        _controller.SimpleMove(direction * _moveSpeed);

    }
    private Vector3 SetDirectionToFaceCamera(Vector2 moveInput,Transform cameraTransform)
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
        if(direction.sqrMagnitude > 0.001f)
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
