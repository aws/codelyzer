using Newtonsoft.Json;
using System;

namespace Codelyzer.Analysis.Model
{
    public class MethodDeclaration : BaseMethodDeclaration
    {
        [JsonProperty("return-type", Order = 12)]
        public string ReturnType { get; set; }
        
        [JsonProperty("semantic-return-type", Order = 13)]
        public string SemanticReturnType { get; set; }

        public MethodDeclaration()
            : base(IdConstants.MethodIdName)
        {
        }
        public MethodDeclaration(string idName)
            : base(idName)
        {
        }
        public override bool Equals(object obj)
        {
            if (obj is MethodDeclaration)
            {
                return Equals(obj as MethodDeclaration);
            }
            return false;
        }

        public bool Equals(MethodDeclaration compareNode)
        {
            return
                compareNode != null &&
                ReturnType?.Equals(compareNode.ReturnType) != false &&
                SemanticReturnType?.Equals(compareNode.SemanticReturnType) != false &&
                base.Equals(compareNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(ReturnType, SemanticReturnType, base.GetHashCode());
        }
    }
}
