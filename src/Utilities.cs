using DotnetToMd.Metadata;
using System.Reflection;
using System.Text;

namespace DotnetToMd
{
    internal static class Utilities
    {
        /// <summary>
        /// Default flags for querying all members.
        /// </summary>
        public static BindingFlags DefaultFlags = BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.NonPublic;

        public static string AsKey(this Type t)
        {
            return t.IsGenericParameter || t.FullName is null ? t.Name : t.FullName;
        }

        public static string CleanNameOfGeneric(Type genericType, bool useFullName = false)
        {
            string name = useFullName && genericType.FullName is not null ? 
                genericType.FullName : genericType.Name;

            return name.Substring(0, name.IndexOf("`", StringComparison.InvariantCulture));
        }

        public static string GetDeclaringTypeName(string name) =>
            name.Substring(0, name.LastIndexOf('.'));

        public static string GetDeclaringTypeOfMethod(string name) =>
            GetDeclaringTypeName(StripParameters(name));

        public static string StripParameters(string methodName)
        {
            int index = methodName.LastIndexOf('(');
            if (index == -1)
            {
                return methodName;
            }

            return methodName.Substring(0, index);
        }

        public static string GetArgumentCountFromName(Type genericType)
        {
            string name = genericType.Name;
            return name.Substring(name.IndexOf("`", StringComparison.InvariantCulture) + 1);
        }

        public static string EscapeNameForFilename(TypeInformation t) => EscapeNameForFilename(t.Type);

        public static string EscapeNameForFilename(Type t)
        {
            string name;
            if (t.IsGenericType)
            {
                string genericName = CleanNameOfGeneric(t);
                string arguments = GetArgumentCountFromName(t);

                name = $"{genericName}-{arguments}";
            }
            else
            {
                name = t.Name;
            }

            return name;
        }

        public static string FormatGenericName(Type genericType)
        {
            Type[] arguments = genericType.GenericTypeArguments;
            if (arguments.Length == 0)
            {
                arguments = genericType.GetTypeInfo().GenericTypeParameters;
            }

            if (genericType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return $"{PrettifyName(arguments[0])}?";
            }

            StringBuilder builder = new();

            builder.AppendFormat("{0}<", CleanNameOfGeneric(genericType));
            for (int a = 0; a < arguments.Length; a++)
            {
                builder.Append(PrettifyName(arguments[a]));

                if (a != arguments.Length - 1)
                {
                    builder.Append(", ");
                }
            }

            builder.Append(">");

            return builder.ToString();
        }

        /// <summary>
        /// This is the set of types from the C# keyword list.
        /// </summary>
        static readonly private Dictionary<Type, string> _primitiveAlias = new()
            {
                { typeof(bool), "bool" },
                { typeof(byte), "byte" },
                { typeof(char), "char" },
                { typeof(decimal), "decimal" },
                { typeof(double), "double" },
                { typeof(float), "float" },
                { typeof(int), "int" },
                { typeof(long), "long" },
                { typeof(object), "object" },
                { typeof(sbyte), "sbyte" },
                { typeof(short), "short" },
                { typeof(string), "string" },
                { typeof(uint), "uint" },
                { typeof(ulong), "ulong" },
                { typeof(void), "void" }
            };

        public static string PrettifyName(Type type)
        {
            if (type.IsGenericType)
            {
                return FormatGenericName(type);
            }

            if ((type.IsPrimitive || type == typeof(string)) && 
                _primitiveAlias.TryGetValue(type, out string? value))
            {
                return value;
            }

            return type.Name;
        }
    }
}
