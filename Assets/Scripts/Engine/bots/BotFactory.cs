using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ChessEngine
{
    // Finds bot classes by name: every class deriving from BotBase called Bot_vN is available as "vN".
    // New bot versions show up automatically, both in the game settings and in the UCI tool.
    public static class BotFactory
    {
        private static readonly Dictionary<string, Type> bots = typeof(BotBase).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BotBase)) && !t.IsAbstract && t.Name.StartsWith("Bot_"))
            .ToDictionary(t => t.Name.Substring("Bot_".Length), t => t, StringComparer.OrdinalIgnoreCase);

        // Sorted by version number: v0, v1, ..., v9, v10, v11 (not alphabetically)
        public static IReadOnlyList<string> AvailableBots() =>
            bots.Keys.OrderBy(VersionNumber).ThenBy(k => k).ToList();

        public static bool Exists(string name) => name != null && bots.ContainsKey(name);

        // Bots that search for a given time instead of a fixed depth
        public static bool IsTimed(string name) => typeof(ITimedBot).IsAssignableFrom(bots[name]);

        // Bots that take a search depth in the constructor (e.g. Bot_v2(int depth))
        public static bool HasDepth(string name) => DepthConstructor(bots[name]) != null;

        public static BotBase Create(string name, int depth)
        {
            Type type = bots[name];
            ConstructorInfo withDepth = DepthConstructor(type);
            return withDepth != null
                ? (BotBase)withDepth.Invoke(new object[] { depth })
                : (BotBase)Activator.CreateInstance(type);
        }

        private static ConstructorInfo DepthConstructor(Type type) =>
            type.GetConstructors().FirstOrDefault(c =>
                c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == typeof(int));

        private static int VersionNumber(string name) =>
            name.Length > 1 && int.TryParse(name.Substring(1), out int number) ? number : int.MaxValue;
    }
}
