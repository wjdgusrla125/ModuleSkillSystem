using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CookieBaseLayerBehaviour : StateMachineBehaviour
{
    private readonly static int cSpeedHash = Animator.StringToHash("speed");
    private readonly static int cIsDeadHash = Animator.StringToHash("isDead");
    private readonly static int cIsStunningHash = Animator.StringToHash("isStunning");

    private Entity entity;
    private NavMeshAgent agent;
    private EntityMovement movement;
    private CookieController controller;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (entity != null) return;
        
        entity = animator.GetComponent<Entity>();
        agent = animator.GetComponent<NavMeshAgent>();
        controller = animator.GetComponent<CookieController>();
        movement = animator.GetComponent<EntityMovement>();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (controller)
        {
            // JoyStickController에서 조이스틱 크기 값을 가져옵니다
            float speed = 0f;
            if (JoyStickController.Instance != null)
            {
                // JoyStickController에 추가한 public 메서드를 통해 조이스틱 크기에 접근합니다
                speed = controller.MoveSpeed * JoyStickController.Instance.GetJoystickMagnitude();
            }
        
            animator.SetFloat(cSpeedHash, speed);
        }
    }

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    //override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    //override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}
}
