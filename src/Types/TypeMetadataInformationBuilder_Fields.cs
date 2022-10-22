using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace DotnetToMd.Metadata
{
    internal partial class TypeMetadataInformationBuilder
    {
        private ImmutableDictionary<string, PropertyInformation> FetchFields()
        {
            Debug.Assert(_typeResult is not null);

            List<PropertyInformation> result = new();

            IEnumerable<FieldInfo> fields = _type.GetFields(Utilities.DefaultFlags).Where(IsFieldVisible);

            foreach (FieldInfo field in fields)
            {
                if (field.Attributes.HasFlag(FieldAttributes.RTSpecialName))
                {
                    // This is a runtime field, so just skip.
                    continue;
                }

                TypeInformation? typeInfo = TypeInformationBuilder.FetchOrCreate(_parser, field.FieldType);
                if (typeInfo is null)
                {
                    Debug.Fail("Unable to decode field type?");
                    continue;
                }
                
                ArgumentInformation returnInfo = new(default, typeInfo);
                PropertyInformation fieldInfo = new(MemberKind.Field, field.Name, _typeResult, returnInfo);

                result.Add(fieldInfo);

                fieldInfo.Signature = CreateFieldSignature(field, typeInfo);
            }

            return result.ToDictionary(p => p.Name, p => p).ToImmutableDictionary();
        }

        private static bool IsFieldVisible(FieldInfo f)
        {
            return !f.IsPrivate && !f.IsAssembly;
        }

        private static string? CreateFieldSignature(FieldInfo f, TypeInformation returnTypeInfo)
        {
            StringBuilder result = new();

            result.Append(GetFieldAccessorName(f));

            if (f.IsInitOnly)
            {
                result.Append("readonly ");
            }

            if (f.IsStatic)
            {
                result.Append("static ");
            }

            if (f.IsLiteral && !f.IsInitOnly)
            {
                result.Append("const ");
            }

            result.Append($"{returnTypeInfo.Name} {f.Name};");

            return result.ToString();
        }

        private static string GetFieldAccessorName(FieldInfo f)
        {
            if (f.IsPublic)
            {
                return "public ";
            }
            else if (f.IsFamily)
            {
                return "protected ";
            }
            else if (f.IsPrivate)
            {
                return "private ";
            }
            else if (f.IsAssembly)
            {
                return "internal ";
            }

            return string.Empty;
        }
    }
}
