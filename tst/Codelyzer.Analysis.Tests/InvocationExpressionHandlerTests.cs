using System.Collections.Immutable;
using Codelyzer.Analysis.VisualBasic.Handlers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Moq;

namespace Codelyzer.Analysis.Tests
{
    public class InvocationExpressionHandlerTests
    {
        [Test]
        public void TypeSymbolWithTypeParameters_Returns_TypeNameWithTypeParametersInCSharpSyntax()
        {
            var typeParameter1 = new Mock<ITypeParameterSymbol>();
            typeParameter1.Setup(p => p.Name).Returns("tpName1");
            var typeParameter2 = new Mock<ITypeParameterSymbol>();
            typeParameter2.Setup(p => p.Name).Returns("tpName2");
            var typeParameters = ImmutableArray.Create(typeParameter1.Object, typeParameter2.Object);

            var typeSymbolWithTypeParameters = new Mock<INamedTypeSymbol>();
            typeSymbolWithTypeParameters.Setup(s => s.ToString()).Returns("typeSymbolStringForm(xyz)");
            typeSymbolWithTypeParameters.Setup(s => s.IsGenericType).Returns(true);
            typeSymbolWithTypeParameters.Setup(s => s.TypeParameters).Returns(typeParameters);

            var getClassNameWithNamespace = TestUtils.GetPrivateStaticMethod(typeof(InvocationExpressionHandler), "GetClassNameWithNamespace");
            var typeNameWithTypeParametersInCSharpSyntax = getClassNameWithNamespace.Invoke(null, new object[] { typeSymbolWithTypeParameters.Object });

            Assert.AreEqual("typeSymbolStringForm<tpName1, tpName2>", typeNameWithTypeParametersInCSharpSyntax);
        }

        [Test]
        public void TypeSymbolWithoutTypeParameters_Returns_TypeNameInStringForm()
        {
            var typeSymbolWithoutTypeParameters = new Mock<INamedTypeSymbol>();
            typeSymbolWithoutTypeParameters.Setup(s => s.ToString()).Returns("typeSymbolStringForm");

            var getClassNameWithNamespace = TestUtils.GetPrivateStaticMethod(typeof(InvocationExpressionHandler), "GetClassNameWithNamespace");
            var typeNameInStringForm = getClassNameWithNamespace.Invoke(null, new object[] { typeSymbolWithoutTypeParameters.Object });

            Assert.AreEqual("typeSymbolStringForm", typeNameInStringForm);
        }

        [Test]
        public void NonGenericTypeSymbol_Returns_TypeNameInStringForm()
        {
            var typeParameter1 = new Mock<ITypeParameterSymbol>();
            typeParameter1.Setup(p => p.Name).Returns("tpName1");
            var typeParameter2 = new Mock<ITypeParameterSymbol>();
            typeParameter2.Setup(p => p.Name).Returns("tpName2");
            var typeParameters = ImmutableArray.Create(typeParameter1.Object, typeParameter2.Object);

            var typeSymbolWithTypeParameters = new Mock<INamedTypeSymbol>();
            typeSymbolWithTypeParameters.Setup(s => s.ToString()).Returns("typeSymbolStringForm(xyz)");
            typeSymbolWithTypeParameters.Setup(s => s.IsGenericType).Returns(false);
            typeSymbolWithTypeParameters.Setup(s => s.TypeParameters).Returns(typeParameters);

            var getClassNameWithNamespace = TestUtils.GetPrivateStaticMethod(typeof(InvocationExpressionHandler), "GetClassNameWithNamespace");
            var typeNameWithTypeParametersInCSharpSyntax = getClassNameWithNamespace.Invoke(null, new object[] { typeSymbolWithTypeParameters.Object });

            Assert.AreEqual("typeSymbolStringForm(xyz)", typeNameWithTypeParametersInCSharpSyntax);
        }

    }
}
