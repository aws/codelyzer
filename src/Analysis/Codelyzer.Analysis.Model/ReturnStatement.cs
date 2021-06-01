using Newtonsoft.Json;
using System;

namespace Codelyzer.Analysis.Model
{
    public class ReturnStatement : UstNode
    {
        [JsonProperty("semantic-return-type", Order = 10)]
        public string SemanticReturnType { get; set; }

        public ReturnStatement()
            : base(IdConstants.ReturnStatementIdName)
        {
        }

        public ReturnStatement(string idName)
            : base(idName)
        {
        }
        public override bool Equals(object obj)
        {
            if (obj is ReturnStatement)
            {
                return Equals(obj as ReturnStatement);
            }
            return false;
        }

        public bool Equals(ReturnStatement compareNode)
        {
            return
                compareNode != null &&
                SemanticReturnType?.Equals(compareNode.SemanticReturnType) != false &&
                base.Equals(compareNode as UstNode);

        }
        public override int GetHashCode()
        {
            return HashCode.Combine(SemanticReturnType, base.GetHashCode());
        }
    }
}
