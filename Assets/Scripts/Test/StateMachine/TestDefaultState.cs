using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test
{
    public class TestDefaultState : State<GameObject>
    {
        public override void Enter()
        {
            Debug.Log($"<color=green>Enter TestDefaultState {Layer}</color>");
            Debug.Log("Please Press SpaceBar");
        }

        public override void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Debug.Log("Input Space");
        }

        public override void Exit()
        {
            Debug.Log($"<color=yellow>Exit TestDefaultState {Layer}</color>");
        }
    }
}