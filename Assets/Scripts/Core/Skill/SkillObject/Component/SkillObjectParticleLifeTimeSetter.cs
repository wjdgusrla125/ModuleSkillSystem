using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("SkillObject/ParticleLifeTimeSetter")]
public class SkillObjectParticleLifeTimeSetter : MonoBehaviour, ISkillObjectComponent
{
    [SerializeField]
    private bool isUseApplyCycleForStartLifeTime;

    [Space]
    [SerializeField]
    private ParticleSystem[] particleSystems;

    public void OnSetupSkillObject(SkillObject skillObject)
    {
        foreach (var particleSystem in particleSystems)
        {
            particleSystem.Stop();
            var main = particleSystem.main;
            main.duration = skillObject.DestroyTime;
            main.startLifetime = isUseApplyCycleForStartLifeTime ? skillObject.ApplyCycle : skillObject.Duration;
            particleSystem.Play(false);
        }
    }
}