using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test
{
    public class TestStateMachineRunner : MonoBehaviour
    {
        private TestStateMachine stateMachine;

        void Start()
        {
            stateMachine = new();
            stateMachine.Setup(gameObject);
        }

        void Update()
        {
            stateMachine.Update();
        }
    }
}