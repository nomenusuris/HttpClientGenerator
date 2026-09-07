using System;

namespace HttpClient.Utils
{
    /// <summary>
    /// Defines which interfaces expose enpoints and their route prefix.
    /// For generic interfaces route prefix is deduced from <see cref="ServiceSignature"/>
    /// </summary>
    /// <remarks>
    /// For generic interfaces the attribute should be defined for each 
    /// generic arguments set used
    /// </remarks>
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class ExposeEndpointsAttribute : Attribute
    {
        public string Template { get; }
        public Type ServiceSignature { get; }

        public ExposeEndpointsAttribute(string template, Type serviceSignrature = null)
        {
            Template = template;
            ServiceSignature = serviceSignrature;
        }

    }
}
