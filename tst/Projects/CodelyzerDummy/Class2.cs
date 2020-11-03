namespace CodelyzerDummy
{
    public class Class2
    {
        private readonly int _someInt;
        private readonly string _someStr;

        public Class2(int someInt, string someStr)
        {
            _someInt = someInt;
            _someStr = someStr;
        }

        public void TestMethod()
        {
            FirstMethod().ChainedMethod();
        }

        private Class2 FirstMethod()
        {
            return new Class2(0, "");
        }

        private Class2 ChainedMethod()
        {
            return new Class2(0, "");
        }

        public class NestedClass
        {
        }
    }
}