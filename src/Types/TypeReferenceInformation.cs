namespace DotnetToMd.Metadata
{
    public class TypeReferenceInformation : TypeInformation
    {
        /// <summary>
        /// Link to the reference docs of this type.
        /// </summary>
        private readonly string? _referenceDocs;

        public override string ReferenceLink => _referenceDocs ?? string.Empty;

        public TypeReferenceInformation(Type type, string? reference) : 
            base(type, type.Namespace) 
        {
            _referenceDocs = reference;
        }
    }
}
