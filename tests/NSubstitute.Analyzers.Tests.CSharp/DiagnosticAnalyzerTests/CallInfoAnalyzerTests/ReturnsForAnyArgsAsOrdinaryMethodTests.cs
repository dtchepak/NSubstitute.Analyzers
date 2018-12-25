using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NSubstitute.Analyzers.Shared;
using NSubstitute.Analyzers.Tests.Shared.DiagnosticAnalyzers;
using Xunit;

namespace NSubstitute.Analyzers.Tests.CSharp.DiagnosticAnalyzerTests.CallInfoAnalyzerTests
{
    public class ReturnsForAnyArgsAsOrdinaryMethodTests : CallInfoDiagnosticVerifier
    {
        public override async Task ReportsNoDiagnostics_WhenSubstituteMethodCannotBeInferred(string call, string argAccess)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x);

        int Barr {{ get; }}

        int this[int x] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            var returnedValue = {call};
            SubstituteExtensions.ReturnsForAnyArgs(returnedValue, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";

            await VerifyDiagnostic(source);
        }

        public override async Task ReportsDiagnostic_WhenAccessingArgumentOutOfBounds(string call, string argAccess, int expectedLine, int expectedColumn)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x);

        int Barr {{ get; }}

        int this[int x] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.CallInfoArgumentOutOfRange,
                Severity = DiagnosticSeverity.Warning,
                Message = "There is no argument at position 1",
                Locations = new[]
                {
                    new DiagnosticResultLocation(expectedLine, expectedColumn)
                }
            };

            await VerifyDiagnostic(source, expectedDiagnostic);
        }

        public override async Task ReportsNoDiagnostic_WhenAccessingArgumentOutOfBound_AndPositionIsNotLiteralExpression(string call, string argAccess)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x, int y);

        int Barr {{ get; }}

        int this[int x, int y] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";
            await VerifyDiagnostic(source);
        }

        public override async Task ReportsNoDiagnostic_WhenAccessingArgumentWithinBounds(string call, string argAccess)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x, int y);

        int Barr {{ get; }}

        int this[int x, int y] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";
            await VerifyDiagnostic(source);
        }

        public override async Task ReportsNoDiagnostic_WhenConvertingTypeToAssignableTypeForIndirectCasts(string call, string argAccess)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x, Bar y);

        int this[int x, Bar y] {{ get; }}
    }}

    public class BarBase
    {{
    }}

    public class Bar : BarBase
    {{
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";

            await VerifyDiagnostic(source);
        }

        public override async Task ReportsDiagnostic_WhenConvertingTypeToUnsupportedType(string call, string argAccess, int expectedLine, int expectedColumn)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x, double y);

        int Foo(int x, FooBar bar);

        int this[int x, double y] {{ get; }}

        int this[int x, FooBar bar] {{ get; }}
    }}

    public class Bar
    {{
    }}

    public class FooBar : Bar
    {{
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.CallInfoCouldNotConvertParameterAtPosition,
                Severity = DiagnosticSeverity.Warning,
                Message = "Couldn't convert parameter at position 1 to type MyNamespace.Bar.",
                Locations = new[]
                {
                    new DiagnosticResultLocation(expectedLine, expectedColumn)
                }
            };

            await VerifyDiagnostic(source, expectedDiagnostic);
        }

        public override async Task ReportsNoDiagnostic_WhenConvertingTypeToSupportedType(string call, string argAccess)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(Bar x);

        int this[Bar x] {{ get; }}
    }}

    public class Bar
    {{
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";
            await VerifyDiagnostic(source);
        }

        public override async Task ReportsNoDiagnostic_WhenCastingElementsFromArgTypes(string call, string argAccess)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(Bar x);

        int this[Bar x] {{ get; }}
    }}

    public class Bar
    {{
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";
            await VerifyDiagnostic(source);
        }

        public override async Task ReportsNoDiagnostic_WhenAssigningValueToNotRefNorOutArgumentViaIndirectCall(string call, string argAccess)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
     public interface Foo
    {{
        int Bar(Bar x);

        int this[Bar x] {{ get; }}
    }}

    public class Bar
    {{
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                {argAccess}
                return 1;
            }});
        }}
    }}
}}";
            await VerifyDiagnostic(source);
        }

        public override async Task ReportsDiagnostic_WhenAccessingArgumentByTypeNotInInvocation(string call)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x);

        int Barr {{ get; }}

        int this[int x] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                callInfo.Arg<double>();
                return 1;
            }});
        }}
    }}
}}";
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.CallInfoCouldNotFindArgumentToThisCall,
                Severity = DiagnosticSeverity.Warning,
                Message = "Can not find an argument of type double to this call.",
                Locations = new[]
                {
                    new DiagnosticResultLocation(22, 17)
                }
            };

            await VerifyDiagnostic(source, expectedDiagnostic);
        }

        public override async Task ReportsNoDiagnostic_WhenAccessingArgumentByTypeInInInvocation(string call)
        {
            var source = $@"using System;
using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x);

        int this[int x] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                callInfo.Arg<int>();
                return 1;
            }});
        }}
    }}
}}";

            await VerifyDiagnostic(source);
        }

        public override async Task ReportsDiagnostic_WhenAccessingArgumentByTypeMultipleTimesInInvocation(string call)
        {
            var source = $@"using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x, int y);

        int this[int x, int y] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                callInfo.Arg<int>();
                return 1;
            }});
        }}
    }}
}}";
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.CallInfoMoreThanOneArgumentOfType,
                Severity = DiagnosticSeverity.Warning,
                Message = "There is more than one argument of type int to this call.",
                Locations = new[]
                {
                    new DiagnosticResultLocation(19, 17)
                }
            };

            await VerifyDiagnostic(source, expectedDiagnostic);
        }

        public override async Task ReportsNoDiagnostic_WhenAccessingArgumentByTypeMultipleDifferentTypesInInvocation(string call)
        {
            var source = $@"using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x, double y);

        int this[int x, double y] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                callInfo.Arg<int>();
                return 1;
            }});
        }}
    }}
}}";

            await VerifyDiagnostic(source);
        }

        public override async Task ReportsDiagnostic_WhenAssigningValueToNotOutNorRefArgument(string call)
        {
            var source = $@"using NSubstitute;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(int x, double y);

        int this[int x, double y] {{ get; }}
    }}

    public class FooTests
    {{
        public void Test()
        {{
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs({call}, callInfo =>
            {{
                callInfo[1] = 1;
                return 1;
            }});
        }}
    }}
}}";
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.CallInfoArgumentIsNotOutOrRef,
                Severity = DiagnosticSeverity.Warning,
                Message = "Could not set argument 1 (double) as it is not an out or ref argument.",
                Locations = new[]
                {
                    new DiagnosticResultLocation(19, 17)
                }
            };

            await VerifyDiagnostic(source, expectedDiagnostic);
        }

        public override async Task ReportsNoDiagnostic_WhenAssigningValueToRefArgument()
        {
            var source = @"using NSubstitute;

namespace MyNamespace
{
    public interface Foo
    {
        int Bar(ref int x);
    }

    public class FooTests
    {
        public void Test()
        {
            int value = 0;
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs(substitute.Bar(ref value), callInfo =>
            {
                callInfo[0] = 1;
                return 1;
            });
        }
    }
}";
            await VerifyDiagnostic(source);
        }

        public override async Task ReportsNoDiagnostic_WhenAssigningValueToOutArgument()
        {
            var source = @"using NSubstitute;

namespace MyNamespace
{
    public interface Foo
    {
        int Bar(out int x);
    }

    public class FooTests
    {
        public void Test()
        {
            int value = 0;
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs(substitute.Bar(out value), callInfo =>
            {
                callInfo[0] = 1;
                return 1;
            });
        }
    }
}";
            await VerifyDiagnostic(source);
        }

        public override async Task ReportsDiagnostic_WhenAssigningValueToOutOfBoundsArgument()
        {
            var source = @"using NSubstitute;

namespace MyNamespace
{
    public interface Foo
    {
        int Bar(out int x);
    }

    public class FooTests
    {
        public void Test()
        {
            int value = 0;
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs(substitute.Bar(out value), callInfo =>
            {
                callInfo[1] = 1;
                return 1;
            });
        }
    }
}";
            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.CallInfoArgumentOutOfRange,
                Severity = DiagnosticSeverity.Warning,
                Message = "There is no argument at position 1",
                Locations = new[]
                {
                    new DiagnosticResultLocation(18, 17)
                }
            };

            await VerifyDiagnostic(source, expectedDiagnostic);
        }

        public override async Task ReportsDiagnostic_WhenAssigningType_NotAssignableTo_Argument(string left, string right, string expectedMessage)
        {
            var source = $@"using NSubstitute;
using System.Collections.Generic;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(out {left} x);
    }}

    public class FooTests
    {{
        public void Test()
        {{
            {left} value = default({left});
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs(substitute.Bar(out value), callInfo =>
            {{
                callInfo[0] = {right};
                return 1;
            }});
        }}
    }}
}}";

            var expectedDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticIdentifiers.CallInfoArgumentSetWithIncompatibleValue,
                Severity = DiagnosticSeverity.Warning,
                Message = expectedMessage,
                Locations = new[]
                {
                    new DiagnosticResultLocation(19, 17)
                }
            };

            await VerifyDiagnostic(source, expectedDiagnostic);
        }

        public override async Task ReportsNoDiagnostic_WhenAssigningType_AssignableTo_Argument(string left, string right)
        {
            var source = $@"using NSubstitute;
using System.Collections.Generic;

namespace MyNamespace
{{
    public interface Foo
    {{
        int Bar(out {left} x);
    }}

    public class FooTests
    {{
        public void Test()
        {{
            {left} value = default({left});
            var substitute = NSubstitute.Substitute.For<Foo>();
            SubstituteExtensions.ReturnsForAnyArgs(substitute.Bar(out value), callInfo =>
            {{
                callInfo[0] = {right};
                return 1;
            }});
        }}
    }}
}}";
            await VerifyDiagnostic(source);
        }
    }
}