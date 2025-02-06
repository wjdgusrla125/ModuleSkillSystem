using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EntityMovement : MonoBehaviour
{
    public delegate void SetDestinationHandler(EntityMovement movement, Vector3 destination);
        
    [SerializeField] private Stat moveSpeedStat;
    [SerializeField] private float rollTime = 0.5f;
    
    private NavMeshAgent agent;
    private Transform traceTarget;
    private Stat entityMoveSpeedStat;
    
    public Entity Owner { get; private set; }
    public float MoveSpeed => agent.speed;
    public bool IsRolling { get; private set; }

    public Transform TraceTarget
    {
        get => traceTarget;
        set
        {
            if (traceTarget == value) return;

            Stop();
            
            traceTarget = value;

            if (traceTarget)
            {
                StartCoroutine("TraceUpdate");
            }
        }
    }

    public Vector3 Destination
    {
        get => agent.destination;
        set
        {
            TraceTarget = null;
            SetDestination(value);
        }
    }
    
    public event SetDestinationHandler onSetDestination;

    public void Setup(Entity owner)
    {
        Owner = owner;
        
        agent = Owner.GetComponent<NavMeshAgent>();
        agent.updateRotation = false;

        var animator = Owner.Animator;
        if (animator)
        {
            animator.SetFloat("rollSpeed", 1 / rollTime);
        }
        
        entityMoveSpeedStat = moveSpeedStat ? Owner.Stats.GetStat(moveSpeedStat) : null;

        if (entityMoveSpeedStat)
        {
            agent.speed = entityMoveSpeedStat.Value;
            entityMoveSpeedStat.onValueChanged += OnMoveSpeedChanged;
        }
    }

    private void OnDisable() => Stop();

    private void OnDestroy()
    {
        if (entityMoveSpeedStat)
        {
            entityMoveSpeedStat.onValueChanged -= OnMoveSpeedChanged;
        }
    }

    private void SetDestination(Vector3 destination)
    {
        agent.destination = destination;
        LookAt(destination);
        
        onSetDestination?.Invoke(this, destination);
    }

    public void Stop()
    {
        traceTarget = null;
        StopCoroutine("TraceUpdate");

        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        
        agent.velocity = Vector3.zero;
    }

    public void LookAt(Vector3 position)
    {
        StopCoroutine("LookAtUpdate");
        StartCoroutine("LookAtUpdate", position);
    }

    public void LookAtImmediate(Vector3 position)
    {
        position.y = transform.position.y;
        var lookDirection = (position - transform.position).normalized;
        var rotation = lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : transform.rotation;
        transform.rotation = rotation;
    }

    private IEnumerator LookAtUpdate(Vector3 position)
    {
        position.y = transform.position.y;
        var lookDirection = (position - transform.position).normalized;
        var rotation = lookDirection != Vector3.zero ? Quaternion.LookRotation(lookDirection) : transform.rotation;
        var speed = 180f / 0.15f;

        while (true)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, speed * Time.deltaTime);
            
            if(transform.rotation == rotation) break;
            
            yield return null;
        }
    }

    public void Roll(float distance, Vector3 direction)
    {
        Stop();

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
        
        IsRolling = true;
        StopCoroutine("RollUpdate");
        StartCoroutine("RollUpdate", distance);
    }

    public void Roll(float distance) => Roll(distance, transform.forward);

    private IEnumerator RollUpdate(float rollDistance)
    {
        float currentRollTime = 0f;
        float prevRollDistance = 0f;

        while (true)
        {
            currentRollTime += Time.deltaTime;

            float timePoint = currentRollTime / rollTime;
            float inOutSine = -(Mathf.Cos(Mathf.PI * timePoint) - 1f) / 2f;
            float currentRollDistance = Mathf.Lerp(0f, rollDistance, inOutSine);
            float deltaValue = currentRollDistance - prevRollDistance;
            
            transform.position += (transform.forward * deltaValue);
            prevRollDistance = currentRollDistance;

            if (currentRollTime >= rollTime) 
                break;
            else 
                yield return null;
        }
        
        IsRolling = false;
    }

    private IEnumerator TraceUpdate()
    {
        while (true)
        {
            if (Vector3.SqrMagnitude(TraceTarget.position - transform.position) > 1.0f)
            {
                SetDestination(TraceTarget.position);
                yield return null;
            }
            else
            {
                break;
            }
        }
    }

    private void OnMoveSpeedChanged(Stat stat, float currentValue, float prevValue) => agent.speed = currentValue;
}
