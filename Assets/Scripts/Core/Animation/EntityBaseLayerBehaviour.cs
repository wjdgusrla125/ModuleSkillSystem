using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EntityBaseLayerBehaviour : StateMachineBehaviour
{
    private readonly static int kSpeedHash = Animator.StringToHash("speed");
    private readonly static int kIsRollingHash = Animator.StringToHash("isRolling");
    private readonly static int kIsDead = Animator.StringToHash("isDead");
    
    private Entity entity;
    private NavMeshAgent agent;
    private EntityMovement movement;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (entity != null)
            return;

        entity = animator.GetComponent<Entity>();
        agent = animator.GetComponent<NavMeshAgent>();
        movement = animator.GetComponent<EntityMovement>();
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (agent)
            animator.SetFloat(kSpeedHash, agent.desiredVelocity.sqrMagnitude / (agent.speed * agent.speed));
    
        if (movement)
            animator.SetBool(kIsRollingHash, movement.IsRolling);
    }
}
