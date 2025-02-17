using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public abstract class Condition<T> : ICloneable
{
    public abstract bool IsPass(T data);
    public abstract object Clone();
}