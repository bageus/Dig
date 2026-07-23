# Depth excavation designation highlight

Depth excavation designations use the same designation overlay semantic as other excavation cells. The marker is projected through `DigTunnelProjection.FloorWorldPosition` with the authoritative `CellId.X/Y/Z`, so a designation for a deeper layer is rendered on that layer's tunnel floor instead of the legacy top-down plane.
