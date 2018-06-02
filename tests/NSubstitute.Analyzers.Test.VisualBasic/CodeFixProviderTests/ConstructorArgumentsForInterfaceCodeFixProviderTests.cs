﻿using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using NSubstitute.Analyzers.CodeFixProviders;
using NSubstitute.Analyzers.DiagnosticAnalyzers;
using Xunit;

namespace NSubstitute.Analyzers.Test.VisualBasic.CodeFixProviderTests
{
    public class ConstructorArgumentsForInterfaceCodeFixProviderTests : CodeFixProviderTest
    {
        [Fact]
        public async Task RemovesInvocationArguments_WhenGenericFor_UsedWithParametersForInterface()
        {
            var source = @"Imports NSubstitute

Namespace MyNamespace
    Public Interface IFoo
    End Interface

    Public Class FooTests
        Public Sub Test()
            Dim substitute = NSubstitute.Substitute.[For](Of IFoo)(1, 2, 3)
        End Sub
    End Class
End Namespace
";
            var newSource = @"Imports NSubstitute

Namespace MyNamespace
    Public Interface IFoo
    End Interface

    Public Class FooTests
        Public Sub Test()
            Dim substitute = NSubstitute.Substitute.[For](Of IFoo)()
        End Sub
    End Class
End Namespace
";

            await VerifyVisualBasicFix(source, newSource);
        }

        [Fact]
        public async Task RemovesInvocationArguments_WhenNonGenericFor_UsedWithParametersForInterface()
        {
            var source = @"Imports NSubstitute

Namespace MyNamespace
    Public Interface IFoo
    End Interface

    Public Class FooTests
        Public Sub Test()
            Dim substitute = NSubstitute.Substitute.[For]({GetType(IFoo)}, New Object() {1})
        End Sub
    End Class
End Namespace
";
            var newSource = @"Imports NSubstitute

Namespace MyNamespace
    Public Interface IFoo
    End Interface

    Public Class FooTests
        Public Sub Test()
            Dim substitute = NSubstitute.Substitute.[For]({GetType(IFoo)}, Nothing)
        End Sub
    End Class
End Namespace
";
            await VerifyVisualBasicFix(source, newSource);
        }

        protected override CodeFixProvider GetVisualBasicCodeFixProvider()
        {
            return new ConstructorArgumentsForInterfaceCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetVisualBasicDiagnosticAnalyzer()
        {
            return new SubstituteAnalyzer();
        }
    }
}