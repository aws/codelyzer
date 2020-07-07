namespace AwsCodeAnalyzer.Model
{
    public static class IdConstants
    {
        public const int SolutionId = 88;
        public const string SolutionIdName = "solution";
        
        public const int ProjectId = 99;
        public const string ProjectIdName = "project";
        
        public const int RootId = 100;
        public const string RootIdName = "source-file-root";

        public const int UsingDirectiveId = 102;
        public const string UsingDirectiveIdName = "using-dir-or-import";
        
        public const int NamespaceId = 103;
        public const string NamespaceIdName = "namespace-or-package";
        
        public const int ClassId = 104;
        public const string ClassIdName = "class";
        
        public const int MethodId = 105;
        public const string MethodIdName = "method";
        
        public const int BodyId = 106;
        public const string BodyIdName = "body";
        
        public const int LiteralId = 107;
        public const string LiteralIdName = "literal";
        
        public const int InvocationId = 108;
        public const string InvocationIdName = "invocation";
    }
}