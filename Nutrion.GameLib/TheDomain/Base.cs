using Nutrion.GameLib.Database;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.GameLib.TheDomain;

public interface IGameAction<T>
{
    Task<ValidationResult> ValidateAsync(AppDbContext db);
    Task<T> ExecuteAsync(AppDbContext db);
}

public record ValidationResult(bool IsValid, string? Message = null)
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Fail(string message) => new(false, message);
}
