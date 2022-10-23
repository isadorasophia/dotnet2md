using DotnetToMd.Metadata;
using System.Collections.Immutable;
using System.Text;

namespace DotnetToMd
{
    internal partial class Parser
    {
        private readonly Dictionary<string /* path */, string /* name */> _markdowns = new(StringComparer.OrdinalIgnoreCase);

        private void GenerateMarkdown()
        {
            IEnumerable<TypeMetadataInformation> types = 
                NameToTypes.Values.Where(t => t is TypeMetadataInformation).Select(t => (TypeMetadataInformation)t);

            foreach (TypeMetadataInformation t in types)
            {
                if (t.Namespace is null)
                {
                    continue;
                }

                string result = GenerateMarkdownForType(t);

                string namespacePath = Path.Join(_outputPath, CreatePathForNamespace(t.Namespace));
                if (!Directory.Exists(namespacePath))
                {
                    _ = Directory.CreateDirectory(namespacePath);
                }
                
                string fullPath = Path.Join(namespacePath, $"{t.EscapedFilename}.md");
                File.WriteAllText(fullPath, result);

                _markdowns.Add(fullPath, t.EscapedNameForHeader);
            }

            HashSet<string> targetNamespaces = Targets.Select(a => Path.GetFileNameWithoutExtension(a.ManifestModule.Name)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> summaryResultPerTarget = new();

            foreach (string directory in Directory.GetDirectories(_outputPath))
            {
                string targetName = new DirectoryInfo(directory).Name;
                if (targetNamespaces.Contains(targetName))
                {
                    StringBuilder builder = new();
                    WriteSummary(/* ref */ ref builder, directory, 1);

                    summaryResultPerTarget.Add(targetName, builder.ToString());
                }
            }

            string presummaryFilePath = Path.Join(_outputPath, "pre_SUMMARY.md");
            string summaryFilePath = Path.Join(_outputPath, "summary.md");

            if (File.Exists(presummaryFilePath))
            {
                string summaryFileContent = File.ReadAllText(presummaryFilePath);
                foreach (string target in summaryResultPerTarget.Keys)
                {
                    summaryFileContent = summaryFileContent.Replace($"<{target}-Content>", $"{summaryResultPerTarget[target]}", StringComparison.InvariantCultureIgnoreCase);
                }

                File.WriteAllText(summaryFilePath, summaryFileContent);
            }
        }

        private void WriteSummary(ref StringBuilder text, string directory, int level)
        {
            StringBuilder indent = new();

            // Skip first directory!
            if (level != 1)
            {
                for (int i = 2; i < level; ++i)
                {
                    indent.Append("  ");
                }

                text.Append(indent);
                text.AppendLine($"- [{new DirectoryInfo(directory).Name}]()");
                indent.Append("  ");
            }

            foreach (string subdirectory in Directory.GetDirectories(directory))
            {
                WriteSummary(ref text, subdirectory, level + 1);
            }

            foreach (string file in Directory.GetFiles(directory))
            {
                string relativePath = Path.GetRelativePath(_outputPath, file);
                text.AppendLine($"{indent}- [{_markdowns[file]}]({relativePath})");
            }
        }

        private string CreatePathForNamespace(string @namespace)
        {
            return @namespace.Replace('.', Path.DirectorySeparatorChar);
        }

        private string GenerateMarkdownForType(TypeMetadataInformation t)
        {
            StringBuilder builder = new();

            builder.Append($"# {t.EscapedNameForHeader}\n\n");

            builder.Append($"**Namespace:** {t.Namespace} \\\n");
            builder.Append($"**Assembly:** {t.Assembly}\n\n");

            builder.Append($"```csharp\n{t.Signature}\n```\n\n");
            
            if (t.Summary is not null)
            {
                builder.Append($"{t.Summary}\n\n");
            }

            ImmutableArray<TypeInformation> inheritedTypes = t.GetInheritedMembers();
            if (inheritedTypes.Length > 0)
            {
                builder.Append("**Implements:** _");

                for (int i = 0; i < inheritedTypes.Length; ++i)
                {
                    TypeInformation tt = inheritedTypes[i];
                    builder.Append($"[{tt.EscapedNameForHeader}]({tt.ReferenceLink})");

                    if (i != inheritedTypes.Length - 1)
                    {
                        builder.Append(", ");
                    }
                }

                builder.Append("_\n\n");
            }

            if (t.Constructors?.Count > 0)
            {
                builder.Append("### ⭐ Constructors\n");

                foreach (MethodInformation c in t.Constructors.Values)
                {
                    builder.Append(MethodToMarkdown(c));
                }
            }

            if (t.Properties?.Count > 0)
            {
                builder.Append("### ⭐ Properties\n");

                List<PropertyInformation> sortedProperties = t.Properties.Values.OrderBy(p => p.Name).ToList();
                sortedProperties.Sort();

                foreach (PropertyInformation p in sortedProperties)
                {
                    builder.Append(PropertyToMarkdown(p));
                }
            }

            if (t.Events?.Count > 0)
            {
                builder.Append("### ⭐ Events\n");

                List<PropertyInformation> sortedEvents = t.Events.Values.OrderBy(e => e.Name).ToList();
                sortedEvents.Sort();

                foreach (PropertyInformation p in sortedEvents)
                {
                    builder.Append(PropertyToMarkdown(p));
                }
            }

            if (t.Methods?.Count > 0)
            {
                builder.Append("### ⭐ Methods\n");

                List<MethodInformation> sortedMethods = t.Methods.Values.OrderBy(m => m.Name).ToList();
                sortedMethods.Sort();

                foreach (MethodInformation m in sortedMethods)
                {
                    builder.Append($"#### {m.GetPrettyKey()}\n");
                    builder.Append(MethodToMarkdown(m));
                }
            }

            builder.Append("\n\n⚡");

            return builder.ToString();
        }

        private StringBuilder PropertyToMarkdown(PropertyInformation p)
        {
            StringBuilder builder = new();

            builder.Append($"#### {p.Name}\n");

            builder.Append($"```csharp\n{p.Signature}\n```\n\n");

            if (p.Summary is not null)
            {
                builder.Append($"{p.Summary}\n\n");
            }

            builder.Append("**Returns** \\\n");
            builder.Append(ArgumentToMarkdown(p.Return));

            return builder;
        }

        private StringBuilder MethodToMarkdown(MethodInformation m)
        {
            StringBuilder builder = new();

            builder.Append($"```csharp\n{m.Signature}\n```\n\n");

            if (m.Summary is not null)
            {
                builder.Append($"{m.Summary}\n\n");
            }

            if (m.Parameters?.Count > 0)
            {
                builder.Append("**Parameters** \\\n");

                foreach (ArgumentInformation argument in m.Parameters.Values)
                {
                    builder.Append(ArgumentToMarkdown(argument));
                }

                builder.Append("\n");
            }

            if (m.Return is ArgumentInformation @return)
            {
                builder.Append("**Returns** \\\n");
                builder.Append(ArgumentToMarkdown(@return));

                builder.Append("\n");
            }

            if (m.Exceptions?.Length > 0)
            {
                builder.Append("**Exceptions** \\\n");

                foreach ((TypeInformation tException, string summary) in m.Exceptions)
                {
                    builder.Append($"[{tException.EscapedNameForHeader}]({tException.ReferenceLink}) \\\n");
                    builder.Append($"{summary}\\\n");
                }
            }

            return builder;
        }

        private StringBuilder ArgumentToMarkdown(ArgumentInformation arg)
        {
            StringBuilder builder = new();

            if (!string.IsNullOrEmpty(arg.Name))
            {
                builder.Append($"`{arg.Name}` ");
            }

            builder.Append($"[{arg.Type.EscapedNameForHeader}]({arg.Type.ReferenceLink}) \\\n");

            if (arg.Summary is not null)
            {
                builder.Append($"{arg.Summary}\\\n");
            }

            return builder;
        }
    }
}
