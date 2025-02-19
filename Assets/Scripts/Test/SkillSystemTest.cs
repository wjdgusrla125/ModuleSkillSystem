using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillSystemTest : MonoBehaviour
{
    [SerializeField]
    private Skill testSkill;

    [ContextMenu("Test")]
    private void Test() => StartCoroutine("TestCoroutine");

    private IEnumerator TestCoroutine()
    {
        Debug.Log($"<color=yellow>[SkillSystemTest] Start</color>");

        var skillSystem = GetComponent<SkillSystem>();
        if (!skillSystem.Register(testSkill))
            Debug.LogAssertion($"{testSkill.CodeName}을 등록하지 못했습니다.");

        Debug.Log($"Skill 등록 성공: {testSkill.CodeName}");

        var skill = skillSystem.Find(testSkill);
        Debug.Assert(skill != null, $"{skill.CodeName}을 찾지 못했습니다.");

        Debug.Log("testSkill을 통해 skillSystem에 등록된 Skill을 검색에 성공.");
        Debug.Log($"{skill.CodeName} 사용 시도...");

        if (!skillSystem.Use(skill))
            Debug.LogAssertion($"{skill.CodeName}을 사용하지 못했습니다.");

        Debug.Log("Skill을 Unregister하고 Test를 종료하려면 S Key를 누르세요.");

        while (true)
        {
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (!skillSystem.Unregister(skill))
                    Debug.LogAssertion($"{skill.CodeName}을 등록 해제하지 못했습니다.");
                break;
            }
            yield return null;
        }

        Debug.Log($"Skill 해제 성공");
        Debug.Log($"<color=green>[SkillSystemTest] Success</color>");
    }
}