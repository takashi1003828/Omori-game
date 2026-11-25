using System;
using UnityEngine;

public class WeightController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _holdPoint;
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private WeightSetSO _weightSet;
    [Header("設定")]
    [SerializeField] private float _upwardRatio = 0.15f;
    [SerializeField] private float _catchDistance = 1.5f;
    private bool _isHold = true;
    private WeightDataSO _currentWeightData;
    public WeightLevel CurrentWeightLevel => _currentWeightData.WeightLevel;
    public float CurrentLimitChainLength => _currentWeightData.MaxChainLength;
    public float CurrentPlayerPullRatio => _currentWeightData.PlayerPullRatio;
    public bool IsHold => _isHold;


    void Awake()
    {
        if (
            _rigidbody == null ||
            _holdPoint == null ||
            _weightSet == null
        )
        {
            Debug.LogError("アタッチされていないSerializeFieldがあります。", this);
            enabled = false;
            return;
        }
        //初期設定
        ChangeWeight(WeightLevel.Light);
        if (!HasWeightData())
        {
            Debug.LogError("従量データの初期化に失敗しました。", this);
            return;
        }
        WeightCatch();
    }
    public bool HasWeightData()
    {
        return _currentWeightData != null;
    }
    public void WeightAction(Transform cameraTransform)
    {
        if (!HasWeightData())
        {
            return;
        }
        if (!_isHold)
        {
            if (Vector3.Distance(_playerTransform.position, transform.position) < _catchDistance)
            {
                WeightCatch();
            }
            return;
        }
        WeightThrow(cameraTransform);
    }
    public void DecreaseWeight()
    {
        if (!HasWeightData())
        {
            return;
        }
        int nextWeightLevel = (int)_currentWeightData.WeightLevel;
        nextWeightLevel--;
        nextWeightLevel = Math.Clamp(nextWeightLevel, (int)WeightLevel.Light, (int)WeightLevel.Heavy);
        ChangeWeight((WeightLevel)nextWeightLevel);
    }
    public void IncreaseWeight()
    {
        if (!HasWeightData())
        {
            return;
        }
        int nextWeightLevel = (int)_currentWeightData.WeightLevel;
        nextWeightLevel++;
        nextWeightLevel = Math.Clamp(nextWeightLevel, (int)WeightLevel.Light, (int)WeightLevel.Heavy);
        ChangeWeight((WeightLevel)nextWeightLevel);
    }
    public void ApplyChainForce(Vector3 force)
    {
        if (_isHold)
        {
            return;
        }
        _rigidbody.AddForce(force, ForceMode.Force);
    }
    private void ChangeWeight(WeightLevel nextLevel)
    {
        WeightDataSO tempWeightData;
        tempWeightData = _weightSet.GetWeightData(nextLevel);
        //データ取得失敗まだはレベルの変更なしならリターン
        if (tempWeightData == null)
        {
            return;
        }
        _currentWeightData = tempWeightData;
        //実際のパラメータ更新処理
        _rigidbody.mass = _currentWeightData.Mass;

        //もし持てない重さならリリース    
        if (_currentWeightData.CanHold == false && _isHold == true)
        {
            ReleaseWeight();
        }
    }

    private void ReleaseWeight()
    {
        //動きリセットと親オブジェクトの解除

        transform.SetParent(null);
        _rigidbody.isKinematic = false;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _isHold = false;

    }
    private void WeightCatch()
    {
        //持てるか判定
        if (!_currentWeightData.CanHold)
        {
            return;
        }
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        transform.SetParent(_holdPoint);
        transform.localPosition = Vector3.zero;
        _isHold = true;
    }
    private void WeightThrow(Transform cameraTransform)
    {
        ReleaseWeight();
        _rigidbody.AddForce(
            cameraTransform.forward * _currentWeightData.ThrowForce + cameraTransform.up * _upwardRatio, ForceMode.Impulse
        );
    }
}
