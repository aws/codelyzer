using System.Collections.Generic;
using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;


namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class PropertyBlockHandler : UstNodeHandler
    {
        private PropertyBlock Model { get => (PropertyBlock)UstNode; }

        public PropertyBlockHandler(CodeContext context,
            PropertyBlockSyntax syntaxNode)
            : base(context, syntaxNode, new PropertyBlock())
        {
            Model.Parameters = new List<Parameter>();
            if (syntaxNode.PropertyStatement.ParameterList != null)
            {
                foreach (var parameter in syntaxNode.PropertyStatement.ParameterList.Parameters)
                {
                    var param = new Parameter
                    {
                        Name = parameter.Identifier.ToString()
                    };

                    if (parameter.AsClause.Type != null)
                    {
                        param.Type = parameter.AsClause.Type.ToString();
                        param.SemanticType = SemanticHelper.GetSemanticType(parameter.AsClause.Type, SemanticModel, OriginalSemanticModel);
                    }
                    Model.Parameters.Add(param);
                }
            }
        }
    }
}
