using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test
{
    public class TestOther1State : State<GameObject>
    {
        public override void Enter()
        {
            Debug.Log($"<color=green>Enter TestOther1State {Layer}</color>");
            Debug.Log("Please Press A");
        }

        public override void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
                Debug.Log("Input A");
        }

        public override void Exit()
        {
            Debug.Log($"<color=yellow>Exit TestOther1State {Layer}</color>");
        }
    }
}