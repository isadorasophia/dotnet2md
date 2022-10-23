using System.Collections.Immutable;
using System.Diagnostics;

namespace DotnetToMd.Metadata
{
    internal static partial class TypeInformationBuilder
    {
        public static TypeInformation? FetchOrCreate(Parser parser, Type t)
        {
            if (parser.NameToTypes.TryGetValue(t.AsKey(), out TypeInformation? typeInfo))
            {
                return typeInfo;
            }

            return CreateTypeInformationFromType(parser, t);
        }

        /// <summary>
        /// Create all type information that is relevant to <paramref name="t"/>.
        /// This will aggregate all information in <paramref name="parser"/>.
        /// </summary>
        /// <returns>
        /// Whether it was able to successfully decode <paramref name="t"/>.
        /// </returns>
        public static TypeInformation? CreateTypeInformationFromType(Parser parser, Type t)
        {
            if (parser.NameToTypes.ContainsKey(t.AsKey()))
            {
                return null;
            }

            TypeInformation typeInformation;
            if (t.IsByRef || t.IsArray)
            {
                if (FetchOrCreate(parser, t.GetElementType()!) is not TypeInformation underlyingType)
                {
                    return null;
                }

                typeInformation = new TypeByReferenceInformation(underlyingType, t);
            }
            else if (t.IsGenericType && !t.IsGenericTypeDefinition)
            {
                typeInformation = FetchOrCreate(parser, t.GetGenericTypeDefinition())!;
            }
            else if (t.IsGenericParameter)
            {
                typeInformation = CreateGenericParameterTypeInformation(parser, t);
            }
            else if (!parser.Targets.Contains(t.Assembly))
            {
                // Not a metadata type.
                typeInformation = CreateTypeReferenceInformation(parser, t);
            }
            else if (t.IsArray)
            {
                // skip for now...
                typeInformation = FetchOrCreate(parser, typeof(Array))!;
            }
            else
            {
                MemberKind? kind = FindTypeKind(t);
                if (kind is null)
                {
                    Debug.Fail($"Unable to identify kind of ${t.Name}");
                    return null;
                }

                TypeMetadataInformationBuilder builder = new(parser, t, kind.Value);
                typeInformation = builder.CreateMetadataInfo();
            }

            typeInformation.GenericArguments = FetchGenericArguments(parser, t);
            return typeInformation;
        }

        private static MemberKind? FindTypeKind(Type t)
        {
            if (t.IsEnum)
            {
                return MemberKind.Enum;
            }

            if (t.IsValueType)
            {
                return MemberKind.Struct;
            }

            if (t.IsClass)
            {
                return MemberKind.Class;
            }

            if (t.IsInterface)
            {
                return MemberKind.Interface;
            }

            return null;
        }

        private static ImmutableArray<TypeInformation>? FetchGenericArguments(Parser parser, Type t)
        {
            if (t.IsGenericType)
            {
                List<TypeInformation> argumentsInfo = new();
                foreach (Type tt in t.GetGenericArguments())
                {
                    TypeInformation? genericArgumentInfo = FetchOrCreate(parser, tt);
                    if (genericArgumentInfo is not null)
                    {
                        argumentsInfo.Add(genericArgumentInfo);
                    }
                }

                return argumentsInfo.ToImmutableArray();
            }

            return default;
        }
    }
}
