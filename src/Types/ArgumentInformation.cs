namespace DotnetToMd.Metadata
{
    public class ArgumentInformation
    {
        public readonly string? Name;
        public readonly TypeInformation Type;

        public ArgumentModifier? Modifier;

        public string? Summary;

        public ArgumentInformation(string? name, TypeInformation type)
        {
            Name = name;
            Type = type;
        }
    }
}
