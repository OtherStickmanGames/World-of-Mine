using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CharacterAction : ScriptableObject
{
    /// <summary>
    /// �� ������ ����� ������ ����� ��������� ��������, ����� ���
    /// ��������� ����� ��������� �� ���� � ��� �� ��������� ��������
    /// �� ����, ���� �� ����� ��� ����
    /// </summary>
    public bool isDone { get; set; }

    public abstract void Execute(Transform character);
}
