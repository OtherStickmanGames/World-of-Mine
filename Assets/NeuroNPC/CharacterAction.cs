using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterAction : ScriptableObject
{
    /// <summary>
    /// Не забыть потом делать копии скриптебл объектов, иначе все
    /// персонажи будут ссылаться на один и тот же экземпляр действия
    /// то есть, пока он общий для всех
    /// </summary>
    public bool isDone { get; set; }

    public abstract void Execute(Transform character);
}
