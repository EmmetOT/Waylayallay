using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Simplex;

namespace UnibusEvent
{
    public static class Unibus
    {
        //public static void Subscribe<T>(string tag, OnEvent<OwnedData<T>> eventCallback, Base owner)
        //{
        //    UnibusObject.Instance?.Subscribe(tag, eventCallback, owner);
        //}

        //public static void Subscribe(string tag, OnEvent<Base> eventCallback, Base owner)
        //{
        //    UnibusObject.Instance?.Subscribe(tag, eventCallback, owner);
        //}

        public static void Subscribe<T, U>(string tag, OnEvent<T, U> eventCallback, Base owner = null)
        {
            UnibusObject.Instance?.Subscribe(tag, eventCallback, owner);
        }

        public static void Subscribe<T>(string tag, OnEvent<T> eventCallback, Base owner = null)
        {
            UnibusObject.Instance?.Subscribe(tag, eventCallback, owner);
        }

        public static void Subscribe(string tag, OnEvent eventCallback, Base owner = null)
        {
            UnibusObject.Instance?.Subscribe(tag, eventCallback, owner);
        }

        //public static void Unsubscribe<T>(string tag, OnEvent<OwnedData<T>> eventCallback, Base owner)
        //{
        //    UnibusObject.Instance?.Unsubscribe(tag, eventCallback, owner);
        //}

        //public static void Unsubscribe(string tag, OnEvent<Base> eventCallback, Base owner)
        //{
        //    UnibusObject.Instance?.Unsubscribe(tag, eventCallback, owner);
        //}

        public static void Unsubscribe<T, U>(string tag, OnEvent<T, U> eventCallback, Base owner = null)
        {
            UnibusObject.Instance?.Unsubscribe(tag, eventCallback, owner);
        }

        public static void Unsubscribe<T>(string tag, OnEvent<T> eventCallback, Base owner = null)
        {
            UnibusObject.Instance?.Unsubscribe(tag, eventCallback, owner);
        }

        public static void Unsubscribe(string tag, OnEvent eventCallback, Base owner = null)
        {
            UnibusObject.Instance?.Unsubscribe(tag, eventCallback, owner);
        }

        //public static void Dispatch<T>(string tag, OwnedData<T> action)
        //{
        //    UnibusObject.Instance?.Dispatch(tag, action);
        //}

        //public static void Dispatch(string tag, Base action)
        //{
        //    UnibusObject.Instance?.Dispatch(tag, action);
        //}

        public static void Dispatch<T, U>(string tag, T action1, U action2, Base owner = null)
        {
            UnibusObject.Instance?.Dispatch(tag, action1, action2, owner);
        }

        public static void Dispatch<T>(string tag, T action, Base owner = null)
        {
            UnibusObject.Instance?.Dispatch(tag, action, owner);
        }

        public static void Dispatch(string tag, Base owner = null)
        {
            UnibusObject.Instance?.Dispatch(tag, owner);
        }
    }
}