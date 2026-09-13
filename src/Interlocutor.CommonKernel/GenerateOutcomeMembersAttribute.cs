namespace Interlocutor.CommonKernel;

using System;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>
/// Marks a <c>partial union</c> declaration for source-generated case-construction, case-projection,
/// equality, and <c>ToString</c> members.
/// </summary>
/// <remarks>
/// <para>
/// Applying this attribute to an attributed <c>partial union</c> emits a second partial file adding,
/// unconditionally:
/// </para>
/// <list type="bullet">
/// <item><description>A static <c>Create</c> overload per case type - a discoverable alternative to the
/// compiler-generated positional constructor/implicit conversion for the same case value.</description></item>
/// <item><description><c>Match&lt;TResult&gt;</c> - one delegate per case, compiler-enforced exhaustive,
/// projecting the outcome to a value.</description></item>
/// <item><description><c>Switch</c> - the void-returning, side-effecting companion to <c>Match</c>.</description></item>
/// <item><description>A per-case, non-generic, overloaded <c>TryGetValue(out TCaseN value)</c> for every case
/// type - the shape the union language feature's own pattern-matching already recognizes and prefers, avoiding
/// boxing on the value-type path - plus a named <c>TryGet{CaseTypeName}</c> counterpart for every case type
/// that isn't itself one of the union's own generic type parameters.</description></item>
/// <item><description>Value-based <c>Equals</c>, <c>GetHashCode</c>, <c>IEquatable&lt;TSelf&gt;</c>, and
/// <c>==</c>/<c>!=</c>, all delegating to the active case value's own equality.</description></item>
/// <item><description><c>override string ToString()</c> forwarding to <c>this.Value?.ToString()</c>, so a case
/// type's own <c>ToString()</c> override surfaces automatically.</description></item>
/// </list>
/// <para>
/// Additionally, when the union's own declaration also lists <see cref="IOutcome"/> in its base list, this
/// emits <see cref="IOutcomeCase"/> and its <see cref="IOutcomeCase.ActiveCaseType"/> member. A union attributed
/// here but declared without <c>: IOutcome</c> still gets every member above in full - it's a plain tagged
/// union with no success/failure opinion, not a lesser outcome, and it never enters the reflective
/// case-resolution fallback <see cref="IOutcomeCase"/> exists to support.
/// </para>
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
