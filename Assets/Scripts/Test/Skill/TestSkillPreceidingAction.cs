using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Test
{
    [System.Serializable]
    public class TestSkillPreceidingAction : SkillPrecedingAction
    {
        private int count;

        public override void Release(Skill skill)
            => count = 0;

        public override bool Run(Skill skill)
        {
            Debug.Log($"Preceding Action Count: {++count}");
            if (count == 100)
                return true;
            else
                return false;
        }

        public override object Clone() => new TestSkillPreceidingAction();
    }
}