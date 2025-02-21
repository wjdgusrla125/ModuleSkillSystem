using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum EntityControlType
{
    Player,
    AI
}

public class Entity : MonoBehaviour
{
    public delegate void TakeDamageHandler(Entity entity, Entity instigator, object causer, float damage);
    public delegate void DeadHandler(Entity entity);
    
    [SerializeField] private Category[] categories;
    [SerializeField] private EntityControlType controlType;

    //socket은 Entity 스크립트를 가진 GameObject의 자식 GameObject를 의미함
    //스킬의 발사 위치, 어떤 특정 위치를 저장해두고 외부에서 찾아오기 위해 존재함
    private Dictionary<string, Transform> socketsByName = new();
    
    public EntityControlType ControlType => controlType;
    public IReadOnlyList<Category> Categories => categories;
    public bool IsPlayer => controlType == EntityControlType.Player;
    public Animator Animator { get; private set; }
    
    
    public Stats Stats { get; private set; }
    public bool IsDead => Stats.HPStat != null && Mathf.Approximately(Stats.HPStat.DefaultValue, 0f);
    
    public EntityMovement Movement { get; private set; }
    
    public SkillSystem SkillSystem { get; private set; }
    
    public Entity Target { get; set; }
    
    public MonoStateMachine<Entity> StateMachine { get; private set; }
    
    public event TakeDamageHandler onTakeDamage;
    public event DeadHandler onDead;

    private void Awake()
    {
        Animator = GetComponent<Animator>();
        Stats = GetComponent<Stats>();
        Stats.Setup(this);
        
        Movement = GetComponent<EntityMovement>();
        Movement?.Setup(this);
        
        StateMachine = GetComponent<MonoStateMachine<Entity>>();
        StateMachine?.Setup(this);
        
        SkillSystem = GetComponent<SkillSystem>();
        SkillSystem?.Setup(this);
    }

    public void TakeDamage(Entity instigator, object causer, float damage)
    {
        if(IsDead) return;

        float prevValue = Stats.HPStat.DefaultValue;
        Stats.HPStat.DefaultValue -= damage;
        
        onTakeDamage?.Invoke(this, instigator, causer, damage);

        if (Mathf.Approximately(Stats.HPStat.DefaultValue, 0f))
            OnDead();
    }

    private void OnDead()
    {
        if (Movement)
        {
            Movement.enabled = false;
        }
        
        SkillSystem.CancelAll(true);
        
        onDead?.Invoke(this);
    }

    private Transform GetTransformSocket(Transform root, string socketName)
    {
        if (root.name == socketName) 
            return root;

        foreach (Transform child in root)
        {
            var socket = GetTransformSocket(child, socketName);
            
            if(socket) return socket;
        }
        
        return null;
    }

    public Transform GetTransformSocket(string socketName)
    {
        if (socketsByName.TryGetValue(socketName, out var socket)) 
            return socket;
        
        socket = GetTransformSocket(transform, socketName);
        
        if (socket)
            socketsByName[socketName] = socket;
        
        return socket;
    }

    public bool HasCategory(Category category) => categories.Any(x => x.ID == category.ID);
    
    public bool IsInState<T>() where T : State<Entity>
        => StateMachine.IsInState<T>();

    public bool IsInState<T>(int layer) where T : State<Entity>
        => StateMachine.IsInState<T>(layer);
}
