using System;
using System.Collections.Generic;
using Dig.Presentation.World;
using UnityEngine;

namespace Dig.Unity
{
    public enum DigCaveTemplateProfileKind
    {
        Small = 0,
        Medium = 1,
        Large = 2,
        Tall = 3,
    }

    [Serializable]
    public sealed class DigCaveTemplateVisualProfile
    {
        [SerializeField]
        private string stableId = string.Empty;

        [SerializeField]
        private DigCaveTemplateProfileKind kind;

        [SerializeField]
        private Material? entranceMaterial;

        [SerializeField]
        private Material? archMaterial;

        [SerializeField]
        private Material? sideWallMaterial;

        [SerializeField]
        private Material? backWallMaterial;

        public string StableId => stableId;

        public DigCaveTemplateProfileKind Kind => kind;

        internal Material? Resolve(CaveTemplateTrimRole role)
        {
            switch (role)
            {
                case CaveTemplateTrimRole.Entrance:
                    return entranceMaterial;
                case CaveTemplateTrimRole.Arch:
                    return archMaterial;
                case CaveTemplateTrimRole.SideWall:
                    return sideWallMaterial;
                case CaveTemplateTrimRole.BackWall:
                    return backWallMaterial;
                default:
                    return null;
            }
        }

        internal void AppendValidation(int index, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(stableId))
            {
                errors.Add($"Cave template profile {index} has no stable id.");
            }

            if (!Enum.IsDefined(typeof(DigCaveTemplateProfileKind), kind))
            {
                errors.Add($"Cave template profile {index} has an invalid kind.");
            }

            RequireMaterial(entranceMaterial, "entrance", errors);
            RequireMaterial(archMaterial, "arch", errors);
            RequireMaterial(sideWallMaterial, "side wall", errors);
            RequireMaterial(backWallMaterial, "back wall", errors);
        }

        private void RequireMaterial(
            Material? material,
            string role,
            ICollection<string> errors)
        {
            if (material == null)
            {
                errors.Add(
                    $"Cave template profile '{stableId}' has no {role} material.");
            }
        }
    }
}
