namespace Codelyzer.Analysis.Model
{
    public class ConstructorDeclaration : MethodDeclaration
    {
        public ConstructorDeclaration()
            : base(IdConstants.ConstructorIdName)
        {
            Parameters = new UstList<Parameter>();
            SemanticProperties = new UstList<string>();
        }
    }
}