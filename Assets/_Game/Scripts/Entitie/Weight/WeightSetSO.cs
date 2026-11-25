using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu (fileName = "WeightDataSet", menuName = "Game/WeightDataSet")]
public class WeightSetSO : ScriptableObject
{
    [SerializeField] private List<WeightDataSO> _weightDataList;

    public WeightDataSO GetWeightData(WeightLevel weightLevel)
    {
        if(_weightDataList == null)
        {
            return null;
        }
        foreach(WeightDataSO data in _weightDataList)
        {
            if(data == null)
            {
                continue;
            }
            if(data.WeightLevel == weightLevel)
            {
                return data;
            }
        }
        Debug.LogError($"{weightLevel}はWeightSetに登録されていません。");
        return null;
    }
}
