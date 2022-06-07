using Codelyzer.Analysis.Common;
using Codelyzer.Analysis.Model;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using System.Linq;


namespace Codelyzer.Analysis.VisualBasic.Handlers
{
    public class PropertyBlockHandler : UstNodeHandler
    {
        private PropertyBlock Model { get => (PropertyBlock)UstNode; }

        public PropertyBlockHandler(CodeContext context,
            PropertyBlockSyntax syntaxNode)
            : base(context, syntaxNode, new PropertyBlock())
        {

            foreach (var parameter in syntaxNode.PropertyStatement.ParameterList?.Parameters)
            {
                var param = new Parameter
                {
                    Name = parameter.Identifier.ToString()
                };

                if (parameter.AsClause.Type != null)
                    param.Type = parameter.AsClause.Type.ToString();

                param.SemanticType = SemanticHelper.GetSemanticType(parameter.AsClause.Type, SemanticModel, OriginalSemanticModel);
                Model.Parameters.Add(param);
            }

            var classSymbol = SemanticHelper.GetDeclaredSymbol(syntaxNode, SemanticModel, OriginalSemanticModel);

            Model.Identifier = syntaxNode.Kind().ToString();
            Model.Modifiers = syntaxNode.PropertyStatement.Modifiers.ToString();

            if (classSymbol != null)
            {
                if (classSymbol.BaseType != null)
                {
                    Model.BaseType = classSymbol.BaseType.ToString();
                    Model.BaseTypeOriginalDefinition = GetBaseTypOriginalDefinition(classSymbol);
                    Model.Reference.Namespace = GetNamespace(classSymbol);
                    Model.Reference.Assembly = GetAssembly(classSymbol);
                    Model.Reference.Version = GetAssemblyVersion(classSymbol);
                    Model.Reference.AssemblySymbol = classSymbol.ContainingAssembly;
                }

                if (classSymbol.Interfaces != null)
                {
                    Model.BaseList = classSymbol.Interfaces.Select(x => x.ToString())?.ToList();
                }
            }
        }
    }
}
