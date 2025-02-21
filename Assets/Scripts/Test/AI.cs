using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    [SerializeField]
    private Entity target;
    [SerializeField]
    private Skill skill;

    private void Start()
    {
        var entity = GetComponent<Entity>();
        entity.Target = target;
        var registedSkill = entity.SkillSystem.Register(skill);
        registedSkill.Use();
    }
}