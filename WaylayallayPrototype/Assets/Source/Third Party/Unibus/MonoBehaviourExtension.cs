using UnityEngine;
using System.Collections;
using UnibusEvent;

public static class MonoBehaviourExtension
{
    public static void BindUntilDisable<T>(this MonoBehaviour mono, string tag, OnEvent<T> onEvent)
    {
        GetOrAddComponent<T, UnibusDisableSubscriber>(mono, tag, onEvent);
    }
    
    public static void BindUntilDisable(this MonoBehaviour mono, string tag, OnEvent onEvent)
    {
        GetOrAddComponent<UnibusDisableSubscriber>(mono, tag, onEvent);
    }
    
    public static void BindUntilDestroy<T>(this MonoBehaviour mono, string tag, OnEvent<T> onEvent)
    {
        GetOrAddComponent<T, UnibusDestroySubscriber>(mono, tag, onEvent);
    }
    
    public static void BindUntilDestroy(this MonoBehaviour mono, string tag, OnEvent onEvent)
    {
        GetOrAddComponent<UnibusDestroySubscriber>(mono, tag, onEvent);
    }

    private static void GetOrAddComponent<T, S>(MonoBehaviour mono, string tag, OnEvent<T> onEvent) where S : UnibusSubscriberBase
    {
        S component = mono.GetComponent<S>();

        if (component == null)
            component = mono.gameObject.AddComponent<S>();

        component.SetSubscribeCaller(tag, onEvent);
    }

    private static void GetOrAddComponent<S>(MonoBehaviour mono, string tag, OnEvent onEvent) where S : UnibusSubscriberBase
    {
        S component = mono.GetComponent<S>();

        if (component == null)
            component = mono.gameObject.AddComponent<S>();

        component.SetSubscribeCaller(tag, onEvent);
    }
}

