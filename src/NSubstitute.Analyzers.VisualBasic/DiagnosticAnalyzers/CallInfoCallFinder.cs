using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using NSubstitute.Analyzers.Shared;
using NSubstitute.Analyzers.Shared.DiagnosticAnalyzers;

namespace NSubstitute.Analyzers.VisualBasic.DiagnosticAnalyzers
{
    internal class CallInfoCallFinder : AbstractCallInfoFinder<InvocationExpressionSyntax, InvocationExpressionSyntax>
    {
        public override CallInfoContext<InvocationExpressionSyntax, InvocationExpressionSyntax> GetCallInfoContext(SemanticModel semanticModel, SyntaxNode syntaxNode)
        {
            var visitor = new CallInfoVisitor(semanticModel);
            visitor.Visit(syntaxNode);

            return new CallInfoContext<InvocationExpressionSyntax, InvocationExpressionSyntax>(visitor.ArgAtInvocations, visitor.ArgInvocations, visitor.DirectIndexerAccesses);
        }

        private class CallInfoVisitor : VisualBasicSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;

            public List<InvocationExpressionSyntax> ArgAtInvocations { get; }

            public List<InvocationExpressionSyntax> ArgInvocations { get; }

            public List<InvocationExpressionSyntax> DirectIndexerAccesses { get; }

            public CallInfoVisitor(SemanticModel semanticModel)
            {
                _semanticModel = semanticModel;
                DirectIndexerAccesses = new List<InvocationExpressionSyntax>();
                ArgAtInvocations = new List<InvocationExpressionSyntax>();
                ArgInvocations = new List<InvocationExpressionSyntax>();
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var symbol = _semanticModel.GetSymbolInfo(node).Symbol;

                if (symbol != null && symbol.ContainingType.ToString().Equals(MetadataNames.NSubstituteCoreFullTypeName))
                {
                    if (symbol.Name == MetadataNames.CallInfoArgAtMethod)
                    {
                        ArgAtInvocations.Add(node);
                    }

                    if (symbol.Name == MetadataNames.CallInfoArgMethod)
                    {
                        ArgInvocations.Add(node);
                    }
                }

                var expressionSymbol = _semanticModel.GetSymbolInfo(node.Expression).Symbol;
                if (symbol == null && expressionSymbol != null && expressionSymbol.ContainingType.ToString().Equals(MetadataNames.NSubstituteCoreFullTypeName))
                {
                    DirectIndexerAccesses.Add(node);
                }

                base.VisitInvocationExpression(node);
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                base.VisitMemberAccessExpression(node);
            }
        }
    }
}