using UnityEngine;
using System;

namespace UnibusEvent
{
    public class UnibusSubscriberBase : MonoBehaviour
    {
        protected Action<bool> subscribeCaller;

        public void SetSubscribeCaller<T>(string tag, OnEvent<T> onEvent)
        {
            subscribeCaller = (bool active) =>
            {
                if (active)
                    UnibusObject.Instance.Subscribe(tag, onEvent);
                else if (UnibusObject.IsExistInstance())
                    UnibusObject.Instance.Unsubscribe(tag, onEvent);
            };

            subscribeCaller(true);
        }

        public void SetSubscribeCaller(string tag, OnEvent onEvent)
        {
            subscribeCaller = (bool active) =>
            {
                if (active)
                    UnibusObject.Instance.Subscribe(tag, onEvent);
                else if (UnibusObject.IsExistInstance())
                    UnibusObject.Instance.Unsubscribe(tag, onEvent);
            };

            subscribeCaller(true);
        }
    }
}