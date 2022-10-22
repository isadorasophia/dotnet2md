using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;

namespace DotnetToMd.Metadata
{
    internal partial class TypeMetadataInformationBuilder
    {
        private readonly Parser _parser;
        private readonly Type _type;
        private readonly MemberKind _kind;

        private readonly List<TypeInformation> _inheritedInterfaces = new();
        private TypeInformation? _inheritedClass;

        private TypeMetadataInformation? _typeResult;

        internal TypeMetadataInformationBuilder(Parser parser, Type t, MemberKind kind)
        {
            _parser = parser;
            _type = t;
            _kind = kind;
        }

        internal TypeMetadataInformation CreateMetadataInfo()
        {
            _typeResult = new TypeMetadataInformation(_type, _kind);

            _parser.NameToTypes.Add(_type.AsKey(), _typeResult);

            FetchReferences();

            _typeResult.Signature = CreateTypeSignature();

            _typeResult.InheritedType = _inheritedClass;
            _typeResult.InheritedInterfaces = _inheritedInterfaces?.ToImmutableArray();

            _typeResult.Properties = FetchProperties().AddRange(FetchFields());
            _typeResult.Events = FetchEvents();

            _typeResult.Constructors = FetchConstructors();
            _typeResult.Methods = FetchMethods();

            return _typeResult;
        }

        private void FetchReferences()
        {
            if (_type.BaseType is Type baseType 
                && baseType != typeof(object) 
                && baseType != typeof(ValueType))
            {
                _inheritedClass = TypeInformationBuilder.FetchOrCreate(_parser, baseType);
            }

            foreach (Type @interface in _type.GetInterfaces())
            {
                TypeInformation? @interfaceInfo = TypeInformationBuilder.FetchOrCreate(_parser, @interface);
                if (@interfaceInfo is not null)
                {
                    _inheritedInterfaces.Add(@interfaceInfo);
                }
            }
        }

        /// <summary>
        /// Creates the signature of a type (class, struct, enum) as a string from metadata.
        /// </summary>
        private string CreateTypeSignature()
        {
            StringBuilder result = new();

            if (_type.IsPublic)
            {
                result.Append("public ");
            }
            
            if (_type.IsAbstract && _type.IsSealed)
            {
                result.Append("static ");
            }
            else if (_type.IsAbstract)
            {
                result.Append("abstract ");
            }
            else if (_type.IsSealed)
            {
                result.Append("sealed ");
            }

            result.Append(_kind switch
            {
                MemberKind.Enum => "enum ",
                MemberKind.Struct => "struct ",
                MemberKind.Class => "class ",
                _ => string.Empty
            });

            string name = _type.IsGenericType ? Utilities.FormatGenericName(_type) : _type.Name;
            result.Append($"{name} ");

            List<TypeInformation> implementations = new();

            // Always start with the base class
            if (_inheritedClass is not null)
            {
                implementations.Add(_inheritedClass);
            }

            implementations.AddRange(_inheritedInterfaces);

            for (int i = 0; i < implementations.Count; ++i)
            {
                if (i == 0)
                {
                    result.Append(": ");
                }

                result.Append(implementations[i].Name);

                if (i != implementations.Count - 1)
                {
                    result.Append(", ");
                }
            }

            return result.ToString().Trim();
        }
    }
}
