using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Serializable]
public abstract class EffectAction : ICloneable
{
    // Effect가 시작될 때 호출되는 시작 함수
    public virtual void Start(Effect effect, Entity user, Entity target, int level, float scale) { }
    // 실제 Effect의 효과를 구현하는 함수
    public abstract bool Apply(Effect effect, Entity user, Entity target, int level, int stack, float scale);
    // Effect가 종료될 때 호출되는 종료 함수
    public virtual void Release(Effect effect, Entity user, Entity target, int level, float scale) { }

    // Effect의 Stack이 바뀌었을 때 호출되는 함수
    // Stack마다 Bonus 값을 주는 Action일 경우, 이 함수를 통해서 Bonus 값을 새로 갱신할 수 있음
    public virtual void OnEffectStackChanged(Effect effect, Entity user, Entity target, int level, int stack, float scale) { }

    // Key로 Text Mark, Value로 Text Mark를 대체할 Text를 가진 Dctionary를 만드는 함수
    protected virtual IReadOnlyDictionary<string, string> GetStringsByKeyword(Effect effect) => null;

    // Effect의 설명인 Description Text를 GetStringsByKeyword 함수를 통해 만든 Dictionary로 Replace 작업을 하는 함수
    public string BuildDescription(Effect effect, string description, int stackActionIndex, int stack, int effectIndex)
    {
        var stringsByKeyword = GetStringsByKeyword(effect);
        if (stringsByKeyword == null)
            return description;

        if (stack == 0)
            // ex. description = "적에게 $[EffectAction.defaultDamage.0] 피해를 줍니다."
            // defaultDamage = 300, effectIndex = 0, stringsByKeyword = new() { { "defaultDamage", defaultDamage.ToString() } };
            // description.Replace("$[EffectAction.defaultDamage.0]", "300") => "적에게 300 피해를 줍니다."
            description = TextReplacer.Replace(description, "effectAction", stringsByKeyword, effectIndex.ToString());
        else
            // Mark = $[EffectAction.Keyword.StackActionIndex.Stack.EffectIndex]
            description = TextReplacer.Replace(description, "effectAction", stringsByKeyword, $"{stackActionIndex}.{stack}.{effectIndex}");

        return description;
    }

    public abstract object Clone();
}