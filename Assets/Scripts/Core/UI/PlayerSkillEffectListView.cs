using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSkillEffectListView : SkillEffectListView
{
    private void Start()
        => Target = GameObject.FindGameObjectWithTag("Player").GetComponent<SkillSystem>();
}
