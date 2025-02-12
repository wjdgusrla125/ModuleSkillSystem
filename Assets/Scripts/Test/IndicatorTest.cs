using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class IndicatorTest : MonoBehaviour
{
    [SerializeField]
    private Indicator indicator;
    [SerializeField]
    private float radius;
    [Range(0, 360f)]
    [SerializeField]
    private float angle;
    [Range(0, 1f)]
    [SerializeField]
    private float fillAmount;

    private void Update()
    {
        if (!indicator)
            return;

        indicator.Radius = radius;
        indicator.Angle = angle;
        indicator.FillAmount = fillAmount;
    }
}