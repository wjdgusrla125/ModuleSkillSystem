using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Stat : IdentifiedObject
{
    public delegate void ValueChangedHandler(Stat stat, float currentValue, float prevValue);
    
    [SerializeField] private bool isPercentType;
    [SerializeField] private float maxValue;
    [SerializeField] private float minValue;
    [SerializeField] private float defaultValue;
    
    private Dictionary<object, Dictionary<object, float>> bonusValuesByKey = new();

    public bool IsPercentType => isPercentType;
    
    public float MaxValue
    {
        get => maxValue;
        set => maxValue = value;
    }

    public float MinValue
    {
        get => minValue;
        set => minValue = value;
    }

    public float DefaultValue
    {
        get => defaultValue;
        set
        {
            float prevValue = Value;
            defaultValue = Mathf.Clamp(value, MinValue, MaxValue);
            
            TryInvokeValueChangedEvent(Value, prevValue);
        }
    }
    
    public float BonusValue { get; private set; }
    
    public float Value => Mathf.Clamp(defaultValue + BonusValue, MinValue, MaxValue);
    public bool IsMax => Mathf.Approximately(Value, maxValue);
    public bool IsMin => Mathf.Approximately(Value, minValue);
    
    public event ValueChangedHandler onValueChanged;
    public event ValueChangedHandler onValueMax;
    public event ValueChangedHandler onValueMin;
    
    private void TryInvokeValueChangedEvent(float currentValue, float prevValue)
    {
        if (!Mathf.Approximately(currentValue, prevValue))
        {
            onValueChanged?.Invoke(this, currentValue, prevValue);
            
            if (Mathf.Approximately(currentValue, MaxValue))
            {
                onValueMax?.Invoke(this, MaxValue, prevValue);
            }
            else if (Mathf.Approximately(currentValue, MinValue))
            {
                onValueMin?.Invoke(this, MinValue, prevValue);
            }
        }
    }

    public void SetBonusValue(object key, object subKey, float value)
    {
        if (!bonusValuesByKey.ContainsKey(key))
        {
            bonusValuesByKey[key] = new Dictionary<object, float>();
        }
        else
        {
            BonusValue -= bonusValuesByKey[key][subKey];
        }
        
        float prevValue = Value;
        bonusValuesByKey[key][subKey] = value;
        BonusValue += value;

        TryInvokeValueChangedEvent(Value, prevValue);
    }

    public void SetBonusValue(object key, float value)
        => SetBonusValue(key, string.Empty, value);

    public float GetBonusValue(object key)
        => bonusValuesByKey.TryGetValue(key, out var bonusValuesBySubkey) ?
        bonusValuesBySubkey.Sum(x => x.Value) : 0f;

    public float GetBonusValue(object key, object subKey)
    {
        if (bonusValuesByKey.TryGetValue(key, out var bonusValuesBySubkey))
        {
            if (bonusValuesBySubkey.TryGetValue(subKey, out var value))
            {
                return value;
            }
        }
        
        return 0f;
    }

    public bool RemoveBonusValue(object key)
    {
        if (bonusValuesByKey.TryGetValue(key, out var bonusValuesBySubkey))
        {
            float prevValue = Value;
            BonusValue -= bonusValuesBySubkey.Values.Sum();
            bonusValuesByKey.Remove(key);

            TryInvokeValueChangedEvent(Value, prevValue);
            return true;
        }
        return false;
    }

    public bool RemoveBonusValue(object key, object subKey)
    {
        if (bonusValuesByKey.TryGetValue(key, out var bonusValuesBySubkey))
        {
            if (bonusValuesBySubkey.Remove(subKey, out var value))
            {
                var prevValue = Value;
                BonusValue -= value;
                TryInvokeValueChangedEvent(Value, prevValue);
                return true;
            }
        }
        return false;
    }

    public bool ContainsBonusValue(object key)
        => bonusValuesByKey.ContainsKey(key);

    public bool ContainsBonusValue(object key, object subKey)
        => bonusValuesByKey.TryGetValue(key, out var bonusValuesBySubKey) ? bonusValuesBySubKey.ContainsKey(subKey) : false;
}
