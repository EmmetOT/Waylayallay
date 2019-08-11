using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Simplex
{
    public static class Serialization
    {
        public static void DumpToSerializableArray<T>(this HashSet<T> hashSet, out T[] array)
        {
            array = hashSet.ToArray();
        }

        public static void LoadFromSerializableArray<T>(this HashSet<T> hashSet, T[] array)
        {
            hashSet = new HashSet<T>(array);
        }

        public static void DumpToSerializableArrays<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, out TKey[] keys, out TValue[] values)
        {
            keys = new TKey[dictionary.Count];
            values = new TValue[dictionary.Count];//

            int i = 0;
            foreach (KeyValuePair<TKey, TValue> kvp in dictionary)
            {
                keys[i] = kvp.Key;
                values[i] = kvp.Value;

                ++i;
            }
        }

        public static void DumpToSerializableArrays<TKey, TValue>(this Dictionary<TKey, HashSet<TValue>> dictionary, out TKey[] keys, out TValue[][] values)
        {
            keys = new TKey[dictionary.Count];
            values = new TValue[dictionary.Count][];//

            int i = 0;
            foreach (KeyValuePair<TKey, HashSet<TValue>> kvp in dictionary)//
            {
                keys[i] = kvp.Key;
                values[i] = kvp.Value.ToArray();

                ++i;
            }
        }

        public static void LoadFromSerializedArrays<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey[] keys, TValue[] values)
        {
            if (dictionary == null)
                dictionary = new Dictionary<TKey, TValue>(keys.Length);
            else
                dictionary.Clear();

            for (int i = 0; i < keys.Length && i < values.Length; i++)
            {
                dictionary.Add(keys[i], values[i]);
            }
        }

        public static void LoadFromSerializedArrays<TKey, TValue>(this Dictionary<TKey, HashSet<TValue>> dictionary, TKey[] keys, TValue[][] values)
        {
            if (dictionary == null)
                dictionary = new Dictionary<TKey, HashSet<TValue>>(keys.Length);
            else
                dictionary.Clear();

            for (int i = 0; i < keys.Length && i < values.Length; i++)
            {
                dictionary.Add(keys[i], new HashSet<TValue>(values[i]));
            }
        }

        public static IntSet[] ToIntSets(this Dictionary<int, HashSet<int>> dictionary)
        {
            IntSet[] sets = new IntSet[dictionary.Count];

            int i = 0;

            foreach (KeyValuePair<int, HashSet<int>> kvp in dictionary)
                sets[i++] = new IntSet(kvp);

            return sets;
        }

        public static Dictionary<int, HashSet<int>> ToDictionary(this IntSet[] intSets)//
        {
            Dictionary<int, HashSet<int>> dictionary = new Dictionary<int, HashSet<int>>(intSets.Length);

            for (int i = 0; i < intSets.Length; i++)
            {
                if (!dictionary.ContainsKey(intSets[i].Key))
                    dictionary.Add(intSets[i].Key, new HashSet<int>(intSets[i].Values));//
            }

            return dictionary;
        }

        [Serializable]
        public class IntSet
        {
            [SerializeField]
            public int Key;

            [SerializeField]
            public int[] Values;

            public IntSet(KeyValuePair<int, HashSet<int>> kvp)
            {
                Key = kvp.Key;
                Values = kvp.Value.ToArray();
            }
        }
    }
}