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
        ["Gold"] = 1000000000,
        ["Wood"] = 1000000000,
        ["Stone"] = 1000000000
    };

}
