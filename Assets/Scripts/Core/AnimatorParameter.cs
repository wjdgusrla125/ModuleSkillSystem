using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AnimatorParameterType
{
    Bool,
    Trigger
}

[System.Serializable]
public struct AnimatorParameter
{
    public AnimatorParameterType type;
    public string name;

    private int hash;

    public bool IsValid => !string.IsNullOrEmpty(name);
    public int Hash
    {
        get
        {
            if (hash == 0 && IsValid)
                hash = Animator.StringToHash(name);
            return hash;
        }
    }
}