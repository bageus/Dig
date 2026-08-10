using UnityEngine;

namespace Dig.Unity
{

public sealed partial class DigWorldItemVisual
{
    internal void ApplyLooseWorldFloorPose()
    {
        if (Model == null || Model.IsBuildingBox)
        {
            return;
        }

        for (int index = 0; index < _instances.Count; index++)
        {
            GameObject instance = _instances[index];
            if (!instance.activeSelf
                || Mathf.Abs(Vector3.Dot(instance.transform.up, Vector3.up)) <= 0.5f)
            {
                continue;
            }

            instance.transform.localRotation =
                DigWorldItemVisualPolicy.ResolveLooseWorldRotation(
                    Model,
                    instance.transform.localRotation);
        }

        RefreshInteractionColliderForCurrentPose();
    }

    private void RefreshInteractionColliderForCurrentPose()
    {
        EnsureCollider();
        Bounds local = DigWorldItemGrounding.ResolveLocalBounds(
            transform,
            Vector3.one);
        _interactionCollider!.center = local.center;
        _interactionCollider.size = new Vector3(
            Mathf.Max(0.28f, local.size.x + 0.10f),
            Mathf.Max(0.28f, local.size.y + 0.06f),
            Mathf.Max(0.28f, local.size.z + 0.10f));
    }
}

}
