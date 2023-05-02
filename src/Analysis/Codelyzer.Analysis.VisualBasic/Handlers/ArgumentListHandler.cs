using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class ArgumentListHandler : UstNodeHandler
    {
        private ArgumentList Model { get => (ArgumentList)UstNode; }

        public ArgumentListHandler(CodeContext context,
            ArgumentListSyntax syntaxNode)
            : base(context, syntaxNode, new ArgumentList())
        {
            Model.Identifier = syntaxNode.ToString();
            SetMetaData(syntaxNode);
        }

        private void SetMetaData(ArgumentListSyntax syntaxNode)
        {
            if (syntaxNode != null)
            {
                foreach (var argumentSyntax in syntaxNode.Arguments)
                {
                    var identifier = "";
                    var semanticType = "";

                    if (argumentSyntax.GetType() != typeof(OmittedArgumentSyntax))
                    {
                        identifier = argumentSyntax.GetExpression().ToString();
                        semanticType = SemanticHelper.GetSemanticType(argumentSyntax.GetExpression(), SemanticModel);
                    }

                    var parameter = new Parameter()
                    {
                        Name = identifier,
                        SemanticType = semanticType
                    };
#pragma warning disable CS0618 // Type or member is obsolete
                    Model.Parameters.Add(parameter);
#pragma warning restore CS0618 // Type or member is obsolete

                    var argument = new Argument
                    {
                        Identifier = identifier,
                        SemanticType = semanticType
                    };
                    Model.Arguments.Add(argument);
                }
            }

            IMethodSymbol invokedSymbol = (IMethodSymbol)(SemanticHelper.GetSemanticSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel)
                ?? SemanticHelper.GetDeclaredOriginalSymbol(syntaxNode, SemanticModel, OriginalSemanticModel));
            ;

            if (invokedSymbol == null) return;
            
            //Set method properties
            SemanticHelper.AddMethodProperties(invokedSymbol, Model.SemanticProperties);
        }
    }
}
