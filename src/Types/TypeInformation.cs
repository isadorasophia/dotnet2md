using System.Collections.Immutable;
using System.Text;

namespace DotnetToMd.Metadata
{
    public abstract class TypeInformation
    {
        public readonly string Name;

        public readonly string Assembly;
        public readonly string? Namespace;

        public readonly Type Type;

        public ImmutableArray<TypeInformation>? GenericArguments;

        /// <summary>
        /// This is the reference link used to point to the documentation.
        /// </summary>
        public abstract string ReferenceLink { get; }

        public virtual string EscapedFilename => Utilities.EscapeNameForFilename(Type);

        public virtual string EscapedNameForHeader => Name.Replace("<", "\\<").Replace(">", "\\>").Replace("[", "\\[").Replace("]", "\\]");

        public TypeInformation(Type type, string? @namespace)
        {
            Name = Utilities.PrettifyName(type);
            Assembly = type.Assembly.ManifestModule.Name;
            Namespace = @namespace;

            Type = type;
        }

        /// <summary>
        /// Get the key used to identify this type through xml.
        /// </summary>
        public string GetKey()
        {
            if (Type.IsGenericType)
            {
                StringBuilder builder = new();

                builder.Append(Utilities.CleanNameOfGeneric(Type, useFullName: true));
                builder.Append('{');

                ImmutableArray<TypeInformation> arguments = GenericArguments!.Value;
                for (int a = 0; a < arguments.Length; a++)
                {
                    builder.Append(arguments[a].GetKey());

                    if (a != arguments.Length - 1)
                    {
                        builder.Append(",");
                    }
                }

                builder.Append('}');

                return builder.ToString();
            }

            return Type.FullName ?? $"{Namespace}.{Type.Name}";
        }
    }
}
