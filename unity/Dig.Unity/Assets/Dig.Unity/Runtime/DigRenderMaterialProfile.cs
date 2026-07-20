using System;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[Serializable]
public sealed class DigRenderMaterialProfile
{
    [SerializeField] private RenderMaterialSemantic semantic;
    [SerializeField] private RenderSurfaceKind surface = RenderSurfaceKind.Lit;
    [SerializeField] private Material? material;
    [SerializeField] private Color fallbackTint = Color.magenta;

    public RenderMaterialSemantic Semantic => semantic;
    public RenderSurfaceKind Surface => surface;
    public Material? Material => material;
    public Color FallbackTint => fallbackTint;
    public string StableKey => semantic + ":" + surface;

    public void Validate()
    {
        if (!Enum.IsDefined(typeof(RenderMaterialSemantic), semantic))
            throw new InvalidOperationException("Render material semantic is invalid.");
        if (!Enum.IsDefined(typeof(RenderSurfaceKind), surface))
            throw new InvalidOperationException("Render surface kind is invalid.");
        if (material != null && !material.enableInstancing)
            throw new InvalidOperationException("Authored render material must enable instancing.");
    }
}
}
