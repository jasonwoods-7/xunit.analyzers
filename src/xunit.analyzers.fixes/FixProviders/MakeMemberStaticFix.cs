﻿using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit.Analyzers.CodeActions;

namespace Xunit.Analyzers.FixProviders
{
	[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
	public sealed class MakeMemberStaticFix : MemberFixBase
	{
		const string title = "Make Member Static";

		public MakeMemberStaticFix()
			: base(new[] { Descriptors.X1017_MemberDataMustReferenceStaticMember.Id })
		{ }

		public override Task RegisterCodeFixesAsync(
			CodeFixContext context,
			ISymbol member)
		{
			context.RegisterCodeFix(
				CodeAction.Create(
					title: title,
					createChangedSolution: ct => context.Document.Project.Solution.ChangeMemberStaticModifier(member, true, ct),
					equivalenceKey: title
				),
				context.Diagnostics
			);

			return Task.CompletedTask;
		}
	}
}
