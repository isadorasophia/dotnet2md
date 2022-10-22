namespace DotnetToMd.Metadata
{
    /// <summary>
    /// This is not really a type, but rather a generic abstraction.
    /// </summary>
    public class TypeGenericInformation : TypeInformation
    {
        internal TypeGenericInformation(Type metadata) :
            base(metadata, string.Empty)
        { }

        public override string ReferenceLink => string.Empty;
    }
}
