using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Test
{
    [AddComponentMenu("Test/StatTest")]
    public class StatTest : MonoBehaviour
    {
        [ContextMenu("Test")]
        private void Test()
        {
            Debug.Log("<color=yellow>[StatTest] Start</color>");

            var stat = ScriptableObject.CreateInstance<Stat>();
            stat.MaxValue = float.MaxValue;

            stat.SetBonusValue("Test", 10f);
            Assert.IsTrue(stat.ContainsBonusValue("Test"), "Test Bonus Value가 없습니다.");
            Assert.IsTrue(Mathf.Approximately(stat.GetBonusValue("Test"), 10f), "Stat의 Test Bonus Value가 10이 아닙니다.");
            Debug.Log($"Test Bonus Value: {stat.GetBonusValue("Test")}");

            Assert.IsTrue(stat.RemoveBonusValue("Test"), "Test Bonus Value의 삭제 실패");
            Assert.IsFalse(stat.ContainsBonusValue("Test"), "Test Bonus Value를 삭제하였으나 아직 남아있습니다.");
            Debug.Log("Remove Test Bonus Value");

            stat.SetBonusValue("Test", "Test2", 10f);
            Assert.IsTrue(stat.ContainsBonusValue("Test", "Test2"), "Test-Test2 Bonus Value가 없습니다.");
            Assert.IsTrue(Mathf.Approximately(stat.GetBonusValue("Test", "Test2"), 10f), "Test-Test2 Bonus Value가 10이 아닙니다.");
            Debug.Log($"Test-Test2 Bonus Value: {stat.GetBonusValue("Test", "Test2")}");

            Assert.IsTrue(stat.RemoveBonusValue("Test", "Test2"), "Test-Test2 Bonus Value의 삭제 실패");
            Assert.IsFalse(stat.ContainsBonusValue("Test", "Test2"), "Test-Test2 Bonus Value를 삭제하였으나 아직 남아있습니다.");
            Debug.Log("Remove Test-Test2 Bonus Value");

            stat.RemoveBonusValue("Test");
            Debug.Log("Remove Test Bonus Value");

            stat.SetBonusValue("Test", 100f);
            Debug.Log("Set Test Bonus: " + stat.GetBonusValue("Test"));
            stat.SetBonusValue("Test2", 100f);
            Debug.Log("Set Test2 Bonus: " + stat.GetBonusValue("Test2"));
            Assert.IsTrue(Mathf.Approximately(stat.BonusValue, 200f), "Bonus Value의 합계가 200이 아닙니다.");
            Debug.Log("Total Bonus Value: 200");

            stat.DefaultValue = 100f;
            Debug.Log("Set Default Value: " + stat.DefaultValue);
            Assert.IsTrue(Mathf.Approximately(stat.Value, 300f), "Total Value가 300이 아닙니다.");
            Debug.Log("Value: 300");

            if (Application.isPlaying)
                Destroy(stat);
            else
                DestroyImmediate(stat);

            Debug.Log("<color=green>[StatTest] Success</color>");
        }
    }
}