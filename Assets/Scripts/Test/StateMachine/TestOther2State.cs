using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestOther2State : State<GameObject>
{
    public override void Enter()
    {
        Debug.Log($"<color=green>Enter TestOther2State {Layer}</color>");
        Entity.GetComponent<Rigidbody>().useGravity = false;
        Debug.Log("Gravity Off");
        Debug.Log("Please Press S");
    }

    public override void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
        {
            // 0과 은 Default로 돌아가라는 명령이라고 가정
            // Enum으로 치면 StateMachineCommand.ToDefault가 될 수 있음
            Owner.ExecuteCommand(0);
        }
    }

    public override void Exit()
    {
        Entity.GetComponent<Rigidbody>().useGravity = true;
        Debug.Log("Gravity On");
        Debug.Log($"<color=yellow>Exit TestOther2State {Layer}</color>");
    }
}
