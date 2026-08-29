namespace Interlocutor.Abstractions.Tests;

using Interlocutor.Abstractions.Pipeline;
using Interlocutor.CommonKernel;
using Interlocutor.CommonKernel.ResultTypes;

/// <summary>Tests for <see cref="Eject"/> and <see cref="PipelineEjectException{TResponse}"/>.</summary>
public class EjectTests
{
    [Fact]
    public void With_Throws_PipelineEjectExceptionCarryingTheGivenResponse()
    {
        // Arrange
        var response = new OperationOutcome<Success, Failure>(new Failure("dead end"));

        // Act
        var ex = Assert.Throws<PipelineEjectException<OperationOutcome<Success, Failure>>>(() => Eject.With(response));

        // Assert
        Assert.Equal(response, ex.Response);
    }

    [Fact]
    public void With_Message_NamesTheResponseType()
    {
        // Arrange
        var response = new OperationOutcome<Success, Failure>(new Failure("dead end"));

        // Act
        var ex = Assert.Throws<PipelineEjectException<OperationOutcome<Success, Failure>>>(() => Eject.With(response));

        // Assert - the message exists to help anyone who sees this signal leak out of a dispatch (a missing
        // catch site), so it must actually name the response type rather than being generic boilerplate.
        Assert.Contains(nameof(OperationOutcome<Success, Failure>), ex.Message);
    }
}
