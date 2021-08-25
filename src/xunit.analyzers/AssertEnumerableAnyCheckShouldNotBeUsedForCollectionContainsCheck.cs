﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Xunit.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class AssertEnumerableAnyCheckShouldNotBeUsedForCollectionContainsCheck : AssertUsageAnalyzerBase
	{
		const string enumerableAnyExtensionMethod = "System.Linq.Enumerable.Any<TSource>(System.Collections.Generic.IEnumerable<TSource>, System.Func<TSource, bool>)";
		static readonly string[] targetMethods =
		{
			Constants.Asserts.True,
			Constants.Asserts.False
		};

		public AssertEnumerableAnyCheckShouldNotBeUsedForCollectionContainsCheck()
			: base(Descriptors.X2012_AssertEnumerableAnyCheckShouldNotBeUsedForCollectionContainsCheck, targetMethods)
		{ }

		protected override void Analyze(
			OperationAnalysisContext context,
			IInvocationOperation invocationOperation,
			IMethodSymbol method)
		{
			var arguments = invocationOperation.Arguments;
			if (arguments.Length != 1)
				return;
			if (arguments[0].Value is not IInvocationOperation invocationExpression)
				return;

			var methodSymbol = invocationExpression.TargetMethod;
			if (SymbolDisplay.ToDisplayString(methodSymbol.OriginalDefinition) != enumerableAnyExtensionMethod)
				return;

			var builder = ImmutableDictionary.CreateBuilder<string, string>();
			builder[Constants.Properties.AssertMethodName] = method.Name;

			context.ReportDiagnostic(
				Diagnostic.Create(
					Descriptors.X2012_AssertEnumerableAnyCheckShouldNotBeUsedForCollectionContainsCheck,
					invocationOperation.Syntax.GetLocation(),
					builder.ToImmutable(),
					SymbolDisplay.ToDisplayString(
						method,
						SymbolDisplayFormat
							.CSharpShortErrorMessageFormat
							.WithParameterOptions(SymbolDisplayParameterOptions.None)
							.WithGenericsOptions(SymbolDisplayGenericsOptions.None)
					)
				)
			);
		}
	}
}
