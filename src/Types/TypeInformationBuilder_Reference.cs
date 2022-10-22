namespace DotnetToMd.Metadata
{
    internal static partial class TypeInformationBuilder
    {
        private static TypeInformation CreateGenericParameterTypeInformation(Parser parser, Type t)
        {
            TypeGenericInformation result = new TypeGenericInformation(t);
            parser.NameToTypes.Add(
                key: t.AsKey(),
                result);

            return result;
        }

        private static TypeInformation CreateTypeReferenceInformation(Parser parser, Type t)
        {
            string? reference = TryGetReferenceLink(t);

            TypeReferenceInformation typeRefInfo = new(t, reference);

            parser.NameToTypes.Add(
                key: t.AsKey(),
                typeRefInfo);

            return typeRefInfo;
        }

        /// <summary>
        /// Fetches the reference link to a type <paramref name="t"/>.
        /// Currently only supports Microsoft ("System.") and MonoGame types.
        /// </summary>
        private static string? TryGetReferenceLink(Type t)
        {
            if (t.Namespace is not string @namespace)
            {
                return null;
            }

            string linkPath = t.FullName ?? t.Name;
            if (t.IsGenericType)
            {
                linkPath = $"{t.Namespace}.{Utilities.EscapeNameForFilename(t)}";
            }

            if (@namespace.Equals("System", StringComparison.OrdinalIgnoreCase) ||
                @namespace.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
            {
                return $"https://learn.microsoft.com/en-us/dotnet/api/{linkPath}?view=net-7.0";
            }

            if (@namespace.StartsWith("Microsoft.Xna.Framework", StringComparison.OrdinalIgnoreCase) || 
                @namespace.StartsWith("MonoGame.Framework", StringComparison.OrdinalIgnoreCase))
            {
                return $"https://docs.monogame.net/api/{linkPath}.html";
            }

            return string.Empty;
        }
    }
}
