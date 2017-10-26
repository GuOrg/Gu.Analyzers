namespace Gu.Analyzers.Helpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class TestMethod
    {
        internal static bool IsTestMethod(this MethodDeclarationSyntax methodDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var attributeList in methodDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    if (attribute.Name is IdentifierNameSyntax identifier &&
                        identifier.Identifier.ValueText == "Test")
                    {
                        var type = semanticModel.GetTypeInfoSafe(attribute, cancellationToken).Type;
                        if (type == KnownSymbol.NUnitTestAttribute ||
                            type == KnownSymbol.NUnitTestCaseAttribute)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
