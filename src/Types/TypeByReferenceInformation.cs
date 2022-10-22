namespace DotnetToMd.Metadata
{
    /// <summary>
    /// This references reference classes, e.g. T&.
    /// </summary>
    public class TypeByReferenceInformation : TypeInformation
    {
        private readonly TypeInformation _underlyingType;

        public override string ReferenceLink => _underlyingType.ReferenceLink;

        public override string EscapedFilename => _underlyingType.EscapedFilename;

        public override string EscapedNameForHeader
        {
            get
            {
                string suffix = Type.IsArray ? "[]" : "&";
                return $"{_underlyingType.EscapedNameForHeader}{suffix}";
            }
        }

        public TypeByReferenceInformation(TypeInformation underlyingType, Type referenceType) : 
            base(referenceType, referenceType.Namespace) 
        {
            _underlyingType = underlyingType;
        }
    }
}
