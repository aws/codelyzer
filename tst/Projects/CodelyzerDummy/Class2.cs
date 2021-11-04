using System;
using System.Diagnostics;

namespace CodelyzerDummy
{
    public class Class2
    {
        private readonly int _someInt;
        private readonly string _someStr;
        private readonly Grade _grade;

        public Class2(int someInt, string someStr)
        {
            _someInt = someInt;
            _someStr = someStr;
            _grade = Grade.A;
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

        [Conditional("DEBUG")]
        //TODO: Let's see if it works
        public static void Message(string msg)
        {
            Console.WriteLine(msg);
        }

        public class NestedClass
        {
        }

        public enum Grade
        {
            A,
            B,
            C
        }
    }
}