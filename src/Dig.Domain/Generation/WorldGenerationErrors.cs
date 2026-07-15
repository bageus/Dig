using Dig.Domain.Core;

namespace Dig.Domain.Generation
{

public static class WorldGenerationErrors
{
    public static readonly DomainError UnsupportedGeneratorVersion = new DomainError(
        "generation.version.unsupported",
        "The requested generator version is not supported.");

    public static readonly DomainError UnknownMaterial = new DomainError(
        "generation.material.unknown",
        "A generation profile references a material that is not in the catalog.");

    public static readonly DomainError InvalidMaterialRole = new DomainError(
        "generation.material.invalid_role",
        "A generation material does not match its required solid or empty role.");

    public static readonly DomainError InvalidGeneratedWorld = new DomainError(
        "generation.world.invalid",
        "The generated world failed connectivity or starting-resource validation.");

    public static readonly DomainError IncompatibleOverlayWorld = new DomainError(
        "generation.overlay.incompatible_world",
        "The overlay world does not match the generated base world.");
}


}
