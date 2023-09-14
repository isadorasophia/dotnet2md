using DotnetToMd.Metadata;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DotnetToMd
{
    internal partial class Parser
    {
        /// <summary>
        /// This will format a summary with cref parameters with their markdown syntax.
        /// </summary>
        private string? FormatSummary(string? text, string prefix)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            text = GetSummary(text);
            if (text is null)
            {
                return null;
            }

            Regex re = new("(<see cref=\")(.*)(\"[ ]?/>)");
            MatchCollection matchCollection = re.Matches(text);

            foreach (Match match in matchCollection)
            {
                if (!match.Success)
                {
                    continue;
                }

                string replaceString = match.Value;
                string memberName = match.Groups[2].Value;

                text = text.Replace(replaceString, ToReferenceLink(memberName, prefix));
            }

            return text;
        }

        /// <summary>
        /// Return raw string value between <summary>. We do not use XNode here
        /// because it will escape the see cref parameters.
        /// </summary>
        private string? GetSummary(string text)
        {
            Regex re = new(@"^(<summary>)((.|\r|\n)*)(?=<\/summary>)");
            Match m = re.Match(text);

            return m.Groups[2].Value.Trim();
        }

        /// <param name="fullName">Full name of the target type.</param>
        /// <param name="prefix">Prefix of the current namespace (for appending to a relative path).</param>
        private string ToReferenceLink(string fullName, string prefix)
        {
            string name = fullName.Substring(fullName.LastIndexOf(':') + 1);
            char firstCharacter = fullName[0];

            string? declaringTypeName;
            TypeInformation? type;

            string referenceLink = string.Empty;
            switch (firstCharacter)
            {
                case 'T':
                    type = FetchOrCreate(name);
                    if (type is null)
                    {
                        break;
                    }

                    name = type.Name;
                    referenceLink = FormatReferenceLink(prefix, type.ReferenceLink);
                    break;

                case 'P':
                case 'F':
                case 'E':
                    declaringTypeName = Utilities.GetDeclaringTypeName(name);
                    type = FetchOrCreate(declaringTypeName);
                    if (type is null)
                    {
                        break;
                    }

                    string propertyName = GetMemberName(declaringTypeName, name);

                    name = $"{type.Name}.{propertyName}";
                    referenceLink = GetPropertyReferenceLink(type, propertyName, prefix);
                    break;

                case 'M':
                    declaringTypeName = Utilities.GetDeclaringTypeOfMethod(name);

                    type = FetchOrCreate(declaringTypeName);
                    if (type is null)
                    {
                        break;
                    }

                    string methodName = GetMemberName(declaringTypeName, name);

                    name = $"{type.Name}.{methodName}";
                    referenceLink = GetMethodReferenceLink(type, methodName, prefix);
                    break;

                case '!':
                    // So far, I have only seen that happen for generic parameters. Not really supported as of now.
                    break;

                case 'A':
                case 'N':
                    // TODO: Assembly reference?
                    break;

                default:
                    Debug.Fail("Unsupported scenario?");
                    break;
            }

            return $"[{name}]({referenceLink})";
        }

        private string GetPropertyReferenceLink(TypeInformation type, string member, string prefix)
        {
            string referenceLink = type.ReferenceLink;

            // TODO: Support external websites!!
            if (type.ReferenceLink.Contains("https"))
            {
                return referenceLink;
            }

            // TODO: Figure out conflicting links.
            return $"{prefix}{referenceLink}#{member}";
        }

        private string GetMethodReferenceLink(TypeInformation type, string method, string prefix)
        {
            string referenceLink = type.ReferenceLink;

            // TODO: Support external types.
            if (type is not TypeMetadataInformation metadataType || 
                !(metadataType.Methods?.TryGetValue(method, out MethodInformation? methodInfo) ?? false))
            {
                return referenceLink;
            }

            method = methodInfo.GetPrettyKey();

            int firstSpaceIndex = method.IndexOf(' ');
            if (firstSpaceIndex != -1)
            {
                method = method.Substring(0, firstSpaceIndex);
            }

            method = method.Trim('(', ')');

            // For now, the header will be method name and the first parameter.
            // TODO: Figure out conflicting links.
            return $"{prefix}{referenceLink}#{method}";
        }
    }
}
