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
            // Ű���� N ��ư�� ������ TestDefaultState���� TestOther1State�� ����
            MakeTransition<TestDefaultState, TestOther1State>(state => Input.GetKeyDown(KeyCode.N));
            // Ű���� N ��ư�� ������ TestOther1State�� TestOther2State�� ����
            MakeTransition<TestOther1State, TestOther2State>(state => Input.GetKeyDown(KeyCode.N));
            // 0�̶�� Command�� �Ѿ����, 1�� Layer���� �������� State�� TestOther2State��� TestDefaultState�� ����
            MakeAnyTransition<TestDefaultState>(0, State => IsInState<TestOther1State>(1));

            MakeTransition<TestDefaultState, TestOther1State>(state => Input.GetKeyDown(KeyCode.M), 1);
            MakeTransition<TestOther1State, TestOther2State>(state => Input.GetKeyDown(KeyCode.M), 1);
            // 0�̶�� Command�� �Ѿ����, 0�� Layer���� �������� State�� TestOther2State��� TestDefaultState�� ����
            MakeAnyTransition<TestDefaultState>(0, State => IsInState<TestOther1State>(0), 1);
        }
    }
}