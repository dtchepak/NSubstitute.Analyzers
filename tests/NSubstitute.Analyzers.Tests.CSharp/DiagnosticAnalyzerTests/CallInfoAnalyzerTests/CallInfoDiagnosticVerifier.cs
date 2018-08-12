using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using NSubstitute.Analyzers.CSharp.DiagnosticAnalyzers;
using NSubstitute.Analyzers.Tests.Shared.DiagnosticAnalyzers;

namespace NSubstitute.Analyzers.Tests.CSharp.DiagnosticAnalyzerTests.CallInfoAnalyzerTests
{
    public abstract class CallInfoDiagnosticVerifier : CSharpDiagnosticVerifier, ICallInfoDiagnosticVerifier
    {
        public abstract Task ReportsDiagnostic_WhenAccessingArgumentOutOfBounds(string argAccess, int expectedLine, int expectedColumn);

        public abstract Task ReportsNoDiagnostic_WhenAccessingArgumentWithinBounds(string argAccess);

        public abstract Task ReportsDiagnostic_WhenConvertingTypeToUnsupportedType(string argAccess, int expectedLine, int expectedColumn);

        public abstract Task ReportsNoDiagnostic_WhenConvertingTypeToSupportedType(string argAccess);

        public abstract Task ReportsNoDiagnostic_WhenCastingElementsFromArgTypes(string argAccess);

        public abstract Task ReportsNoDiagnostic_WhenAssigningValueToNotRefNorOutArgumentViaIndirectCall(string argAccess);

        public abstract Task ReportsDiagnostic_WhenAccessingArgumentByTypeNotInInvocation();

        public abstract Task ReportsNoDiagnostic_WhenAccessingArgumentByTypeInInInvocation();

        public abstract Task ReportsDiagnostic_WhenAccessingArgumentByTypeMultipleTimesInInvocation();

        public abstract Task ReportsNoDiagnostic_WhenAccessingArgumentByTypeMultipleDifferentTypesInInvocation();

        public abstract Task ReportsDiagnostic_WhenAssigningValueToNotOutNorRefArgument();

        public abstract Task ReportsNoDiagnostic_WhenAssigningValueToRefArgument();

        public abstract Task ReportsNoDiagnostic_WhenAssigningValueToOutArgument();

        public abstract Task ReportsDiagnostic_WhenAssigningValueToOutOfBoundsArgument();

        public abstract Task ReportsDiagnostic_WhenAssigningWrongTypeToArgument();

        public abstract Task ReportsNoDiagnostic_WhenAssigningProperTypeToArgument();

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
        {
            return new CallInfoAnalyzer();
        }
    }
}