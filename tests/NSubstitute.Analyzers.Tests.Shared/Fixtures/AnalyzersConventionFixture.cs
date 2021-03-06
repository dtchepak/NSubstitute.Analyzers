﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NSubstitute.Analyzers.Tests.Shared.Fixtures
{
    public class AnalyzersConventionFixture
    {
        public void AssertDiagnosticAnalyzerAttributeUsageFormAssemblyContaining<T>(string expectedLanguage)
        {
            AssertDiagnosticAnalyzerAttributeUsageFormAssemblyContaining(typeof(T), expectedLanguage);
        }

        public void AssertExportCodeFixProviderAttributeUsageFromAssemblyContaining<T>(string expectedLanguage)
        {
            AssertExportCodeFixProviderAttributeUsageFromAssemblyContaining(typeof(T), expectedLanguage);
        }

        public void AssertExportCodeFixProviderAttributeUsageFromAssemblyContaining(Type type, string expectedLanguage)
        {
            var types = GetTypesAssignableTo<CodeFixProvider>(type.Assembly).ToList();

            types.Should().OnlyContain(innerType => innerType.GetCustomAttributes<ExportCodeFixProviderAttribute>(true).Count() == 1, "because each code fix provider should be marked with only one attribute ExportCodeFixProviderAttribute");
            types.SelectMany(innerType => innerType.GetCustomAttributes<ExportCodeFixProviderAttribute>(true)).Should()
                .OnlyContain(
                    attr => attr.Languages.Length == 1 && attr.Languages.Count(lang => lang == expectedLanguage) == 1,
                    $"because each code fix provider should support only selected language ${expectedLanguage}");
        }

        public void AssertDiagnosticAnalyzerAttributeUsageFormAssemblyContaining(Type type, string expectedLanguage)
        {
            var types = GetTypesAssignableTo<DiagnosticAnalyzer>(type.Assembly).ToList();

            types.Should().OnlyContain(innerType => innerType.GetCustomAttributes<DiagnosticAnalyzerAttribute>(true).Count() == 1, "because each analyzer should be marked with only one attribute DiagnosticAnalyzerAttribute");
            types.SelectMany(innerType => innerType.GetCustomAttributes<DiagnosticAnalyzerAttribute>(true)).Should()
                .OnlyContain(
                    attr => attr.Languages.Length == 1 && attr.Languages.Count(lang => lang == expectedLanguage) == 1,
                    $"because each analyzer should support only selected language ${expectedLanguage}");
        }

        private IEnumerable<Type> GetTypesAssignableTo<T>(Assembly assembly)
        {
            var type = typeof(T);
            return assembly.GetTypes().Where(innerType => type.IsAssignableFrom(innerType)).ToList();
        }
    }
}