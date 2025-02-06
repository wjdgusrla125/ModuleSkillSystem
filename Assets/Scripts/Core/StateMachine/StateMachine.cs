using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StateMachine<EntityType>
{
    public delegate void StateChangedHandler(StateMachine<EntityType> stateMachine, State<EntityType> newState, State<EntityType> prevState, int layer);
    
    private class StateData
    {
        public int Layer { get; private set; }
        public int Priority { get; private set; }
        public State<EntityType> State { get; private set; }
        public List<StateTransition<EntityType>> Transitions { get; private set; } = new();
        
        public StateData(int layer, int priority, State<EntityType> state)
            => (Layer, Priority, State) = (layer, priority, state);
    }
    
    private readonly Dictionary<int, Dictionary<Type, StateData>> stateDatasByLayer = new();
    private readonly Dictionary<int, List<StateTransition<EntityType>>> anyTransitionsByLayer = new();
    private readonly Dictionary<int, StateData> currentStateDatasByLayer = new();
    private readonly SortedSet<int> layers = new();
    
    public EntityType Owner { get; private set; }

    public event StateChangedHandler onStateChanged;
    
    public void Setup(EntityType owner)
    {
        Debug.Assert(owner != null, $"StateMachine<{typeof(EntityType).Name}>::Setup - owner는 null이 될 수 없습니다.");

        Owner = owner;

        AddStates();
        MakeTransitions();
        SetupLayers();
    }
    
    public void SetupLayers()
    {
        foreach ((int layer, var statDatasByType) in stateDatasByLayer)
        {
            currentStateDatasByLayer[layer] = null;
            
            var firstStateData = statDatasByType.Values.First(x => x.Priority == 0);
            ChangeState(firstStateData);
        }
    }
    
    private void ChangeState(StateData newStateData)
    {
        var prevState = currentStateDatasByLayer[newStateData.Layer];

        prevState?.State.Exit();
        currentStateDatasByLayer[newStateData.Layer] = newStateData;
        newStateData.State.Enter();
        
        onStateChanged?.Invoke(this, newStateData.State, prevState.State, newStateData.Layer);
    }
    
    private void ChangeState(State<EntityType> newState, int layer)
    {
        var newStateData = stateDatasByLayer[layer][newState.GetType()];
        ChangeState(newStateData);
    }
    
    private bool TryTransition(IReadOnlyList<StateTransition<EntityType>> transtions, int layer)
    {
        foreach (var transition in transtions)
        {
            if (transition.TransitionCommand != StateTransition<EntityType>.kNullCommand || !transition.IsTransferable)
                continue;

            if (!transition.CanTrainsitionToSelf && currentStateDatasByLayer[layer].State == transition.ToState)
                continue;
            
            ChangeState(transition.ToState, layer);
            return true;
        }
        return false;
    }
    
    public void Update()
    {
        foreach (var layer in layers)
        {
            var currentStateData = currentStateDatasByLayer[layer];
            
            bool hasAnyTransitions = anyTransitionsByLayer.TryGetValue(layer, out var anyTransitions);
            
            if ((hasAnyTransitions && TryTransition(anyTransitions, layer)) ||
                TryTransition(currentStateData.Transitions, layer))
                continue;
            
            currentStateData.State.Update();
        }
    }
    
    public void AddState<T>(int layer = 0) where T : State<EntityType>
    {
        layers.Add(layer);
        
        var newState = Activator.CreateInstance<T>();
        newState.Setup(this, Owner, layer);
        
        if (!stateDatasByLayer.ContainsKey(layer))
        {
            stateDatasByLayer[layer] = new();
            anyTransitionsByLayer[layer] = new();
        }

        Debug.Assert(!stateDatasByLayer[layer].ContainsKey(typeof(T)),
            $"StateMachine::AddState<{typeof(T).Name}> - 이미 상태가 존재합니다.");

        var stateDatasByType = stateDatasByLayer[layer];
        stateDatasByType[typeof(T)] = new StateData(layer, stateDatasByType.Count, newState);
    }
    
    public void MakeTransition<FromStateType, ToStateType>(int transitionCommand, Func<State<EntityType>, bool> transitionCondition, int layer = 0)
        where FromStateType : State<EntityType> where ToStateType : State<EntityType>
    {
        var stateDatas = stateDatasByLayer[layer];
        var fromStateData = stateDatas[typeof(FromStateType)];
        var toStateData = stateDatas[typeof(ToStateType)];
        
        var newTransition = new StateTransition<EntityType>(fromStateData.State, toStateData.State,
            transitionCommand, transitionCondition, true);
        fromStateData.Transitions.Add(newTransition);
    }
    
    public void MakeTransition<FromStateType, ToStateType>(Enum transitionCommand,
        Func<State<EntityType>, bool> transitionCondition, int layer = 0)
        where FromStateType : State<EntityType>
        where ToStateType : State<EntityType>
        => MakeTransition<FromStateType, ToStateType>(Convert.ToInt32(transitionCommand), transitionCondition, layer);

    public void MakeTransition<FromStateType, ToStateType>(Func<State<EntityType>, bool> transitionCondition, int layer = 0)
        where FromStateType : State<EntityType>
        where ToStateType : State<EntityType>
        => MakeTransition<FromStateType, ToStateType>(StateTransition<EntityType>.kNullCommand, transitionCondition, layer);

    public void MakeTransition<FromStateType, ToStateType>(int transitionCommand, int layer = 0)
        where FromStateType : State<EntityType>
        where ToStateType : State<EntityType>
        => MakeTransition<FromStateType, ToStateType>(transitionCommand, null, layer);

    public void MakeTransition<FromStateType, ToStateType>(Enum transitionCommand, int layer = 0)
        where FromStateType : State<EntityType>
        where ToStateType : State<EntityType>
        => MakeTransition<FromStateType, ToStateType>(transitionCommand, null, layer);
    
    
    public void MakeAnyTransition<ToStateType>(int transitionCommand,
        Func<State<EntityType>, bool> transitionCondition, int layer = 0, bool canTransitonToSelf = false)
        where ToStateType : State<EntityType>
    {
        var stateDatasByType = stateDatasByLayer[layer];
        var state = stateDatasByType[typeof(ToStateType)].State;
        var newTransition = new StateTransition<EntityType>(null, state, transitionCommand, transitionCondition, canTransitonToSelf);
        anyTransitionsByLayer[layer].Add(newTransition);
    }
    
    public void MakeAnyTransition<ToStateType>(Enum transitionCommand,
        Func<State<EntityType>, bool> transitionCondition, int layer = 0, bool canTransitonToSelf = false)
        where ToStateType : State<EntityType>
        => MakeAnyTransition<ToStateType>(Convert.ToInt32(transitionCommand), transitionCondition, layer, canTransitonToSelf);

    public void MakeAnyTransition<ToStateType>(Func<State<EntityType>, bool> transitionCondition,
        int layer = 0, bool canTransitonToSelf = false)
        where ToStateType : State<EntityType>
        => MakeAnyTransition<ToStateType>(StateTransition<EntityType>.kNullCommand, transitionCondition, layer, canTransitonToSelf);

    public void MakeAnyTransition<ToStateType>(int transitionCommand, int layer = 0, bool canTransitonToSelf = false)
        where ToStateType : State<EntityType>
        => MakeAnyTransition<ToStateType>(transitionCommand, null, layer, canTransitonToSelf);
    
    public void MakeAnyTransition<ToStateType>(Enum transitionCommand, int layer = 0, bool canTransitonToSelf = false)
        where ToStateType : State<EntityType>
        => MakeAnyTransition<ToStateType>(transitionCommand, null, layer, canTransitonToSelf);
    
    public bool ExecuteCommand(int transitionCommand, int layer)
    {
        var transition = anyTransitionsByLayer[layer].Find(x =>
            x.TransitionCommand == transitionCommand && x.IsTransferable);
        
        transition ??= currentStateDatasByLayer[layer].Transitions.Find(x =>
            x.TransitionCommand == transitionCommand && x.IsTransferable);
        
        if (transition == null)
            return false;
        
        ChangeState(transition.ToState, layer);
        return true;
    }
    
    public bool ExecuteCommand(Enum transitionCommand, int layer)
        => ExecuteCommand(Convert.ToInt32(transitionCommand), layer);
    
    public bool ExecuteCommand(int transitionCommand)
    {
        bool isSuccess = false;

        foreach (int layer in layers)
        {
            if (ExecuteCommand(transitionCommand, layer))
                isSuccess = true;
        }

        return isSuccess;
    }
    
    public bool ExecuteCommand(Enum transitionCommand)
        => ExecuteCommand(Convert.ToInt32(transitionCommand));
    
    public bool SendMessage(int message, int layer, object extraData = null)
        => currentStateDatasByLayer[layer].State.OnReceiveMessage(message, extraData);
    
    public bool SendMessage(Enum message, int layer, object extraData = null)
        => SendMessage(Convert.ToInt32(message), layer, extraData);
    
    public bool SendMessage(int message, object extraData = null)
    {
        bool isSuccess = false;
        foreach (int layer in layers)
        {
            if (SendMessage(message, layer, extraData))
                isSuccess = true;
        }
        return isSuccess;
    }
    
    public bool SendMessage(Enum message, object extraData = null)
        => SendMessage(Convert.ToInt32(message), extraData);
    
    public bool IsInState<T>() where T : State<EntityType>
    {
        foreach ((_, StateData data) in currentStateDatasByLayer)
        {
            if (data.State.GetType() == typeof(T))
                return true;
        }
        return false;
    }
    
    public bool IsInState<T>(int layer) where T : State<EntityType>
        => currentStateDatasByLayer[layer].State.GetType() == typeof(T);
    
    public State<EntityType> GetCurrentState(int layer = 0) => currentStateDatasByLayer[layer].State;

    public Type GetCurrentStateType(int layer = 0) => GetCurrentState(layer).GetType();

    protected virtual void AddStates() { }
    
    protected virtual void MakeTransitions() { }
}
