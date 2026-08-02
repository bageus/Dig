using UnityEngine;

namespace Dig.Unity
{

[DisallowMultipleComponent]
internal sealed class DigCombatHealthBar : MonoBehaviour
{
    private const float Width = 0.72f;
    private const float Height = 0.075f;
    private const float Depth = 0.025f;
    private static Material? _backgroundMaterial;
    private static Material? _fillMaterial;
    private Transform? _fill;
    private Camera? _camera;

    internal void Configure(
        int currentHealth,
        int maximumHealth,
        bool visible,
        Camera? camera,
        float verticalOffset)
    {
        EnsureVisuals();
        _camera = camera;
        transform.localPosition = new Vector3(0f, verticalOffset, 0f);
        gameObject.SetActive(visible && maximumHealth > 0 && currentHealth > 0);
        if (!gameObject.activeSelf)
        {
            return;
        }

        float normalized = Mathf.Clamp01((float)currentHealth / maximumHealth);
        _fill!.localScale = new Vector3(Width * normalized, Height, Depth * 0.8f);
        _fill.localPosition = new Vector3(
            -((Width - (Width * normalized)) * 0.5f),
            0f,
            -0.003f);
        FaceCamera();
    }

    private void LateUpdate()
    {
        FaceCamera();
    }

    private void FaceCamera()
    {
        Camera? target = _camera != null ? _camera : Camera.main;
        if (target == null)
        {
            return;
        }

        Vector3 direction = transform.position - target.transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    private void EnsureVisuals()
    {
        if (_fill != null)
        {
            return;
        }

        EnsureMaterials();
        GameObject background = CreatePart("Background", _backgroundMaterial!);
        background.transform.localScale = new Vector3(Width + 0.04f, Height + 0.04f, Depth);
        background.transform.localPosition = Vector3.zero;

        GameObject fill = CreatePart("Fill", _fillMaterial!);
        fill.transform.localScale = new Vector3(Width, Height, Depth * 0.8f);
        _fill = fill.transform;
    }

    private GameObject CreatePart(string name, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(transform, false);
        Collider? collider = part.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    private static void EnsureMaterials()
    {
        if (_backgroundMaterial != null && _fillMaterial != null)
        {
            return;
        }

        Shader shader = Shader.Find("Dig/Stylized Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Standard");
        _backgroundMaterial = CreateMaterial(
            shader,
            "CombatHealthBackground",
            new Color(0.12f, 0.04f, 0.04f, 0.95f));
        _fillMaterial = CreateMaterial(
            shader,
            "CombatHealthFill",
            new Color(0.18f, 0.88f, 0.28f, 1f));
    }

    private static Material CreateMaterial(
        Shader shader,
        string name,
        Color color)
    {
        Material material = new Material(shader)
        {
            name = name,
        };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        return material;
    }
}

}
