using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace DotnetToMd.Metadata
{
    internal partial class TypeMetadataInformationBuilder
    {
        private ImmutableDictionary<string, MethodInformation> FetchConstructors()
        {
            IEnumerable<ConstructorInfo> methods = _type.GetConstructors(Utilities.DefaultFlags).Where(IsMethodVisible);
            return ProcessMethods(methods);
        }

        private ImmutableDictionary<string, MethodInformation> FetchMethods()
        {
            IEnumerable<MethodInfo> methods = _type.GetMethods(Utilities.DefaultFlags).Where(IsMethodVisible);
            return ProcessMethods(methods);
        }

        private ImmutableDictionary<string, MethodInformation> ProcessMethods(IEnumerable<MethodBase> methods)
        {
            Debug.Assert(_typeResult is not null);

            List<MethodInformation> result = new();

            foreach (MethodBase method in methods)
            {
                if (method.IsSpecialName && !method.IsConstructor)
                {
                    // This is a runtime method, so just skip.
                    continue;
                }

                if (method.Name.Contains('$'))
                {
                    // Weird compiler case we just don't handle. Ignore it.
                    continue;
                }

                if (method.DeclaringType == typeof(object) || 
                    method.DeclaringType == typeof(ValueType) ||
                    method.DeclaringType == typeof(Enum) ||
                    method.DeclaringType == typeof(Attribute))
                {
                    // Skip default methods.
                    continue;
                }

                ArgumentInformation? returnInfo = null;

                Type? returnType = (method as MethodInfo)?.ReturnType;
                if (returnType is not null && returnType != typeof(void))
                {
                    TypeInformation? typeInfo = TypeInformationBuilder.FetchOrCreate(_parser, returnType);
                    if (typeInfo is null)
                    {
                        Debug.Fail("Unable to decode return type?");
                        continue;
                    }

                    returnInfo = new(default, typeInfo);
                }

                List<ArgumentInformation> parameters = new();
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    TypeInformation? typeInfo = TypeInformationBuilder.FetchOrCreate(_parser, parameter.ParameterType);
                    if (typeInfo is null)
                    {
                        Debug.Fail("Unable to decode paremeter type?");
                        continue;
                    }

                    ArgumentInformation arg = new(parameter.Name, typeInfo);

                    arg.Modifier = 
                        parameter.IsOut       ? ArgumentModifier.Out :
                        parameter.IsIn        ? ArgumentModifier.In  :
                        typeInfo.Type.IsByRef ? ArgumentModifier.Ref : 
                        default;

                    parameters.Add(arg);
                }

                List<ArgumentInformation> genericParameters = new();

                if (!method.IsConstructor)
                {
                    foreach (Type genericType in method.GetGenericArguments())
                    {
                        TypeInformation? typeInfo = TypeInformationBuilder.FetchOrCreate(_parser, genericType);
                        if (typeInfo is null)
                        {
                            Debug.Fail("Unable to decode paremeter type?");
                            continue;
                        }

                        genericParameters.Add(new(null, typeInfo));
                    }
                }
                
                MethodInformation methodInfo = new(method.Name, _typeResult, returnInfo);

                result.Add(methodInfo);

                methodInfo.Parameters = parameters.ToDictionary(p => p.Name!, p => p).ToImmutableDictionary();
                methodInfo.GenericArguments = genericParameters.ToImmutableArray();

                methodInfo.Signature = CreateMethodSignature(method, returnInfo?.Type);
            }

            var builder = ImmutableDictionary.CreateBuilder<string, MethodInformation>();
            foreach (MethodInformation m in result)
            {
                // We might expect methods with the same signature due to overriding settings.
                // In these cases, just keep track of one of them.
                builder[m.GetKey()] = m;
            }

            return builder.ToImmutable();
        }

        private static bool IsMethodVisible(MethodBase m)
        {
            return !m.IsPrivate && !m.IsAssembly;
        }

        private string? CreateMethodSignature(MethodBase m, TypeInformation? returnTypeInfo)
        {
            StringBuilder result = new();

            result.Append(GetMethodAccessorName(m));

            if (m is ConstructorInfo constructorInfo && constructorInfo.DeclaringType is not null)
            {
                TypeInformation? declaredType = 
                    TypeInformationBuilder.FetchOrCreate(_parser, constructorInfo.DeclaringType);

                result.Append($"{declaredType!.Name}(");
            }
            else
            {
                if (m.IsAbstract)
                {
                    result.Append("abstract ");
                }
                else if (m.IsVirtual)
                {
                    result.Append("virtual ");
                }

                result.Append($"{returnTypeInfo?.Name ?? "void"} ");
                result.Append($"{m.Name}(");
            }

            ParameterInfo[] parameters = m.GetParameters();
            for (int i = 0; i < parameters.Length; ++i)
            {
                TypeInformation? typeInfo = TypeInformationBuilder.FetchOrCreate(_parser, parameters[i].ParameterType);
                if (typeInfo is null)
                {
                    Debug.Fail("Unable to decode paremeter type?");
                    continue;
                }

                if (i != 0)
                {
                    result.Append(", ");
                }

                result.Append($"{typeInfo.Name} {parameters[i].Name}");
            }

            result.Append(")");

            return result.ToString();
        }
    }
}
