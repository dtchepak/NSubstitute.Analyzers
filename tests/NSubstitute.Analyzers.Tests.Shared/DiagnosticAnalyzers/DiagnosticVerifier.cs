﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace NSubstitute.Analyzers.Tests.Shared.DiagnosticAnalyzers
{
    /// <summary>
    /// Class for turning strings into documents and getting the diagnostics on them.
    /// All methods are static.
    /// </summary>
    public abstract class DiagnosticVerifier
    {
        private static readonly MetadataReference CorlibReference =
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        private static readonly MetadataReference SystemCoreReference =
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location);

        private static readonly MetadataReference CodeAnalysisReference =
            MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);

        private static readonly MetadataReference NSubstituteReference =
            MetadataReference.CreateFromFile(typeof(Substitute).Assembly.Location);

        private static readonly MetadataReference ValueTaskReference =
            MetadataReference.CreateFromFile(typeof(ValueTask<>).Assembly.Location);

        public static string DefaultFilePathPrefix { get; } = "Test";

        public static string CSharpDefaultFileExt { get; } = "cs";

        public static string VisualBasicDefaultExt { get; } = "vb";

        public static string TestProjectName { get; } = "TestProject";

        protected abstract string Language { get; }

        protected DiagnosticVerifier()
        {
            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture =
                CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");
        }

        public async Task VerifyDiagnostic(string source, params DiagnosticResult[] expected)
        {
            await VerifyDiagnostic(new[] { source }, Language, GetDiagnosticAnalyzer(), expected, false);
        }

        protected abstract DiagnosticAnalyzer GetDiagnosticAnalyzer();

        protected abstract CompilationOptions GetCompilationOptions();

        protected virtual IEnumerable<MetadataReference> GetAdditionalMetadataReferences()
        {
            return Enumerable.Empty<MetadataReference>();
        }

        protected async Task<Diagnostic[]> GetSortedDiagnosticsFromDocuments(DiagnosticAnalyzer analyzer, Document[] documents, bool allowCompilationErrors)
        {
            if (documents == null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            var projects = new HashSet<Project>();
            foreach (var document in documents)
            {
                projects.Add(document.Project);
            }

            var diagnostics = new List<Diagnostic>();
            var analyzerExceptions = new List<Exception>();
            foreach (var project in projects)
            {
                var options = new CompilationWithAnalyzersOptions(
                    new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty),
                    (exception, diagnosticAnalyzer, diagnostic) => analyzerExceptions.Add(exception),
                    false,
                    true);
                var compilationWithAnalyzers = (await project.GetCompilationAsync())
                    .WithAnalyzers(ImmutableArray.Create(analyzer), options);

                if (!allowCompilationErrors)
                {
                    AssertThatCompilationSucceeded(compilationWithAnalyzers);
                }

                var diags = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

                if (analyzerExceptions.Any())
                {
                    if (analyzerExceptions.Count == 1)
                    {
                        ExceptionDispatchInfo.Capture(analyzerExceptions[0]).Throw();
                    }
                    else
                    {
                        throw new AggregateException("Multiple exceptions thrown during analysis", analyzerExceptions);
                    }
                }

                foreach (var diag in diags)
                {
                    if (diag.Location == Location.None || diag.Location.IsInMetadata)
                    {
                        diagnostics.Add(diag);
                    }
                    else
                    {
                        for (int i = 0; i < documents.Length; i++)
                        {
                            var document = documents[i];
                            var tree = await document.GetSyntaxTreeAsync();
                            if (tree == diag.Location.SourceTree)
                            {
                                diagnostics.Add(diag);
                            }
                        }
                    }
                }
            }

            var results = SortDiagnostics(diagnostics);
            diagnostics.Clear();
            return results;
        }

        protected Project CreateProject(string[] sources, string language)
        {
            string fileNamePrefix = DefaultFilePathPrefix;
            string fileExt = language == LanguageNames.CSharp ? CSharpDefaultFileExt : VisualBasicDefaultExt;
            var referencedAssemblies = typeof(Substitute).Assembly.GetReferencedAssemblies();
            var systemRuntimeReference = GetAssemblyReference(referencedAssemblies, "System.Runtime");
            var systemThreadingTasksReference = GetAssemblyReference(referencedAssemblies, "System.Threading.Tasks");

            var projectId = ProjectId.CreateNewId(debugName: TestProjectName);

            using (var adhocWorkspace = new AdhocWorkspace())
            {
                var compilationOptions = GetCompilationOptions();

                var solution = adhocWorkspace
                    .CurrentSolution
                    .AddProject(projectId, TestProjectName, TestProjectName, language)
                    .WithProjectCompilationOptions(projectId, compilationOptions)
                    .AddMetadataReference(projectId, CorlibReference)
                    .AddMetadataReference(projectId, SystemCoreReference)
                    .AddMetadataReference(projectId, CodeAnalysisReference)
                    .AddMetadataReference(projectId, NSubstituteReference)
                    .AddMetadataReference(projectId, ValueTaskReference)
                    .AddMetadataReference(projectId, systemRuntimeReference)
                    .AddMetadataReference(projectId, systemThreadingTasksReference)
                    .AddMetadataReferences(projectId, GetAdditionalMetadataReferences());

                int count = 0;
                foreach (var source in sources)
                {
                    var newFileName = fileNamePrefix + count + "." + fileExt;
                    var documentId = DocumentId.CreateNewId(projectId, debugName: newFileName);
                    solution = solution.AddDocument(documentId, newFileName, SourceText.From(source));
                    count++;
                }

                return solution.GetProject(projectId);
            }
        }

        private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
        {
            int expectedCount = expectedResults.Count();
            int actualCount = actualResults.Count();

            if (expectedCount != actualCount)
            {
                string diagnosticsOutput = actualResults.Any() ? FormatDiagnostics(analyzer, actualResults.ToArray()) : "    NONE.";

                var message =
                    $@"Mismatch between number of diagnostics returned, expected ""{expectedCount}"" actual ""{actualCount}""

Diagnostics:
{diagnosticsOutput}
";
                Execute.Assertion.FailWith(message);
            }

            for (int i = 0; i < expectedResults.Length; i++)
            {
                var actual = actualResults.ElementAt(i);
                var expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        var message =
                            $@"Expected:
A project diagnostic with No location
Actual:
{FormatDiagnostics(analyzer, actual)}";
                        Execute.Assertion.FailWith(message);
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer, actual, actual.Location, expected.Locations.First());
                    var additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Length - 1)
                    {
                        var message =
                            $@"Expected {expected.Locations.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:
    {FormatDiagnostics(analyzer, actual)}
";
                        Execute.Assertion.FailWith(message);
                    }

                    for (int j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzer, actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                if (actual.Id != expected.Id)
                {
                    var message =
                        $@"Expected diagnostic id to be ""{expected.Id}"" was ""{actual.Id}""

Diagnostic:
    {FormatDiagnostics(analyzer, actual)}
";
                    Execute.Assertion.FailWith(message);
                }

                if (actual.Severity != expected.Severity)
                {
                    var message =
                            $@"Expected diagnostic severity to be ""{expected.Severity}"" was ""{actual.Severity}""

Diagnostic:
    {FormatDiagnostics(analyzer, actual)}
";
                    Execute.Assertion.FailWith(message);
                }

                if (actual.GetMessage() != expected.Message)
                {
                    var message =
                            $@"Expected diagnostic message to be ""{expected.Message}"" was ""{actual.GetMessage()}""

Diagnostic:
    {FormatDiagnostics(analyzer, actual)}
";
                    Execute.Assertion.FailWith(message);
                }
            }
        }

        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            var actualSpan = actual.GetLineSpan();

            string message;
            var actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.Line)
                {
                    message =
                            $@"Expected diagnostic to be on line ""{expected.Line}"" was actually on line ""{actualLinePosition.Line + 1}""

Diagnostic:
    {FormatDiagnostics(analyzer, diagnostic)}
";
                    Execute.Assertion.FailWith(message);
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.Column)
                {
                    message =
                            $@"Expected diagnostic to start at column ""{expected.Column}"" was actually at column ""{actualLinePosition.Character + 1}""

Diagnostic:
    {FormatDiagnostics(analyzer, diagnostic)}
";
                    Execute.Assertion.FailWith(message);
                }
            }
        }

        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i]);

                var analyzerType = analyzer.GetType();
                var rules = analyzer.SupportedDiagnostics;

                foreach (var rule in rules)
                {
                    if (rule != null && rule.Id == diagnostics[i].Id)
                    {
                        var location = diagnostics[i].Location;
                        if (location == Location.None)
                        {
                            builder.Append($"GetGlobalResult({analyzerType.Name}.{rule.Id})");
                        }
                        else
                        {
                            location.IsInSource.Should()
                                .BeTrue(
                                    $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                            string resultMethodName = diagnostics[i].Location.SourceTree.FilePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? "GetCSharpResultAt" : "GetBasicResultAt";
                            var linePosition = diagnostics[i].Location.GetLineSpan().StartLinePosition;

                            builder.Append(
                                    $"{resultMethodName}({linePosition.Line + 1}, {linePosition.Character + 1}, {analyzerType.Name}.{rule.Id})");
                        }

                        if (i != diagnostics.Length - 1)
                        {
                            builder.Append(',');
                        }

                        builder.AppendLine();
                        break;
                    }
                }
            }

            return builder.ToString();
        }

        private async Task VerifyDiagnostic(string[] sources, string language, DiagnosticAnalyzer analyzer, DiagnosticResult[] expected, bool allowCompilationErrors)
        {
            var diagnostics = await GetSortedDiagnostics(sources, language, analyzer, allowCompilationErrors);
            VerifyDiagnosticResults(diagnostics, analyzer, expected);
        }

        private Document[] GetDocuments(string[] sources, string language)
        {
            if (language != LanguageNames.CSharp && language != LanguageNames.VisualBasic)
            {
                throw new ArgumentException("Unsupported Language");
            }

            var project = CreateProject(sources, language);
            var documents = project.Documents.ToArray();

            if (sources.Length != documents.Length)
            {
                throw new InvalidOperationException("Amount of sources did not match amount of Documents created");
            }

            return documents;
        }

        private static void AssertThatCompilationSucceeded(CompilationWithAnalyzers compilationWithAnalyzers)
        {
            var compilationDiagnostics = compilationWithAnalyzers.Compilation.GetDiagnostics();

            if (compilationDiagnostics.Any(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error))
            {
                var messageBuilder = new StringBuilder();
                messageBuilder.Append("Test code compilation failed. Error(s) encountered:");
                foreach (var diagnostic in compilationDiagnostics)
                {
                    messageBuilder.AppendLine();
                    messageBuilder.AppendFormat("  {0}", diagnostic);
                }

                throw new ArgumentException(messageBuilder.ToString());
            }
        }

        private async Task<Diagnostic[]> GetSortedDiagnostics(string[] sources, string language, DiagnosticAnalyzer analyzer, bool allowCompilationErrors)
        {
            return await GetSortedDiagnosticsFromDocuments(analyzer, GetDocuments(sources, language), allowCompilationErrors);
        }

        private Diagnostic[] SortDiagnostics(IEnumerable<Diagnostic> diagnostics)
        {
            return diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();
        }

        private MetadataReference GetAssemblyReference(IEnumerable<AssemblyName> assemblies, string name)
        {
            return MetadataReference.CreateFromFile(Assembly.Load(assemblies.First(n => n.Name == name)).Location);
        }
    }
}