using UnityEngine;
using UnityEngine.Rendering;

namespace Dig.Unity
{
[DefaultExecutionOrder(-900)]
[DisallowMultipleComponent]
public sealed class DigStylizedLightingRig : MonoBehaviour
{
    private Transform? _root;
    private Light? _keyLight;
    private Light? _rimLight;

    public Light? KeyLight => _keyLight;
    public Light? RimLight => _rimLight;

    public void Configure()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.36f, 0.40f, 0.48f, 1f);
        if (_root == null)
        {
            _root = new GameObject("Stylized Lighting Rig").transform;
            _root.SetParent(transform, worldPositionStays: false);
        }
        _keyLight = EnsureDirectional(_keyLight, "Key Light",
            new Color(1f, 0.88f, 0.72f, 1f), 1.05f,
            Quaternion.Euler(52f, -34f, 0f));
        _rimLight = EnsureDirectional(_rimLight, "Rim Light",
            new Color(0.48f, 0.68f, 1f, 1f), 0.42f,
            Quaternion.Euler(128f, 146f, 0f));
    }

    private Light EnsureDirectional(Light? existing, string name,
        Color color, float intensity, Quaternion rotation)
    {
        Light light = existing!;
        if (light == null)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(_root, worldPositionStays: false);
            light = lightObject.AddComponent<Light>();
        }
        light.name = name;
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        light.transform.localRotation = rotation;
        light.enabled = true;
        return light;
    }
}
}
