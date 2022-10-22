namespace DotnetToMd.Metadata
{
    public class PropertyInformation : IComparable<PropertyInformation>
    {
        public readonly MemberKind Kind;

        public readonly string Name;
        public readonly TypeInformation DeclaringType;
        public readonly ArgumentInformation Return;

        public string? Summary;
        public string? Signature;

        public PropertyInformation(MemberKind kind, string name, TypeInformation declaringType, ArgumentInformation @return)
        {
            Kind = kind;

            Name = name;
            DeclaringType = declaringType;

            Return = @return;
        }

        public int CompareTo(PropertyInformation? propInfo) => Name.CompareTo(propInfo?.Name);
    }
}
