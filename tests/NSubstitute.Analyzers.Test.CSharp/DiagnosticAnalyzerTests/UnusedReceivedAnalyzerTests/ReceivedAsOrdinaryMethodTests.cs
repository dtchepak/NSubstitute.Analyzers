﻿using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NSubstitute.Analyzers.DiagnosticAnalyzers;
using Xunit;

namespace NSubstitute.Analyzers.Test.CSharp.DiagnosticAnalyzerTests.UnusedReceivedAnalyzerTests
{
    public class ReceivedAsOrdinaryMethodTests : UnusedReceivedAnalyzerTests
    {
        [Fact]
        public override async Task ReportDiagnostics_WhenUsedWithoutMemberCall()
        {
            var source = @"using NSubstitute;

namespace MyNamespace
{
    public interface IFoo
    {
    }

    public class FooTests
    {
        public void Test()
        {
            var substitute = NSubstitute.Substitute.For<IFoo>();
            SubstituteExtensions.Received(substitute);
        }
    }
}";
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.UnusedReceived,
                Severity = DiagnosticSeverity.Warning,
                Message = "Unused received check.",
                Locations = new[]
                {
                    new DiagnosticResultLocation(14, 13)
                }
            };

            await VerifyCSharpDiagnostic(source, expectedDiagnostic);
        }

        [Fact]
        public override async Task ReportNoDiagnostics_WhenUsedWithMethodMemberAccess()
        {
            var source = @"using NSubstitute;

namespace MyNamespace
{
    public class FooBar
    {
    }

    public interface IFoo
    {
        FooBar Bar();
    }

    public class FooTests
    {
        public void Test()
        {
            var substitute = NSubstitute.Substitute.For<IFoo>();
            SubstituteExtensions.Received(substitute).Bar();
        }
    }
}";

            await VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public override async Task ReportNoDiagnostics_WhenUsedWithPropertyMemberAccess()
        {
            var source = @"using NSubstitute;

namespace MyNamespace
{

    public interface IFoo
    {
        int Bar { get; set; }
    }

    public class FooTests
    {
        public void Test()
        {
            var substitute = NSubstitute.Substitute.For<IFoo>();
            var bar = SubstituteExtensions.Received(substitute).Bar;
        }
    }
}";

            await VerifyCSharpDiagnostic(source);
        }

        [Fact]
        public override async Task ReportNoDiagnostics_WhenUsedWithIndexerMemberAccess()
        {
            var source = @"using NSubstitute;

namespace MyNamespace
{

    public interface IFoo
    {
        int this[int x] { get; }
    }

    public class FooTests
    {
        public void Test()
        {
            var substitute = NSubstitute.Substitute.For<IFoo>();
            var bar = SubstituteExtensions.Received(substitute)[0];
        }
    }
}";

            await VerifyCSharpDiagnostic(source);
        }

        public override async Task ReportNoDiagnostics_WhenUsedWithInvokingDelegate()
        {
            var source = @"using System;
using NSubstitute;

namespace MyNamespace
{
    public class FooTests
    {
        public void Test()
        {
            var substitute = NSubstitute.Substitute.For<Func<int>>();
            SubstituteExtensions.Received(substitute)();
        }
    }
}";
            await VerifyCSharpDiagnostic(source);
        }

        public override async Task ReportsNoDiagnostics_WhenUsedWithUnfortunatelyNamedMethod()
        {
            var source = @"using System;

namespace NSubstitute
{
    public static class SubstituteExtensions
    {
        public static T Received<T>(this T substitute, decimal x) where T : class
        {
            return null;
        }
    }

    public class FooTests
    {
        public void Test()
        {
            object substitute = null;
            SubstituteExtensions.Received(substitute, 1m);
        }
    }
}";
            await VerifyCSharpDiagnostic(source);
        }
    }
}