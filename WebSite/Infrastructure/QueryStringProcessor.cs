﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Server.Infrastructure
{
    internal static class QueryStringProcessor
    {
        internal static T GetEnum<T>(IEnumerable<KeyValuePair<string, string>> parameters, string name, T defaultValue)
        {
            if (parameters.Any(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant()))
            {
                var match = parameters.First(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant());
                return (T)Enum.Parse(typeof(T), match.Value, ignoreCase: true);
            }
            return defaultValue;
        }

        internal static int GetInt(IEnumerable<KeyValuePair<string, string>> parameters, string name, int defaultValue)
        {
            if (parameters.Any(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant()))
            {
                var match = parameters.First(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant());
                return int.Parse(match.Value, NumberStyles.Integer);
            }
            return defaultValue;
        }

        internal static string GetString(IEnumerable<KeyValuePair<string, string>> parameters, string name, string defaultValue)
        {
            if (parameters.Any(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant()))
            {
                var match = parameters.First(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant());
                return match.Value;
            }
            return defaultValue;
        }

        internal static bool GetBool(IEnumerable<KeyValuePair<string, string>> parameters, string name, bool defaultValue)
        {
            if (parameters.Any(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant()))
            {
                var match = parameters.First(p => p.Key.ToLowerInvariant() == name.ToLowerInvariant());
                if (String.Compare(match.Value, "true", ignoreCase: true) == 0)
                    return true;
                if (String.Compare(match.Value, "fakse", ignoreCase: true) == 0)
                    return false;
            }
            return defaultValue;
        }
    }
}