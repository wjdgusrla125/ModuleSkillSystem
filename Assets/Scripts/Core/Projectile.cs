using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField]
    private GameObject impactPrefab;

    private Entity owner;
    private Rigidbody rigidBody;
    private float speed;
    private Skill skill;

    public void Setup(Entity owner, float speed, Vector3 direction, Skill skill)
    {
        this.owner = owner;
        this.speed = speed;
        transform.forward = direction;
        // 현재 Skill의 Level 정보를 저장하기 위해 Clone을 보관
        this.skill = skill.Clone() as Skill;
    }

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void OnDestroy()
    {
        Destroy(skill);
    }

    private void FixedUpdate()
    {
        rigidBody.velocity = transform.forward * speed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Entity>() == owner)
            return;

        var impact = Instantiate(impactPrefab);
        impact.transform.forward = -transform.forward;
        impact.transform.position = transform.position;

        var entity = other.GetComponent<Entity>();
        if (entity)
            entity.SkillSystem.Apply(skill);

        Destroy(gameObject);
    }
}