using System.Collections.Immutable;
using System.Text;

namespace DotnetToMd.Metadata
{
    public class MethodInformation : IComparable<MethodInformation>
    {
        public readonly string Name;
        public readonly TypeInformation DeclaringType;
        public readonly ArgumentInformation? Return;

        public ImmutableArray<ArgumentInformation>? GenericArguments;

        /// <summary>
        /// This is not a dictionary since it's important to keep the order for this.
        /// </summary>
        public ImmutableArray<ArgumentInformation>? Parameters;

        public ImmutableArray<(TypeInformation Type, string Summary)>? Exceptions;

        private string? _key;

        public string GetKey()
        {
            if (_key is null)
            {
                StringBuilder result = new();

                if (Name == ".ctor")
                {
                    // The special xml formatting uses #.
                    result.Append("#ctor");
                }
                else
                {
                    result.Append(Name);
                }

                if (GenericArguments?.Length > 0)
                {
                    result.Append($"``{GenericArguments?.Length}");
                }

                if (Parameters?.Length > 0)
                {
                    result.Append("(");

                    int remainingParameters = Parameters.Value.Length;
                    foreach (ArgumentInformation arg in Parameters.Value)
                    {
                        remainingParameters--;
                        result.Append(arg.Type.GetKey());

                        if (arg.Modifier == ArgumentModifier.Out)
                        {
                            result.Append('@');
                        }

                        if (remainingParameters != 0)
                        {
                            result.Append(',');
                        }
                    }

                    result.Append(")");
                }

                _key = result.ToString();
            }

            return _key;
        }

        public string GetPrettyKey()
        {
            StringBuilder result = new();

            result.Append(Name);
            result.Append("(");

            if (Parameters is not null)
            {
                int remainingParameters = Parameters.Value.Length;
                foreach (ArgumentInformation arg in Parameters)
                {
                    remainingParameters--;

                    if (arg.Modifier == ArgumentModifier.Out)
                    {
                        result.Append("out ");
                    }

                    result.Append(arg.Type.Name);

                    if (remainingParameters != 0)
                    {
                        result.Append(", ");
                    }
                }
            }

            result.Append(")");

            return result.ToString();
        }

        public string? Summary;
        public string? Signature;

        public MethodInformation(string name, TypeInformation declaringType, ArgumentInformation? @return)
        {
            Name = name;
            DeclaringType = declaringType;
            Return = @return;
        }

        public int CompareTo(MethodInformation? other) => Name.CompareTo(other?.Name);
    }
}
