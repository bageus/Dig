using System;
using System.Collections.Generic;
using Dig.Presentation.Rendering;
using UnityEngine;

namespace Dig.Unity
{
[DisallowMultipleComponent]
public sealed class DigRealtimeLightPool : MonoBehaviour
{
    private readonly List<Light> _lights = new List<Light>();
    private Transform? _root;
    private RenderFrameBudget _budget = RenderFrameBudget.Default;

    public int ActiveCount { get; private set; }
    public int PoolSize => _lights.Count;

    public void SetBudget(RenderFrameBudget budget)
    {
        _budget = budget ?? throw new ArgumentNullException(nameof(budget));
        EnsureCapacity(_budget.MaximumRealtimeLights);
    }

    public void Render(IReadOnlyList<LightRequest> requests, Camera? camera)
    {
        if (requests == null) throw new ArgumentNullException(nameof(requests));
        EnsureCapacity(_budget.MaximumRealtimeLights);
        Vector3 focus = camera == null ? Vector3.zero : camera.transform.position;
        RenderBudgetPlan plan = RenderBudgetPlan.Create(
            Array.Empty<EffectSpawnRequest>(), requests, _budget,
            focus.x, focus.y, focus.z);
        ActiveCount = plan.Lights.Count;
        for (int index = 0; index < _lights.Count; index++)
        {
            Light light = _lights[index];
            if (index >= plan.Lights.Count)
            {
                light.enabled = false;
                continue;
            }
            Apply(light, plan.Lights[index]);
        }
    }

    private void EnsureCapacity(int capacity)
    {
        if (_root == null)
        {
            _root = new GameObject("Realtime Light Pool").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }
        while (_lights.Count < capacity)
        {
            GameObject lightObject = new GameObject("Pooled Realtime Light " + _lights.Count);
            lightObject.transform.SetParent(_root, worldPositionStays: false);
            Light light = lightObject.AddComponent<Light>();
            light.enabled = false;
            light.renderMode = LightRenderMode.ForcePixel;
            _lights.Add(light);
        }
    }

    private static void Apply(Light light, SelectedLight selected)
    {
        LightRequest request = selected.Request;
        Transform target = light.transform;
        target.localPosition = new Vector3((float)request.WorldX,
            (float)request.WorldY, (float)request.WorldZ);
        target.localRotation = Quaternion.Euler(90f, 0f, 0f);
        light.type = request.Kind == RealtimeLightKind.Spot
            ? LightType.Spot : LightType.Point;
        light.range = (float)request.Range;
        light.intensity = (float)request.Intensity;
        light.color = new Color((float)request.Red,
            (float)request.Green, (float)request.Blue, 1f);
        light.shadows = selected.ShadowsEnabled ? LightShadows.Soft : LightShadows.None;
        light.enabled = true;
    }
}
}
