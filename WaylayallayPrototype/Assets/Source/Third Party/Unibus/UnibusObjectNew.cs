/*using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnibusEvent
{
    public delegate void OnEvent<T>(T action);

    public delegate void OnEvent();

    public delegate void OnEventWrapper(object _object);

    public delegate void OnNoParamEventWrapper();

    class DictionaryKey
    {
        public Type Type;
        public object Tag;

        public DictionaryKey(string tag, Type type)
        {
            Tag = tag;
            Type = type;
        }

        public override int GetHashCode()
        {
            return Tag.GetHashCode() ^ (Type == null ? 1 : Type.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is DictionaryKey)
            {
                DictionaryKey key = (DictionaryKey)obj;
                return Tag.Equals(key.Tag) && ((Type == null && key.Type == null) || Type == key.Type);
            }

            return false;
        }

        public override string ToString()
        {
            return Tag + ", " + Type;
        }
    }

    public class UnibusObject : Singleton<UnibusObject>
    {
        public const string DefaultTag = "default";

        private Dictionary<DictionaryKey, Dictionary<object, OnEventWrapper>> observerDictionary = new Dictionary<DictionaryKey, Dictionary<object, OnEventWrapper>>();

        private Dictionary<DictionaryKey, Dictionary<object, OnNoParamEventWrapper>> observerDictionaryNoParam = new Dictionary<DictionaryKey, Dictionary<object, OnNoParamEventWrapper>>();

        public void Subscribe<T>(OnEvent<T> eventCallback)
        {
            Subscribe(DefaultTag, eventCallback);
        }

        public void Subscribe(OnEvent eventCallback)
        {
            Subscribe(DefaultTag, eventCallback);
        }

        public void Subscribe<T>(string tag, OnEvent<T> eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T));

            if (!observerDictionary.ContainsKey(key))
            {
                observerDictionary[key] = new Dictionary<object, OnEventWrapper>();
            }

            if (observerDictionary[key].ContainsKey(eventCallback))
            {
                Debug.LogError("Unibus::Subscribe Already added this callback: " + eventCallback.Target + " - " + eventCallback.Method);
                return;
            }

            observerDictionary[key][eventCallback] = (object _object) => { eventCallback((T) _object); };

#if UNITY_EDITOR
            // add debug UI
            DebugEvent ev = Events.Find((x) => x.EventKey.Equals(key));

            if (ev == null)
            {
                ev = new DebugEvent(key, tag.ToString() + " (" + typeof(T).ToString() + ")");
                Events.Add(ev);
                Events.Sort((k, j) => (k.Name.CompareTo(j.Name)));
            }

            DebugEventListener listener = new DebugEventListener();
            listener.HashCode = eventCallback.GetHashCode();
            listener.Target = eventCallback.Target as Component;
            listener.TargetName = eventCallback.Target.ToString();
            listener.MethodName = eventCallback.Method.ToString();
            ev.Listeners.Add(listener);
#endif
        }

        public void Subscribe(string tag, OnEvent eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, null);

            if (!observerDictionaryNoParam.ContainsKey(key))
            {
                observerDictionaryNoParam[key] = new Dictionary<object, OnNoParamEventWrapper>();
            }

            if (observerDictionaryNoParam[key].ContainsKey(eventCallback))
            {
                Debug.LogError("Unibus::Subscribe Already added this callback: " + eventCallback.Target + " - " + eventCallback.Method);
                return;
            }

            observerDictionaryNoParam[key][eventCallback] = () => { eventCallback(); };

#if UNITY_EDITOR
            // add debug UI
            DebugEvent ev = Events.Find((x) => x.EventKey.Equals(key));
            if (ev == null)
            {
                ev = new DebugEvent(key, tag.ToString() + " (None)");
                Events.Add(ev);
                Events.Sort((k,j) => (k.Name.CompareTo(j.Name)));
            }

            DebugEventListener listener = new DebugEventListener();
            listener.HashCode = eventCallback.GetHashCode();
            listener.Target = eventCallback.Target as Component;
            listener.TargetName = eventCallback.Target.ToString();
            listener.MethodName = eventCallback.Method.ToString();
            ev.Listeners.Add(listener);
#endif
        }

        public void Unsubscribe<T>(OnEvent<T> eventCallback)
        {
            Unsubscribe(DefaultTag, eventCallback);
        }

        public void Unsubscribe(OnEvent eventCallback)
        {
            Unsubscribe(DefaultTag, eventCallback);
        }

        public void Unsubscribe<T>(string tag, OnEvent<T> eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T));

            if (observerDictionary[key] != null)
            {
                observerDictionary[key].Remove(eventCallback);
            }

#if UNITY_EDITOR
            // clean up debug UI
            DebugEvent ev = Events.Find((x) => x.EventKey.Equals(key));
            if (ev != null)
            {
                ev.Listeners.RemoveAll((x) => x.HashCode == eventCallback.GetHashCode());
                if (ev.Listeners.Count == 0)
                {
                    Events.Remove(ev);
                }
            }
#endif
        }

        public void Unsubscribe(string tag, OnEvent eventCallback)
        {
            DictionaryKey key = new DictionaryKey(tag, null);

            if (observerDictionary[key] != null)
            {
                observerDictionary[key].Remove(eventCallback);
            }

            if (observerDictionaryNoParam[key] != null)
            {
                observerDictionaryNoParam[key].Remove(eventCallback);
            }

#if UNITY_EDITOR
            // clean up debug UI
            DebugEvent ev = Events.Find((x) => x.EventKey.Equals(key));
            if (ev != null)
            {
                ev.Listeners.RemoveAll((x) => x.HashCode == eventCallback.GetHashCode());
                if (ev.Listeners.Count == 0)
                {
                    Events.Remove(ev);
                }
            }
#endif
        }

        public void Dispatch<T>(T action)
        {
            Dispatch(DefaultTag, action);
        }

        public void Dispatch(string tag)
        {
            DictionaryKey key = new DictionaryKey(tag, null);

            // known issue - if dispatching an event directly leads to a new event being bound,
            // this will throw an out of sync error

            if (observerDictionaryNoParam.ContainsKey(key))
            {                
                // Q: Why use ToList()? Why not just iterate through the ValueCollection?
                // A: We need to create a copy of the collection because some unibus events
                // will indirectly lead to modification of the dictionary. This will lead to an out of
                // sync error.
                foreach (OnNoParamEventWrapper caller in observerDictionaryNoParam[key].Values.ToList())
                {
                    caller();
                }
            }
            else
            {
                if (MasterControl.Instance.DebugUnibus)
                {
                    // We are quite happy having no one listening to our events, this is desired behaviour, for now
                    StackTraceLogType stlt = Application.GetStackTraceLogType(LogType.Log);
                    string tagAndAction = string.Format("(tag:{0})", tag);
                    Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                    Debug.Log("Unibus.Dispatch failed to send: " + tagAndAction);
                    Application.SetStackTraceLogType(LogType.Log, stlt);
                }
            }
        }

        public void Dispatch<T>(string tag, T action)
        {
            DictionaryKey key = new DictionaryKey(tag, typeof(T));

            // known issue - if dispatching an event directly leads to a new event being bound,
            // this will throw an out of sync error

            if (observerDictionary.ContainsKey(key))
            {
                // Q: Why use ToList()? Why not just iterate through the ValueCollection?
                // A: We need to create a copy of the collection because some unibus events
                // will indirectly lead to modification of the dictionary. This will lead to an out of
                // sync error.
                foreach (OnEventWrapper caller in observerDictionary[key].Values.ToList())
                {
                    caller(action);
                }
            }
            else
            {
                if (MasterControl.Instance.DebugUnibus)
                {
                    // We are quite happy having no one listening to our events, this is desired behaviour, for now
                    StackTraceLogType stlt = Application.GetStackTraceLogType(LogType.Log);
                    string tagAndAction = string.Format("(tag:{0}, action:{1})", tag, action);
                    Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
                    Debug.Log("Unibus.Dispatch failed to send: " + tagAndAction);
                    Application.SetStackTraceLogType(LogType.Log, stlt);
                }
            }
        }

// START BS CHANGE (Tim)
#if UNITY_EDITOR
        // simple Editor inspector to figure out who is listening to what event
        [Serializable]
        class DebugEvent
        {
            public string Name;
            [NonSerialized]
            public DictionaryKey EventKey;
            public List<DebugEventListener> Listeners = new List<DebugEventListener>();

            public DebugEvent(DictionaryKey key, string name)
            {
                Name = name;
                EventKey = key;
            }
        }

        [Serializable]
        class DebugEventListener
        {
            public string TargetName;
            public Component Target;
            public string MethodName;
            [NonSerialized]
            public int HashCode;
        }

        [SerializeField]
        private List<DebugEvent> Events = new List<DebugEvent>();
#endif
// END BS CHANGE
        }
}
*/