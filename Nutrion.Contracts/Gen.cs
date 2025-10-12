using TypeGen.Core.SpecGeneration;

namespace Nutrion.Contracts;

public class GameContractsSpec : GenerationSpec
{
    public GameContractsSpec()
    {
        AddClass<TileUpdated>();
        AddClass<PlayerState>();
        AddClass<ResourceRate>();
    }
}
