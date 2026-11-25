using System;
using System.Collections.Generic;
using Unity.Android.Gradle;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;
using UnityEngine.UIElements;

public class ChainController : MonoBehaviour
{
    [Header("Transform参照")]
    [SerializeField] private Transform _playerChainAnchor;
    [SerializeField] private Transform _playerChainRenderAnchor;
    [SerializeField] private Transform _weightChainAnchor;
    [SerializeField] private Transform _weightPivod;

    [Header("スクリプト・レンダー参照")]
    [SerializeField] private WeightController _weightController; [SerializeField] private LineRenderer _lineRenderer;

    [Header("鎖の設定")]
    [SerializeField] private float _chainWidth;
    [SerializeField] private float _measuredDistance;
    [SerializeField] private float _allowChainLength;
    [SerializeField] private float _chainReelSpeed = 25f;
    [SerializeField] private float _minChainLength = 1f;
    [SerializeField] private float _weightRotationSpeed = 180f;
    [SerializeField] private float _minWrapDistance = 0.5f;
    [Header("鎖が引っかかるレイヤー")]
    [SerializeField] private LayerMask _ChainObstacleLayers;

    [Header("重りにかかる力")]
    [SerializeField] private float _pullForcePerMeterForWeight = 8;
    [SerializeField] private float _maxPullForceForWeight = 20;

    [Header("プレイヤーにかかる力")]
    [SerializeField] private float _pullVelocityForPlayer = 2f;
    [SerializeField] private float _maxPullVelocityForPlayer = 6f;

    [Header("デバッグ用：現在の鎖の力と経由点数")]
    [SerializeField] private Vector3 _currentWightPullForce;
    [SerializeField] private Vector3 _currentPlayerPullVelocity;
    [SerializeField] private List<Vector3> _wrapPoints;
    //経由地点にプレイヤー、重りの2つの点を加算した数
    private int _renderPointCount => _wrapPoints.Count + 2;
    private int _lastWrapIndex => _wrapPoints.Count - 1;
    private int _playerAnchorIndex => _wrapPoints.Count + 1;
    private Vector3 _playerPosition => _playerChainAnchor.position;
    private Vector3 _weightPosition => _weightChainAnchor.position;
    private Vector3 _playerRenderPosition => _playerChainRenderAnchor.position;
    private bool _isLengthLimitOver;
    private bool _isReelChain;
    private Vector3 _walkDirection;
    public Vector3 PlayerPullVelocity => _currentPlayerPullVelocity;



    void Awake()
    {
        if (
            _playerChainAnchor == null ||
            _playerChainRenderAnchor == null ||
            _lineRenderer == null ||
            _weightChainAnchor == null ||
            _weightController == null

        )
        {
            Debug.LogError("アタッチされていないSerializeFieldがあります。", this);
            enabled = false;
            return;
        }
        _lineRenderer.positionCount = _renderPointCount;
        _lineRenderer.startWidth = _chainWidth;
        _lineRenderer.endWidth = _chainWidth;
    }
    void Start()
    {
        _allowChainLength = _weightController.CurrentLimitChainLength;
    }
    public void SetReelButton(bool isHoldButton)
    {
        _isReelChain = isHoldButton;
    }
    public void SetWalkDirection(Vector3 nextWalkDirection)
    {
        _walkDirection = nextWalkDirection;
    }
    public void AllowLengthReset()
    {
        if (_weightController.IsHold)
        {
            return;
        }
        _allowChainLength = _weightController.CurrentLimitChainLength;
    }
    public void ResetChainAfterCatch()
    {
        _wrapPoints.Clear();
        _currentPlayerPullVelocity = Vector3.zero;
        _currentWightPullForce = Vector3.zero;
    }

    //LateUpdateでは描画処理を行う
    void LateUpdate()
    {
        _lineRenderer.positionCount = _renderPointCount;
        _lineRenderer.SetPosition(0, _weightPosition);
        //経由地点の描画
        for (int i = 0; i < _wrapPoints.Count; i++)
        {
            _lineRenderer.SetPosition(i + 1, _wrapPoints[i]);
        }
        _lineRenderer.SetPosition(_playerAnchorIndex, _playerRenderPosition);
        WeightRotate();
        //許容距離オーバーで色を変更
        Color chainColor = _isLengthLimitOver ? Color.red : Color.blue;
        ChangeChainColor(chainColor);
    }

    //FixedUpdateでは物理処理を行う
    void FixedUpdate()
    {
        //持っている間は鎖の処理を行わない
        if(_weightController.IsHold)
        {
            return;
        }
        
        UpdateWrapPoint();
        //引っ張りベクトルの決定。
        Vector3 weightPullDirection;
        Vector3 playerPullDirection;
        if (_wrapPoints.Count <= 0)
        {
            weightPullDirection = _playerPosition - _weightPosition;
            playerPullDirection = _weightPosition - _playerPosition;
        }
        else
        {
            weightPullDirection = _wrapPoints[0] - _weightPosition;
            playerPullDirection = _wrapPoints[_lastWrapIndex] - _playerPosition;
        }
        //経由地点を考慮した長さ判定
        _measuredDistance = CalculateRoutedDistance();
        _allowChainLength = Mathf.Min(_allowChainLength, _weightController.CurrentLimitChainLength);

        if (_isReelChain && !_weightController.IsHold)
        {
            ReelChain();
        }
        CalculateChainLength();
        CalculateChainForce(weightPullDirection, playerPullDirection);
        _weightController.ApplyChainForce(_currentWightPullForce);
    }
    //lateUpdateの関数
    private void WeightRotate()
    {
        Vector3 targetAnchor = _playerPosition;
        if (_wrapPoints.Count > 0)
        {
            targetAnchor = _wrapPoints[0];
        }
        Vector3 bossVector = _weightPosition - _weightPivod.position;
        Vector3 toAnchor = targetAnchor - _weightPosition;
        Quaternion nextQuaternion = Quaternion.FromToRotation(bossVector, toAnchor) * _weightPivod.rotation;
        _weightPivod.rotation = Quaternion.RotateTowards(_weightPivod.rotation, nextQuaternion, _weightRotationSpeed * Time.deltaTime);
    }
    private void ChangeChainColor(Color nextColor)
    {
        _lineRenderer.startColor = nextColor;
        _lineRenderer.endColor = nextColor;
    }

    //fixedUpdateの関数
    private void UpdateWrapPoint()
    {
        int wrapCount = _wrapPoints.Count;
        //ラップがない時は追加処理のみ
        if (wrapCount <= 0)
        {
            TryAddWrapPoint(_weightPosition, _playerPosition, false);
            return;
        }
        //ラップが1つなら枷とプレイヤーの間、2つ以上ならそれぞれの隣接ラップの削除を監視
        if (wrapCount == 1)
        {
            if (TryRemoveWrapPoint(_weightPosition, _playerPosition, false))
            {
                return;
            }
        }
        else
        {
            if (TryRemoveWrapPoint(_weightPosition, _wrapPoints[1], false))
            {
                return;
            }
            if (TryRemoveWrapPoint(_playerPosition, _wrapPoints[_lastWrapIndex - 1], true))
            {
                return;
            }
        }
        if (wrapCount > 0)
        {
            TryAddWrapPoint(_weightPosition, _wrapPoints[0], false);
            TryAddWrapPoint(_playerPosition, _wrapPoints[_lastWrapIndex], true);
            return;
        }
        TryAddWrapPoint(_weightPosition, _playerPosition, false);
    }
    private bool TryRemoveWrapPoint(Vector3 entitiyPosition, Vector3 removeCheckPoint, bool isPlayer)
    {
        int targetIndex = isPlayer ? _lastWrapIndex : 0;
        if (!CheckChainBlockPoint(entitiyPosition, removeCheckPoint, out Vector3 blockPoint))
        {
            _wrapPoints.RemoveAt(targetIndex);
            return true;
        }
        return false;
    }
    private void TryAddWrapPoint(Vector3 entitiyPosition, Vector3 AddCheckPoint, bool isPlayer)
    {
        if (!CheckChainBlockPoint(entitiyPosition, AddCheckPoint, out Vector3 blockPoint))
        {
            return;
        }
        if (Vector3.Distance(entitiyPosition, blockPoint) < _minWrapDistance)
        {
            return;
        }
        foreach (Vector3 point in _wrapPoints)
        {
            if (Vector3.Distance(point, blockPoint) < _minWrapDistance)
            {
                return;
            }
        }
        if (isPlayer)
        {
            _wrapPoints.Add(blockPoint);
        }
        else
        {
            _wrapPoints.Insert(0, blockPoint);
        }
    }
    private bool CheckChainBlockPoint(Vector3 startPoint, Vector3 endPoint, out Vector3 blockPoint)
    {
        bool result = Physics.Linecast(startPoint, endPoint, out RaycastHit hit, _ChainObstacleLayers, QueryTriggerInteraction.Ignore);
        blockPoint = hit.point;
        return result;
    }
    private float CalculateRoutedDistance()
    {
        float distance = 0f;

        Vector3 previousPoint = _weightPosition;
        for (int i = 0; i < _wrapPoints.Count; i++)
        {
            //前回の点と次の点の距離を計算して加算していく
            distance += Vector3.Distance(previousPoint, _wrapPoints[i]);
            previousPoint = _wrapPoints[i];
        }
        distance += Vector3.Distance(previousPoint, _playerPosition);
        return distance;
    }
    private void ReelChain()
    {
        _allowChainLength = Mathf.MoveTowards(_allowChainLength, _minChainLength, _chainReelSpeed * Time.fixedDeltaTime);
    }
    private void CalculateChainLength()
    {
        //プレイヤーから最後尾アンカーへのベクトル取得
        Vector3 targetAnchor = _weightPosition;
        if (_wrapPoints.Count > 0)
        {
            targetAnchor = _wrapPoints[_lastWrapIndex];
        }
        Vector3 playerToAnchorDirection = targetAnchor - _playerPosition;
        //進行方向と最後尾アンカーへのベクトルの内積が負なら離れる方向へ進んでいる。
        bool isLeaving = Vector3.Dot(_walkDirection, playerToAnchorDirection) < 0f;
        //保持、離れていない、巻き取りをしている場合は早期リターン
        if (_weightController.IsHold || !isLeaving || _isReelChain)
        {
            return;
        }
        _allowChainLength = Math.Min(_weightController.CurrentLimitChainLength, Mathf.Max(_allowChainLength, _measuredDistance));
    }
    private void CalculateChainForce(Vector3 weightPullDirection, Vector3 playerPullDireciton)
    {
        float overLength = _measuredDistance - _allowChainLength;
        _isLengthLimitOver = overLength > 0f;
        //超えていない時は力をゼロにして早期リターン
        if (!_isLengthLimitOver)
        {
            _currentPlayerPullVelocity = Vector3.zero;
            _currentWightPullForce = Vector3.zero;
            return;
        }
        //引かれる割合（PlayerPullRatio）を利用して鎖による移動速度を更新
        UpdateChainPullPowerForPlayer(playerPullDireciton, overLength);
        UpdatePullPowerForWeight(weightPullDirection, overLength);
    }

    private void UpdatePullPowerForWeight(Vector3 forceVector, float overLength)
    {
        float pullForce = Mathf.Min(overLength * _pullForcePerMeterForWeight, _maxPullForceForWeight);
        //重りが動く割合 = 1f - PlayerPullRatioとして計算
        pullForce *= 1f - _weightController.CurrentPlayerPullRatio;
        _currentWightPullForce = forceVector.normalized * pullForce;
    }
    private void UpdateChainPullPowerForPlayer(Vector3 forceVector, float overLength)
    {
        float pullForce = Mathf.Min(overLength * _pullVelocityForPlayer, _maxPullVelocityForPlayer);
        pullForce *= _weightController.CurrentPlayerPullRatio;
        _currentPlayerPullVelocity = forceVector.normalized * pullForce;
    }
}
