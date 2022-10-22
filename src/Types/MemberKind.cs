namespace DotnetToMd.Metadata
{
    /// <summary>
    /// The type of the member described in the documentation.
    /// </summary>
    public enum MemberKind
    {
        /// <summary>
        /// Fields.
        /// </summary>
        Field = 'F',

        /// <summary>
        /// Properties.
        /// </summary>
        Property = 'P',

        /// <summary>
        /// Events.
        /// </summary>
        Event = 'E',

        /// <summary>
        /// Methods.
        /// </summary>
        Method = 'M',

        /// <summary>
        /// Classes.
        /// </summary>
        Class,

        /// <summary>
        /// Interfaces.
        /// </summary>
        Interface,

        /// <summary>
        /// Structs.
        /// </summary>
        Struct,

        /// <summary>
        /// Enum.
        /// </summary>
        Enum,

        /// <summary>
        /// Constructors.
        /// </summary>
        Constructor,
    }
}
