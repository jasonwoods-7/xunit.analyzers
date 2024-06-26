using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Xunit.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class TheoryDataRowArgumentsShouldBeSerializable : XunitDiagnosticAnalyzer
{
	public TheoryDataRowArgumentsShouldBeSerializable() :
		base(
			Descriptors.X1046_AvoidUsingTheoryDataRowArgumentsThatAreNotSerializable,
			Descriptors.X1047_AvoidUsingTheoryDataRowArgumentsThatMightNotBeSerializable
		)
	{ }

	public override void AnalyzeCompilation(
		CompilationStartAnalysisContext context,
		XunitContext xunitContext)
	{
		Guard.ArgumentNotNull(context);
		Guard.ArgumentNotNull(xunitContext);

		if (SerializableTypeSymbols.Create(context.Compilation, xunitContext) is not SerializableTypeSymbols typeSymbols)
			return;

		var analyzer = new SerializabilityAnalyzer(typeSymbols);

		context.RegisterOperationAction(context =>
		{
			if (context.Operation is not IObjectCreationOperation objectCreation)
				return;

			var argumentOperations = GetConstructorArguments(objectCreation);
			if (argumentOperations is null)
				return;

			foreach (var argumentOperation in argumentOperations)
			{
				if (analyzer.TypeShouldBeIgnored(argumentOperation.Type))
					continue;

				var serializability = analyzer.AnalayzeSerializability(argumentOperation.Type);

				if (serializability != Serializability.AlwaysSerializable)
				{
					var typeDisplayName =
						argumentOperation.SemanticModel is null
							? argumentOperation.Type.Name
							: argumentOperation.Type.ToMinimalDisplayString(argumentOperation.SemanticModel, argumentOperation.Syntax.SpanStart);

					context.ReportDiagnostic(
						Diagnostic.Create(
							serializability == Serializability.NeverSerializable
								? Descriptors.X1046_AvoidUsingTheoryDataRowArgumentsThatAreNotSerializable
								: Descriptors.X1047_AvoidUsingTheoryDataRowArgumentsThatMightNotBeSerializable,
							argumentOperation.Syntax.GetLocation(),
							argumentOperation.Syntax.ToFullString(),
							typeDisplayName
						)
					);
				}
			}

		}, OperationKind.ObjectCreation);
	}

	static IReadOnlyList<IOperation>? GetConstructorArguments(IObjectCreationOperation objectCreation)
	{
		if (objectCreation.Arguments.FirstOrDefault() is not IArgumentOperation argumentOperation)
			return null;

#if ROSLYN_3_11
		var firstArgument = argumentOperation.Children.FirstOrDefault();
#else
		var firstArgument = argumentOperation.ChildOperations.FirstOrDefault();
#endif
		if (firstArgument is null)
			return null;

		// Common pattern: implicit array creation for the params array
		if (firstArgument is IArrayCreationOperation arrayCreation &&
#if ROSLYN_3_11
			arrayCreation.Children.Skip(1).FirstOrDefault() is IArrayInitializerOperation arrayInitializer)
#else
			arrayCreation.ChildOperations.Skip(1).FirstOrDefault() is IArrayInitializerOperation arrayInitializer)
#endif
		{
			var result = new List<IOperation>();

			for (var idx = 0; idx < arrayInitializer.ElementValues.Length; ++idx)
			{
				var elementValue = arrayInitializer.ElementValues[idx];
				while (elementValue is IConversionOperation conversion)
					elementValue = conversion.Operand;

				result.Add(elementValue);
			}

			return result;
		}

		// TODO: Less common pattern: user created the array ahead of time, which shows up as ILocalReferenceOperation

		return null;
	}
}
