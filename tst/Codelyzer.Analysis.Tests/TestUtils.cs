using System;
using System.Reflection;
using NUnit.Framework;

namespace Codelyzer.Analysis.Tests
{
    public class TestUtils
    {
        public static MethodInfo GetPrivateMethod(Type type, string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                Assert.Fail("methodName cannot be null or whitespace");

            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (method == null)
                Assert.Fail("{0} method not found", methodName);

            return method;
        }

        public static MethodInfo GetPrivateStaticMethod(Type type, string methodName)
        {
            if (string.IsNullOrWhiteSpace(methodName))
                Assert.Fail("methodName cannot be null or whitespace");

            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
                Assert.Fail("{0} method not found", methodName);

            return method;
        }

        public static PropertyInfo GetPrivateProperty(Type type, string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                Assert.Fail("propertyName cannot be null or whitespace");

            PropertyInfo property = type.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);

            if (property == null)
                Assert.Fail("{0} property not found", propertyName);

            return property;
        }
    }
}