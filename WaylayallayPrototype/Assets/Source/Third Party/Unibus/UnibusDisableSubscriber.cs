using UnityEngine;
using System;
using System.Collections;

namespace UnibusEvent
{
    public class UnibusDisableSubscriber : UnibusSubscriberBase
    {
        private void OnEnable()
        {
            if (subscribeCaller != null)
            {
                subscribeCaller(true);
            }
        }

        private void OnDisable()
        {
            if (subscribeCaller != null)
            {
                subscribeCaller(false);
            }
        }
    }
}
