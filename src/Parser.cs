﻿using DotnetToMd.Metadata;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;

namespace DotnetToMd
{
    internal partial class Parser
    {
        public ImmutableHashSet<Assembly> Targets;

        public Dictionary<string, TypeInformation> NameToTypes { get; } = new();

        private readonly ImmutableArray<Assembly> _dependencies;

        private readonly ImmutableArray<string> _xmlFiles;
        private readonly string _outputPath;

        internal Parser(List<Assembly> target, List<Assembly> dependencies, string[] xmlFiles, string outputPath)
        {
            Targets = target.ToImmutableHashSet();

            _dependencies = dependencies.ToImmutableArray();

            _xmlFiles = xmlFiles.ToImmutableArray();
            _outputPath = outputPath;
        }

        internal void Generate()
        {
            ReadMetadata();
            ReadXml();

            GenerateMarkdown();
        }

        private void ReadMetadata()
        {
            foreach (Assembly asm in Targets)
            {
                IEnumerable<Type> publicTypes = asm.GetTypes().Where(t => t.IsPublic);
                foreach (Type t in publicTypes)
                {
                    if (TypeInformationBuilder.FetchOrCreate(this, t) is null)
                    {
                        Debug.Fail($"Unable to decode metadata type {t.Name}?");
                    }
                }
            }
        }

        private void ReadXml()
        {
            foreach (string file in _xmlFiles)
            {
                XDocument xml = XDocument.Load(file);

                if (xml.Root?.Descendants("members")?.Elements() is not IEnumerable<XElement> members)
                {
                    // No members declared?
                    return;
                }

                foreach (XElement element in members)
                {
                    ProcessMember(element);
                }
            }
        }

        private void ProcessMember(XElement element)
        {
            string? memberName = element.Attribute("name")?.Value;
            string? summary = element.Element("summary")?.ToString();

            if (string.IsNullOrEmpty(memberName))
            {
                Debug.Fail("Skipping empty member?");
                return;
            }

            string name = memberName.Substring(memberName.LastIndexOf(':') + 1);

            char firstCharacter = memberName[0];
            switch (firstCharacter)
            {
                case 'T':
                    ProcessTypeMember(element, name, summary);
                    return;

                case 'P':
                case 'F':
                    ProcessFieldOrPropertyMember(element, name, summary);
                    return;

                case 'M':
                    ProcessMethodMember(element, name, summary);
                    return;

                case 'E':
                    ProcessEventMember(element, name, summary);
                    return;

                default:
                    Debug.Fail("Unsupported scenario?");
                    return;
            }
        }

        private void ProcessTypeMember(XElement _, string name, string? summary)
        {
            if (!NameToTypes.TryGetValue(name, out TypeInformation? typeInfo))
            {
                return;
            }

            if (typeInfo is TypeMetadataInformation metadataInfo)
            {
                metadataInfo.Summary = FormatSummary(summary, RetrieveRelativePathFromNamespace(metadataInfo.Namespace));
            }
        }

        private void ProcessFieldOrPropertyMember(XElement _, string name, string? summary)
        {
            string declaringType = Utilities.GetDeclaringTypeName(name);
            if (!NameToTypes.TryGetValue(declaringType, out TypeInformation? typeInfo) || 
                typeInfo is not TypeMetadataInformation metadataInfo)
            {
                // Internal types won't be in the list.
                return;
            }

            string propertyName = GetMemberName(declaringType, name);
            if (metadataInfo.Properties?.TryGetValue(propertyName, out PropertyInformation? propertyInfo) ?? false)
            {
                propertyInfo.Summary = FormatSummary(summary, RetrieveRelativePathFromNamespace(propertyInfo.DeclaringType.Namespace));
            }
        }

        private void ProcessMethodMember(XElement element, string name, string? summary)
        {
            string declaringType = Utilities.GetDeclaringTypeOfMethod(name);
            if (!NameToTypes.TryGetValue(declaringType, out TypeInformation? typeInfo) ||
                typeInfo is not TypeMetadataInformation metadataInfo)
            {
                // Internal types won't be in the list.
                return;
            }

            string methodName = GetMemberName(declaringType, name);
            if ((metadataInfo.Methods?.TryGetValue(methodName, out MethodInformation? methodInfo) ?? false) ||
                (metadataInfo.Constructors?.TryGetValue(methodName, out methodInfo) ?? false))
            {
                string relativePathFromNamespace = RetrieveRelativePathFromNamespace(methodInfo.DeclaringType.Namespace);

                methodInfo.Summary = FormatSummary(summary, relativePathFromNamespace);

                List<XElement>? parameters = element.Elements("param")?.ToList();
                if (methodInfo.Parameters is not null && parameters?.Count > 0)
                {
                    foreach (XElement parameter in parameters)
                    {
                        string? parameterName = parameter.Attribute("name")?.Value.Trim();
                        string parameterSummary = FormatSummary(parameter.Value.Trim(), relativePathFromNamespace) ?? string.Empty;

                        if (parameterName is not null && 
                            methodInfo.Parameters.Value.FirstOrDefault(p => p.Name == parameterName) is ArgumentInformation argument)
                        {
                            argument.Summary = FormatSummary(parameterSummary, relativePathFromNamespace);
                        }
                    }
                }

                XElement? @return = element.Element("returns");
                if (@return is not null && methodInfo.Return is ArgumentInformation returnInfo)
                {
                    string returnSummary = @return.Value.Trim();
                    returnInfo.Summary = FormatSummary(returnSummary, relativePathFromNamespace);
                }

                List<XElement>? exceptions = element.Elements("exception")?.ToList();
                if (exceptions?.Count > 0)
                {
                    var builder = ImmutableArray.CreateBuilder<(TypeInformation Type, string Summary)>();
                    foreach (XElement e in exceptions)
                    {
                        string? parameterRefName = e.Attribute("cref")?.Value.Trim();
                        parameterRefName = parameterRefName?.Substring(parameterRefName.LastIndexOf(':') + 1);

                        string exceptionSummary = FormatSummary(e.Value.Trim(), relativePathFromNamespace) ?? string.Empty;

                        if (parameterRefName is not null && 
                            FetchOrCreate(parameterRefName) is TypeInformation typeInformation)
                        {
                            builder.Add((typeInformation, exceptionSummary));
                        }
                    }

                    methodInfo.Exceptions = builder.ToImmutableArray();
                }
            }
        }

        private void ProcessEventMember(XElement _, string name, string? summary)
        {
            string declaringType = Utilities.GetDeclaringTypeName(name);
            if (!NameToTypes.TryGetValue(declaringType, out TypeInformation? typeInfo) ||
                typeInfo is not TypeMetadataInformation metadataInfo)
            {
                // Internal types won't be in the list.
                return;
            }

            string eventName = GetMemberName(declaringType, name);
            if (metadataInfo.Events?.TryGetValue(eventName, out PropertyInformation? eventInfo) ?? false)
            {
                eventInfo.Summary = FormatSummary(summary, RetrieveRelativePathFromNamespace(eventInfo.DeclaringType.Namespace));
            }
        }

        private string GetMemberName(string declaringTypeName, string name)
        {
            return name.Substring(declaringTypeName.Length + 1);
        }

        public TypeInformation? FetchOrCreate(string typeName)
        {
            if (NameToTypes.TryGetValue(typeName, out TypeInformation? typeInfo))
            {
                return typeInfo;
            }

            Type? t = FindType(typeName);
            if (t is not null)
            {
                return TypeInformationBuilder.CreateTypeInformationFromType(this, t);
            }

            return null;
        }

        private Type? FindType(string name)
        {
            Type? t = typeof(string).Assembly.GetType(name);
            if (t is not null)
            {
                return t;
            }

            foreach (Assembly a in _dependencies)
            {
                t = a.GetType(name);
                if (t is not null)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
