namespace Microsoft.Tools.Productivity.ShortStack
{

    /// <summary>
    /// Convenient text to specify special stack levels
    /// </summary>
    public enum StackLevelAlias
    {
        /// <summary>
        /// Most recently created stack level
        /// </summary>
        Top = 1000,

        /// <summary>
        /// Earliest stack level
        /// </summary>
        Bottom = 1,

        /// <summary>
        /// The 'zero' level
        /// </summary>
        Root = 0
    }
}
