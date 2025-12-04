using System;
using TMPro;
using UnityEngine;

public class HeatMapWeight : MonoBehaviour
{
    public string ID { get; private set; }

    [SerializeField, Min(0)]
    private int _weight = 1;
    
    public int Weight
    {
        get => _weight;
        private set
        {
            _weight = value;
            OnWeightChanged?.Invoke(this, _weight);

        }
    }

    public event Action<HeatMapWeight, float> OnWeightChanged;

    public void Initialize(string id, int count)
    {
        ID = id;
        Weight = count; 
        gameObject.name = $"Ward_{id}_{count}";
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        OnWeightChanged?.Invoke(this, _weight);
    }
#endif
}
