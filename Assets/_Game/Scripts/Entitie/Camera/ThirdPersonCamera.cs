using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _target;

    [Header("CameraSettings")]
    [SerializeField] private float _distance = 5f;
    [SerializeField] private float _height = 1.2f;
    [SerializeField] private float _sensitiveity = 3f;
    [SerializeField] private float _maxPitch = 70f;

    private float _yaw;
    private float _pitch = 20f;

    void Awake()
    {
        if(
            _target == null
        )
        {
            Debug.LogError("アタッチされていないSerializeFieldがあります。",this);
        }
    }
    public void MoveCamera(Vector2 input)
    {
        //回転角度を加算する
        _yaw += input.x;
        _pitch -= input.y;
        //縦方向の画面の制限
        _pitch = Mathf.Clamp(_pitch, -_maxPitch, _maxPitch);
        //カメラの回転計算
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        //カメラの移動計算
        AdjustCameraPosition();
    }

    private void AdjustCameraPosition()
    {
        //カメラの回転計算
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        //主人公の注視点を決定する。
        Vector3 focus = _target.position + Vector3.up * _height;
        //回転角に応じたカメラ位置
        transform.position = focus - rotation * Vector3.forward * _distance;
        //主人公を見る
        transform.LookAt(focus);
    }
}
