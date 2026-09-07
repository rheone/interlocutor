namespace Interlocutor.CommonKernel;

using System;

/// <summary>
/// Marks a <c>partial union</c> declaration for source-generated <c>Match</c>, <c>Switch</c>, per-case
/// <c>TryGetValue</c>, and <c>ToString</c> members.
/// </summary>
/// <remarks>
/// <para>
/// Applying this attribute to an attributed <c>partial union</c> emits a second partial file adding:
/// </para>
/// <list type="bullet">
/// <item><description><c>Match&lt;TResult&gt;</c> - one delegate per case, compiler-enforced exhaustive,
/// projecting the outcome to a value.</description></item>
/// <item><description><c>Switch</c> - the void-returning, side-effecting companion to <c>Match</c>.</description></item>
/// <item><description>A per-case, non-generic, overloaded <c>TryGetValue(out TCaseN value)</c> for every case
/// type - the shape the union language feature's own pattern-matching already recognizes and prefers, avoiding
/// boxing on the value-type path.</description></item>
/// <item><description><c>override string ToString()</c> forwarding to <c>this.Value?.ToString()</c>, so a case
/// type's own <c>ToString()</c> override surfaces automatically.</description></item>
/// </list>
/// <para>
/// Lives in this zero-dependency package (already home to <c>IOutcome</c>/<c>Outcome</c>) rather than in
/// <c>Interlocutor.Generator</c>, so referencing it never adds a dependency on the generator package itself -
/// only <c>Interlocutor.Abstractions</c>' existing <c>PrivateAssets="all"</c> analyzer reference on the
/// generator is what actually runs it. Declaring a union with fewer than two case types and attributing it is
/// a build error (<c>INTERLOC011</c>): a single-case union has no alternative for a <c>switch</c> to be
/// exhaustive over, so it isn't a meaningful outcome. Declaring a union with more than sixteen case types is
/// also a build error (<c>INTERLOC013</c>): <c>Match</c>/<c>Switch</c> bind one delegate parameter per case to
/// the BCL's <c>System.Func&lt;T1..T16, TResult&gt;</c>/<c>System.Action&lt;T1..T16&gt;</c> families, which
/// have no overload wider than sixteen type parameters. See <c>documentation/extensibility.md</c> for a worked
/// example of a consumer declaring their own attributed arity.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class GenerateOutcomeMembersAttribute : Attribute;
