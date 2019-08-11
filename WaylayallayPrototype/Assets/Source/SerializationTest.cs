using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;
using Simplex;

public class SerializationTest : MonoBehaviour
{
    [SerializeField]
    private Serialization.IntSet[] m_testOne;
    
    [SerializeField]
    private OuterClass m_outerClass = new OuterClass();

    [Button]
    public void SetRandomInts()
    {
        m_outerClass.SetRandomInts(10);
    }

    [Button]
    public void Print()
    {
        m_outerClass.Print();
    }
}

[System.Serializable]
public class OuterClass : ISerializationCallbackReceiver
{
    [SerializeField]
    private Dictionary<int, InnerClass> m_innerClasses = new Dictionary<int, InnerClass>();//

    [SerializeField]
    private int[] m_serializedKeys;

    [SerializeField]
    private InnerClass[] m_serializedValues;

    public void SetRandomInts(int count)
    {
        m_innerClasses.Clear();

        for (int i = 0; i < count; i++)
        {
            InnerClass inner = new InnerClass();//
            inner.Set();

            m_innerClasses.Add(i, inner);
        }
    }

    public void Print()
    {
        for (int i = 0; i < m_innerClasses.Count; i++)//
        {
            m_innerClasses[i].Print(i);
        }
    }

    public void OnBeforeSerialize()
    {
#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying
        && !UnityEditor.EditorApplication.isUpdating
        && !UnityEditor.EditorApplication.isCompiling)
            return;
#endif

        Debug.Log("OnBeforeSerialize");

        m_innerClasses.DumpToSerializableArrays(out m_serializedKeys, out m_serializedValues);//
    }

    public void OnAfterDeserialize()
    {
        Debug.Log("OnAfterDeserialize");

        m_innerClasses.LoadFromSerializedArrays(m_serializedKeys, m_serializedValues);
    }
}

[System.Serializable]
public class InnerClass
{
    [SerializeField]
    private int m_int;

    public void Set()
    {
        m_int = Random.Range(0, 10);
    }

    public void Print(int index)
    {
        Debug.Log(index + ": " + m_int);
    }
}

[System.Serializable]
public class Serializable2DArray<T>
{
    [SerializeField]
    public T[] Array;

    public T this[int i]
    {
        get
        {
            return Array[i];
        }
    }
}
