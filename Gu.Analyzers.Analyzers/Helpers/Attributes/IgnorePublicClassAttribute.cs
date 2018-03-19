namespace Gu.Analyzers.Helpers.Attributes
{
    using System;

    /// <summary>
    /// This attribute will be used within the current project to avoid build errors -- GU0072
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    internal class IgnorePublicClassAttribute : Attribute
    {
    }
}
