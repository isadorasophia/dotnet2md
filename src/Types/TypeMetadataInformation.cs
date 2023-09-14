using System.Collections.Immutable;
using System.Text;

namespace DotnetToMd.Metadata
{
    /// <summary>
    /// This is a metadata type currently decoded and targeted to be an actual documentation type.
    /// See <see cref="Parser._target"/>.
    /// </summary>
    public class TypeMetadataInformation : TypeInformation
    {
        public readonly MemberKind Kind;

        public string? Signature;

        public TypeInformation? InheritedType;
        public ImmutableArray<TypeInformation>? InheritedInterfaces;

        public ImmutableDictionary<string, MethodInformation>? Constructors;
        public ImmutableDictionary<string, PropertyInformation>? Properties;
        public ImmutableDictionary<string, MethodInformation>? Methods;
        public ImmutableDictionary<string, PropertyInformation>? Events;

        /// <summary>
        /// Fetched once we query the documentation file.
        /// </summary>
        public string? Summary;

        public override string ReferenceLink => Namespace is not null ?
            $"{Namespace.Replace('.', '/')}/{EscapedFilename}.html" : string.Empty;

        internal TypeMetadataInformation(
            Type metadata,
            MemberKind kind) :
            base(metadata, metadata.Namespace)
        {
            Kind = kind;
        }

        public ImmutableArray<TypeInformation> GetInheritedMembers()
        {
            var builder = ImmutableArray.CreateBuilder<TypeInformation>();

            if (InheritedType is not null)
            {
                builder.Add(InheritedType);
            }

            if (InheritedInterfaces?.Length > 0)
            {
                builder.AddRange(InheritedInterfaces);
            }

            return builder.ToImmutableArray();
        }
    }
}
