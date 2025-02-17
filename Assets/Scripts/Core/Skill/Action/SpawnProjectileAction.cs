using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SpawnProjectileAction : SkillAction
{
    [SerializeField]
    private GameObject projectilePrefab;
    [SerializeField]
    private string spawnPointSocketName;
    [SerializeField]
    private float speed;

    public override void Apply(Skill skill)
    {
        var socket = skill.Owner.GetTransformSocket(spawnPointSocketName);
        var projectile = GameObject.Instantiate(projectilePrefab);
        projectile.transform.position = socket.position;
        projectile.GetComponent<Projectile>().Setup(skill.Owner, speed, socket.forward, skill);
    }

    public override object Clone()
    {
        return new SpawnProjectileAction()
        {
            projectilePrefab = projectilePrefab,
            spawnPointSocketName = spawnPointSocketName,
            speed = speed
        };
    }
}