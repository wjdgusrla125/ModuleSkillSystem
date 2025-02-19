using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("SkillObject/ObjectScaler")]
public class SkillObjectScaler : MonoBehaviour, ISkillObjectComponent
{
    [SerializeField]
    private Vector3 baseScale;

    public void OnSetupSkillObject(SkillObject skillObject)
    {
        var scaledBaseScale = Vector3.Scale(baseScale, skillObject.ObjectScale);
        var localScale = transform.localScale;
        var resultScale = Vector3.Scale(localScale, scaledBaseScale);
        
        resultScale.x = GetValidValue(resultScale.x, localScale.x);
        resultScale.y = GetValidValue(resultScale.y, localScale.y);
        resultScale.z = GetValidValue(resultScale.z, localScale.z);

        transform.localScale = resultScale;
    }

    private float GetValidValue(float value, float defaultValue)
        => Mathf.Approximately(value, 0f) ? defaultValue : value;
}