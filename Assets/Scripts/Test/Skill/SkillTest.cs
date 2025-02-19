using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Test
{
    [RequireComponent(typeof(Entity))]
    public class SkillTest : MonoBehaviour
    {
        [SerializeField]
        private Skill skill;

        private Skill skillClone;

        private void OnGUI()
        {
            var boxRect = new Rect(254f, 2f, 200f, 115f);
            GUI.Box(boxRect, string.Empty);

            if (!skillClone)
            {
                if (GUI.Button(new Rect(boxRect.x + 50f, 4f, 100f, 30f), "Start Skill Test"))
                    Test();
            }
            else
            {
                var labelRect = new Rect(boxRect.x + 2f, 4f, 200f, 30f);
                GUI.Label(labelRect, $"Skill: {skillClone.CodeName}");

                labelRect.y += 17f;
                GUI.Label(labelRect, $"Level: {skillClone.Level}");

                if (!skillClone.IsMaxLevel)
                {
                    var buttonRect = new Rect(labelRect.x + 165f, labelRect.y, 30f, 17f);
                    if (GUI.Button(buttonRect, "Up"))
                    {
                        skillClone.Level++;
                        foreach (var effect in skillClone.Effects)
                            Debug.Log($"Effect: {effect.CodeName}, Level {effect.Level}");
                    }
                }

                labelRect.y += 17f;
                GUI.Label(labelRect, skillClone.GetCurrentStateType().Name);

                labelRect.y += 17f;

                if (skillClone.IsSearchingTarget)
                    GUI.Label(labelRect, $"Searching Target...");
                else if (skillClone.IsInState<CastingState>())
                    GUI.Label(labelRect, $"Casting {skillClone.CurrentCastTime:F2} To {skillClone.CastTime}");
                else if (skillClone.IsInState<ChargingState>())
                {
                    GUI.Label(labelRect, $"Charging Duration {skillClone.CurrentChargeDuration:F2} To {skillClone.ChargeDuration}");

                    labelRect.y += 17f;
                    GUI.Label(labelRect, $"Charging Power: {skillClone.CurrentChargePower:F2}");

                    labelRect.y += 17f;
                    GUI.Label(labelRect, $"Useable: {skillClone.IsMinChargeCompleted}");
                }
                else if (skillClone.IsInState<CooldownState>())
                    GUI.Label(labelRect, $"Cooldown {skillClone.CurrentCooldown:F2} To 0");
            }
        }

        [ContextMenu("Test")]
        private void Test() => StartCoroutine("TestCoroutine");


        private IEnumerator TestCoroutine()
        {
            Debug.Log($"<color=yellow>[SkillTest] Start</color>");

            skillClone = skill.Clone() as Skill;
            skillClone.Setup(GetComponent<Entity>());

            Debug.Log($"Skill Code Name: {skillClone.CodeName}");
            Debug.Log($"Description: {skillClone.Description}");

            Debug.Log("Skill을 사용하려면 SpaceBar를 누르세요.");
            Debug.Log("Test를 종료하려면 S를 누르세요.");

            while (true)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    if (skillClone.IsUseable)
                        skillClone.Use();
                    else
                        Debug.Log("Skill을 사용할 수 없습니다.");
                }
                else if (Input.GetKeyDown(KeyCode.S))
                    break;

                skillClone.Update();
                yield return null;
            }

            Destroy(skillClone);
            skillClone = null;

            Debug.Log($"<color=green>[SkillTest] Success</color>");
        }
    }
}