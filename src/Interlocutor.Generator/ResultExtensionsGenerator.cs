namespace Interlocutor.Generator;

using System.Linq;
using Interlocutor.Generator.Analysis;
using Interlocutor.Generator.Emission;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Emits the <c>ToActionResult</c>/<c>ToMinimalApiResult</c> per-arity overloads for the class attributed with
/// <c>GenerateResultExtensionsAttribute</c> - <c>Interlocutor.AspNetCore.ResultExtensions</c>, referencing this
/// generator as a <c>PrivateAssets="all"</c> analyzer over its own compilation, the same self-generation
/// pattern <c>Interlocutor.CommonKernel</c> already uses for <see cref="OutcomeMemberGenerator"/>.
/// </summary>
/// <remarks>
/// A standalone <see cref="IIncrementalGenerator"/>, separate from <see cref="InterlocutorSourceGenerator"/>
/// and <see cref="OutcomeMemberGenerator"/> - it reasons purely about the one attributed target class, not
/// about handler discovery or union member synthesis. Both run as ordinary generators inside the same
/// analyzer package (<c>IsRoslynComponent</c> already allows a project to contain more than one).
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class ResultExtensionsGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context
            .SyntaxProvider.ForAttributeWithMetadataName(
                WellKnownTypeNames.GenerateResultExtensionsAttribute,
                static (node, _) => node is ClassDeclarationSyntax,
                static (attributeContext, cancellationToken) =>
                    ResultExtensionsTargetModelBuilder.Build(attributeContext, cancellationToken)
            )
            .Where(static model => model is not null)
            .Select(static (model, _) => model!);

        context.RegisterSourceOutput(
            models,
            static (productionContext, model) =>
                productionContext.AddSource(model.HintName, ResultExtensionsEmitter.Emit(model))
        );
    }
}
