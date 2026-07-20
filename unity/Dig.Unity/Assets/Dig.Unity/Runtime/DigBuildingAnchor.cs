using System;
using UnityEngine;

namespace Dig.Unity
{
    public enum DigBuildingAnchorKind
    {
        Worker = 0,
        Visitor = 1,
        Input = 2,
        Output = 3,
        Storage = 4,
        Vfx = 5,
    }

    [Flags]
    public enum DigBuildingAnchorMask
    {
        None = 0,
        Worker = 1 << 0,
        Visitor = 1 << 1,
        Input = 1 << 2,
        Output = 1 << 3,
        Storage = 1 << 4,
        Vfx = 1 << 5,
    }

    [DisallowMultipleComponent]
    public sealed class DigBuildingAnchor : MonoBehaviour
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private DigBuildingAnchorKind kind;

        public string StableId => stableId;

        public DigBuildingAnchorKind Kind => kind;

        internal DigBuildingAnchorMask Mask => (DigBuildingAnchorMask)(1 << (int)kind);
    }
}
