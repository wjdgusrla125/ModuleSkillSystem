using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityFloatingTextConnector : MonoBehaviour
{
    [SerializeField]
    private Transform textSpawnPoint;

    private Entity entity;    

    private void Start()
    {
        entity = GetComponent<Entity>();
        entity.onTakeDamage += OnTakeDamage;
        entity.StateMachine.onStateChanged += OnStateChanged;
        entity.Stats.HPStat.onValueChanged += OnHPValueChanged;
    }

    private void OnTakeDamage(Entity entity, Entity instigator, object causer, float damage)
    {
        FloatingTextView.Instance.Show(textSpawnPoint, $"-{Mathf.RoundToInt(damage)}", Color.red);
    }

    private void OnStateChanged(StateMachine<Entity> stateMachine, State<Entity> newState, State<Entity> prevState, int layer)
    {
        var ccState = newState as EntityCCState;
        if (ccState == null)
            return;

        FloatingTextView.Instance.Show(textSpawnPoint, ccState.Description, Color.magenta);
    }

    private void OnHPValueChanged(Stat stat, float currentValue, float prevValue)
    {
        var value = currentValue - prevValue;
        if (value > 0)
            FloatingTextView.Instance.Show(textSpawnPoint, $"+{Mathf.RoundToInt(value)}", Color.green);
    }
}
