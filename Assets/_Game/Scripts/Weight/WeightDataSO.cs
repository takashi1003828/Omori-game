using UnityEngine;
[CreateAssetMenu(fileName = "WeightData", menuName = "Game/WeightData")]
public class WeightDataSO : ScriptableObject
{
    [SerializeField] private WeightLevel _weightLevel;
    [SerializeField] float _mass;
    [SerializeField] float _throwForce;
    [SerializeField] float _maxChainLength;
    [SerializeField] float _playerPullRatio;
    [SerializeField] bool _canHold;

    public WeightLevel WeightLevel => _weightLevel;
    public float Mass => _mass;
    public float ThrowForce => _throwForce;
    public float MaxChainLength => _maxChainLength;
    public float PlayerPullRatio => _playerPullRatio;
    public bool CanHold => _canHold;
    
}
