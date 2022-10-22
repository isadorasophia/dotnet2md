using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace DotnetToMd.Metadata
{
    internal partial class TypeMetadataInformationBuilder
    {
        private ImmutableDictionary<string, PropertyInformation> FetchProperties()
        {
            Debug.Assert(_typeResult is not null);

            List<PropertyInformation> result = new();

            IEnumerable<PropertyInfo> properties = _type.GetProperties(Utilities.DefaultFlags).Where(IsPropertyVisible);

            foreach (PropertyInfo property in properties)
            {
                TypeInformation? propertyType = TypeInformationBuilder.FetchOrCreate(_parser, property.PropertyType);
                if (propertyType is null)
                {
                    Debug.Fail("Unable to decode property type?");
                    continue;
                }

                ArgumentInformation returnInfo = new(default, propertyType);
                PropertyInformation propertyInfo = new(MemberKind.Property, property.Name, _typeResult, returnInfo);

                result.Add(propertyInfo);

                propertyInfo.Signature = CreatePropertySignature(property, propertyType);
            }

            return result.ToDictionary(p => p.Name, p => p).ToImmutableDictionary();
        }

        private static bool IsPropertyVisible(PropertyInfo p)
        {
            if (p.GetMethod is MethodInfo getMethod)
            {
                return IsMethodVisible(getMethod);
            }

            return false;
        }

        private static string? CreatePropertySignature(PropertyInfo p, TypeInformation returnTypeInfo)
        {
            StringBuilder result = new();

            MethodInfo? getMethod = p.GetMethod;
            MethodInfo? setMethod = p.SetMethod;

            if (getMethod is null)
            {
                Debug.Fail("Property without a getter?");
                return null;
            }

            result.Append(GetMethodAccessorName(getMethod));

            if (getMethod.IsAbstract)
            {
                result.Append("abstract ");
            }

            if (getMethod.IsStatic)
            {
                result.Append("static ");
            }

            result.Append($"{returnTypeInfo.Name} {p.Name} {{ get; ");

            if (setMethod is null)
            {
                result.Append("}");
            }
            else
            {
                result.Append(GetMethodAccessorName(setMethod));
                result.Append("set; }");
            }

            return result.ToString();
        }
    }
}
