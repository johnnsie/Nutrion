using Nutrion.GameLib.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameLib.TheDomain;

public class GameActionService
{
    private readonly AppDbContext _db;
    public GameActionService(AppDbContext db) => _db = db;

    public async Task<ActionResult<T>> ExecuteAsync<T>(IGameAction<T> action)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var validation = await action.ValidateAsync(_db);
        if (!validation.IsValid)
            return ActionResult<T>.Fail(validation.Message!);

        var result = await action.ExecuteAsync(_db);

        await tx.CommitAsync();
        return ActionResult<T>.SuccessResult(result);
    }
}

public record ActionResult<T>(bool Success, string? Message = null, T? Data = default)
{
    public static ActionResult<T> SuccessResult(T data) => new(true, null, data);
    public static ActionResult<T> Fail(string message) => new(false, message);
}
