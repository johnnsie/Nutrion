using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nutrion.GameLib.Database;
using Nutrion.GameLib.Database.Entities;
using Nutrion.GameLib.Database.EntityRepository;
using Nutrion.GameLib.TheDomain;
using Nutrion.GameLib.TheDomain.Actions;
using Nutrion.Lib.Database;
using Nutrion.Lib.GameLogic.Helpers;
using Nutrion.Lib.GameLogic.Validation;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TypeGen.Core.Logging;

namespace Nutrion.Lib.GameLogic.Systems;

public interface IBuildingSystem
{
    Task<Building?> CreateBuildingActionAsync(string sessionId, Guid buildingTypeId, int tileId);
}

public class BuildingSystem : IBuildingSystem
{
    private readonly ILogger<BuildingSystem> _logger;
    private readonly AppDbContext _db;
    private readonly EntityRepository _repo;
    private readonly BuildingValidator _validator;
    private readonly GameActionService _gameActionService;


    public BuildingSystem(
        ILogger<BuildingSystem> logger,
        EntityRepository repo,
        AppDbContext db,
        BuildingValidator validator,
        GameActionService gameActionService)
    {
        _logger = logger;
        _repo = repo;
        _db = db;
        _validator = validator;
        _gameActionService = gameActionService;
    }


    public async Task<Building?> CreateBuildingActionAsync(string sessionId, Guid buildingTypeId, int tileId)
    {
        var constructAction = new ConstructBuildingAction
        {
            SessionId = sessionId,
            BuildingTypeId = buildingTypeId,
            OriginTileId = tileId
        };

        var result = await _gameActionService.ExecuteAsync(constructAction);

        if (!result.Success)
            _logger.LogWarning("Building failed: {Msg}", result.Message);
        else
            _logger.LogInformation("Building successfully constructed.");

        return result.Data;
    }

}

