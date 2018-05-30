﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
#if CSHARP
using Microsoft.CodeAnalysis.CSharp;
#elif VISUAL_BASIC
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
#endif

namespace NSubstitute.Analyzers.DiagnosticAnalyzers
{
    // TODO refactor
    public static class SubstituteConstructorMatcher
    {
        private static readonly SpecialType[] IntegerTypes;

        static SubstituteConstructorMatcher()
        {
            IntegerTypes = ((SpecialType[])Enum.GetValues(typeof(SpecialType))).Where(type => (int)type >= 11 && (int)type <= 16).ToArray();
        }

        public static bool MatchesInvocation(Compilation compilation, IMethodSymbol methodSymbol, IList<TypeInfo> argumentTypes)
        {
            if (methodSymbol.Parameters.Length != argumentTypes.Count)
            {
                return false;
            }

            return methodSymbol.Parameters.Length == 0 || methodSymbol.Parameters.Where((symbol, index) => IsConvertible(compilation, argumentTypes[index].Type, symbol.Type)).Any();
        }

        private static bool IsConvertible(Compilation compilation, ITypeSymbol source, ITypeSymbol destination)
        {
            if (source == null || source.Equals(destination))
            {
                return true;
            }

            var conversion = compilation.ClassifyConversion(source, destination);

            if (conversion.IsNumeric)
            {
                // for reasons unknown to me, NSubstitute cannot create mock when assigning int to decimal in constructor
                if (destination.Equals(compilation.GetSpecialType(SpecialType.System_Decimal)) && IntegerTypes
                        .Select(compilation.GetSpecialType).Any(specialType => specialType.Equals(source)))
                {
                    return false;
                }
            }

#if CSHARP
                return conversion.Exists && conversion.IsImplicit;

#elif VISUAL_BASIC
            return conversion.Exists && conversion.IsReference;
#endif
        }
    }
}