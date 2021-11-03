using System;
using System.Collections.Generic;
using System.Text;

namespace CodelyzerDummy
{
    class TestClass : ITest
    {
        public string SayHello(string hi)
        {
            return "Saying hello to " + hi;
        }
    }
}
