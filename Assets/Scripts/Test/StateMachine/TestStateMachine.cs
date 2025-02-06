using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test
{
    public class TestStateMachine : StateMachine<GameObject>
    {
        protected override void AddStates()
        {
            AddState<TestDefaultState>();
            AddState<TestOther1State>();
            AddState<TestOther2State>();

            AddState<TestDefaultState>(1);
            AddState<TestOther1State>(1);
            AddState<TestOther2State>(1);
        }

        protected override void MakeTransitions()
        {
            // 키보드 N 버튼을 누르면 TestDefaultState에서 TestOther1State로 전이
            MakeTransition<TestDefaultState, TestOther1State>(state => Input.GetKeyDown(KeyCode.N));
            // 키보드 N 버튼을 누르면 TestOther1State로 TestOther2State로 전이
            MakeTransition<TestOther1State, TestOther2State>(state => Input.GetKeyDown(KeyCode.N));
            // 0이라는 Command가 넘어오면, 1번 Layer에서 실행중인 State가 TestOther2State라면 TestDefaultState로 전이
            MakeAnyTransition<TestDefaultState>(0, State => IsInState<TestOther1State>(1));

            MakeTransition<TestDefaultState, TestOther1State>(state => Input.GetKeyDown(KeyCode.M), 1);
            MakeTransition<TestOther1State, TestOther2State>(state => Input.GetKeyDown(KeyCode.M), 1);
            // 0이라는 Command가 넘어오고, 0번 Layer에서 실행중인 State가 TestOther2State라면 TestDefaultState로 전이
            MakeAnyTransition<TestDefaultState>(0, State => IsInState<TestOther1State>(0), 1);
        }
    }
}