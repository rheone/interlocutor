namespace Interlocutor.TestSupport.Tests;

using System.Diagnostics;

/// <summary>Tests for <see cref="DebuggerDisplayVerifier"/>.</summary>
// S2325 wants every instance member below to be static since none read instance state, but these are
// deliberately shaped as ordinary instance members - the realistic case DebuggerDisplay expressions are
// written against - not a design a caller would actually use standing alone.
#pragma warning disable S2325
public class DebuggerDisplayVerifierTests
{
    [Fact]
    public void AssertValid_SimpleMemberReference_Passes()
    {
        // Act & Assert - no exception
        DebuggerDisplayVerifier.AssertValid(typeof(WithValidSimpleMember));
    }

    [Fact]
    public void AssertValid_NonexistentMemberReference_Throws()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => DebuggerDisplayVerifier.AssertValid(typeof(WithInvalidMember)));
    }

    [Fact]
    public void AssertValid_NoQuoteSuffix_ParsesMemberNameCorrectly()
    {
        // Act & Assert - no exception; ",nq" is stripped before member lookup
        DebuggerDisplayVerifier.AssertValid(typeof(WithNoQuoteSuffix));
    }

    [Fact]
    public void AssertValid_NestedMemberReference_Passes()
    {
        // Act & Assert - no exception
        DebuggerDisplayVerifier.AssertValid(typeof(WithValidNestedMember));
    }

    [Fact]
    public void AssertValid_InvalidNestedMemberReference_Throws()
    {
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => DebuggerDisplayVerifier.AssertValid(typeof(WithInvalidNestedMember)));
    }

    [Fact]
    public void AssertValid_LiteralTextAndRepeatedMember_IgnoresLiteralTextAndPasses()
    {
        // Act & Assert - no exception
        DebuggerDisplayVerifier.AssertValid(typeof(WithLiteralTextAndRepeatedMember));
    }

    [Fact]
    public void AssertValid_ParameterlessMethodReference_Passes()
    {
        // Act & Assert - no exception
        DebuggerDisplayVerifier.AssertValid(typeof(WithValidParameterlessMethod));
    }

    [Fact]
    public void AssertValid_PrivateFieldReference_Passes()
    {
        // Act & Assert - a DebuggerDisplay expression is evaluated in the debugger's own scope, which can
        // see a type's private members too.
        DebuggerDisplayVerifier.AssertValid(typeof(WithValidPrivateField));
    }

    [Fact]
    public void AssertValid_NoDebuggerDisplayAttribute_TriviallyPasses()
    {
        // Act & Assert - no exception; a type with no attribute at all is not an error
        DebuggerDisplayVerifier.AssertValid(typeof(WithNoAttribute));
    }

    [Fact]
    public void AssertValid_NullType_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(static () => DebuggerDisplayVerifier.AssertValid(null!));
    }

    [DebuggerDisplay("{Name}")]
    private sealed class WithValidSimpleMember
    {
        public string Name => "irrelevant";
    }

    // S4545 (correctly) flags this fixture's own DebuggerDisplay string as referencing a nonexistent member -
    // that is deliberately the point of this fixture.
#pragma warning disable S4545
    [DebuggerDisplay("{Nonexistent}")]
    private sealed class WithInvalidMember
    {
        public string Name => "irrelevant";
    }
#pragma warning restore S4545

    [DebuggerDisplay("{Name,nq}")]
    private sealed class WithNoQuoteSuffix
    {
        public string Name => "irrelevant";
    }

    [DebuggerDisplay("{Inner.Name}")]
    private sealed class WithValidNestedMember
    {
        public WithValidSimpleMember Inner => new();
    }

#pragma warning disable S4545
    [DebuggerDisplay("{Inner.Nonexistent}")]
    private sealed class WithInvalidNestedMember
    {
        public WithValidSimpleMember Inner => new();
    }
#pragma warning restore S4545

    [DebuggerDisplay("prefix {Name} suffix, more {Name}")]
    private sealed class WithLiteralTextAndRepeatedMember
    {
        public string Name => "irrelevant";
    }

    [DebuggerDisplay("{Describe()}")]
    private sealed class WithValidParameterlessMethod
    {
        public string Describe() => "irrelevant";
    }

    [DebuggerDisplay("{_field}")]
    private sealed class WithValidPrivateField
    {
#pragma warning disable CS0169, IDE0051, S1144, RCS1213
        private readonly int _field;
#pragma warning restore CS0169, IDE0051, S1144, RCS1213
    }

    private sealed class WithNoAttribute
    {
        public string Name => "irrelevant";
    }
}
#pragma warning restore S2325
