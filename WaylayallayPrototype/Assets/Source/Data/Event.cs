using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sone
{
    public static class Event
    {
        public const string OnCodeHeldPerFrame = "OnCodeHeldPerFrame";
        public const string OnCodeHeldFixed = "OnCodeHeldFixed";
        public const string OnCodeUp = "OnCodeUp";
        public const string OnCodeDown = "OnCodeDown";

        public const string OnMove = "OnMove";

        public const string OnVolumeEntered = "OnVolumeEntered";
        public const string OnVolumeExited = "OnVolumeExited";

        public const string OnTimeStep = "OnTimeStep";

        public const string FullyRecalculateSplittableMeshes = "FullyRecalculateSplittableMeshes";
        public const string SetSplittableMeshStretching = "SetSplittableMeshStretching";
        public const string BakeSplitMeshes = "BakeSplitMeshes";
    }
}