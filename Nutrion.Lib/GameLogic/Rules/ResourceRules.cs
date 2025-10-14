using Nutrion.Lib.Database.Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nutrion.Lib.GameLogic.Rules;

public static class ResourceRules
{
    // Define base regeneration rates per resource type
    public static readonly Dictionary<string, double> RegenerationRatesPerMinute = new()
    {
        ["Gold"] = 2.0,
        ["Wood"] = 1.0,
        ["Stone"] = 0.5
    };

    // Define hard caps for each resource
    public static readonly Dictionary<string, int> MaxQuantities = new()
    {
        ["Gold"] = 5000,
        ["Wood"] = 2000,
        ["Stone"] = 2000
    };

    // Fun: define special bonuses
    public static int GetBonus(Player player)
    {
        // Just for flavor — color-coded multipliers
        return player.Color switch
        {
            "#FFD700" => 2,  // gold players earn double gold
            "#228B22" => 2,  // green players earn double wood
            _ => 1
        };
    }
}
