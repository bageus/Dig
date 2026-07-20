namespace Dig.Presentation.Rendering
{
public enum RenderSurfaceKind { Lit = 0, Unlit = 1, Overlay = 2 }
public enum RenderMaterialSemantic
{
    Terrain = 0, Resident = 1, ResidentSelected = 2,
    ResidentEquipment = 3, Building = 4, Item = 5,
    Creature = 6, Overlay = 7, Emissive = 8, Vfx = 9,
}
public enum VfxCategory
{
    Excavation = 0, Deposit = 1, Construction = 2,
    Production = 3, Status = 4, Combat = 5, Ambient = 6,
}
public enum VfxPriority { Ambient = 0, Normal = 1, Important = 2, Critical = 3 }
public enum RealtimeLightKind { Point = 0, Spot = 1 }
public enum RealtimeLightPriority { Ambient = 0, Normal = 1, Focused = 2, Critical = 3 }
}
