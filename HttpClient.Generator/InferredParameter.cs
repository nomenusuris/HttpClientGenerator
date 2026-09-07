using Microsoft.CodeAnalysis;

namespace HttpClient.Generator
{
    internal class InferredParameter
    {
        public string Name { get; }
        public ITypeSymbol Type { get; }
        public string AttributeName { get; set; }

        public string ParentMember { get; }

        public InferredParameter(string Name, ITypeSymbol Type, string attributeName, string parentMember = null)
        {
            this.Name = Name;
            this.Type = Type;
            AttributeName = attributeName;
            ParentMember = parentMember;
        }

        public string Path => !string.IsNullOrEmpty(ParentMember) ? $"{ParentMember}.{Name}" : Name;

        public bool IsQuery()
        {
            return AttributeName == Constants.FROM_QUERY_ATTRIBUTE;
        }

        public bool IsForm()
        {
            return AttributeName == Constants.FROM_FORM_ATTRIBUTE;
        }

        public bool IsBody()
        {
            return AttributeName == Constants.FROM_BODY_ATTRIBUTE;
        }

        public bool IsContent()
        {
            return IsBody() || IsForm();
        }
    }
}
