using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class MethodStatementHandler : UstNodeHandler
    {
        private MethodStatement Model { get => (MethodStatement)UstNode; }

        public MethodStatementHandler(CodeContext context,
            MethodStatementSyntax syntaxNode)
            : base(context, syntaxNode, new MethodStatement())
        {
            Model.Identifier = syntaxNode.ToString();

            if (syntaxNode.ParameterList != null)
            {
                foreach (var parameter in syntaxNode.ParameterList.Parameters)
                {
                    var param = new Parameter();
                    if (parameter.Identifier != null)
                        param.Name = parameter.Identifier.Identifier.Text;

                    if (parameter.AsClause?.Type != null)
                        param.Type = parameter.AsClause.Type.ToString();

                    if (parameter.AsClause != null)
                    {
                        param.SemanticType =
                            SemanticHelper.GetSemanticType(parameter.AsClause.Type, SemanticModel,
                                OriginalSemanticModel);
                    }
                    Model.Parameters.Add(param);
                }
            }
        }
    }
}
