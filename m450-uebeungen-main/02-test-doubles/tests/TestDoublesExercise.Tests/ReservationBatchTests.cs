using Moq;
using Xunit;

namespace TestDoublesExercise.Tests;

public sealed class ReservationBatchTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReserveBatch_SavesAllOnce_AndConfirmsInInputOrderOnlyOnSuccess(bool saved)
    {
        // Arrange
        Reservation[] requests = [
            new("anna@example.com", "A", 2),
            new("ben@example.com", "B", 1),
            new("carla@example.com", "A", 1)];
        var availability = new SeatAvailabilityStub(("A", 3), ("B", 1));
        var sender = new ConfirmationSenderSpy();
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        repository.Setup(r => r.TrySaveBatch(It.Is<IReadOnlyList<Reservation>>(x => x.SequenceEqual(requests))))
            .Callback(() => Assert.Empty(sender.Sent))
            .Returns(saved);
        var service = new ReservationService(availability, repository.Object, sender);
        // Act
        bool result = service.ReserveBatch(requests);
        // Assert
        Assert.Equal(saved, result);
        Assert.Equal(new[] { "A", "B" }, availability.Calls);
        repository.Verify(r => r.TrySaveBatch(It.Is<IReadOnlyList<Reservation>>(x =>
            x.SequenceEqual(requests))), Times.Once());
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        repository.VerifyNoOtherCalls();
        Assert.Equal(saved ? requests : Array.Empty<Reservation>(), sender.Sent.ToArray());
    }

    [Theory]
    [InlineData(2, 2, 3)]
    [InlineData(int.MaxValue, int.MaxValue, int.MaxValue)]
    public void ReserveBatch_CombinedDemandExceedsCapacity_DoesNotSaveOrConfirm(int first, int second, int capacity)
    {
        // Arrange
        var availability = new SeatAvailabilityStub(("A", capacity));
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        Reservation[] requests = [new("a@example.com", "A", first), new("b@example.com", "A", second)];
        // Act
        bool result = service.ReserveBatch(requests);
        // Assert
        Assert.False(result);
        Assert.Equal(new[] { "A" }, availability.Calls);
        repository.Verify(r => r.TrySaveBatch(It.IsAny<IReadOnlyList<Reservation>>()), Times.Never());
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public void ReserveBatch_OneEventIsFull_ChecksEachEventOnceAndDoesNotSave()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(("A", 0), ("B", 5));
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        Reservation[] requests = [new("a@example.com", "A", 1), new("b@example.com", "B", 1)];
        // Act
        bool result = service.ReserveBatch(requests);
        // Assert
        Assert.False(result);
        Assert.Equal(new[] { "A", "B" }, availability.Calls);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.Sent);
    }

    public static IEnumerable<object?[]> InvalidRequests()
    {
        yield return [null, typeof(ArgumentNullException)];
        yield return [Array.Empty<Reservation>(), typeof(ArgumentException)];
        yield return [new Reservation[] { null! }, typeof(ArgumentException)];
        foreach (string? value in new string?[] { null, "", "   " })
        {
            yield return [new[] { new Reservation(value!, "A", 1) }, typeof(ArgumentException)];
            yield return [new[] { new Reservation("a@example.com", value!, 1) }, typeof(ArgumentException)];
        }
        yield return [new[] { new Reservation("a@example.com", "A", 0) }, typeof(ArgumentOutOfRangeException)];
        yield return [new[] { new Reservation("a@example.com", "A", -1) }, typeof(ArgumentOutOfRangeException)];
        // Auch ein später ungültiger Eintrag muss vor sämtlichen Abhängigkeitsaufrufen erkannt werden.
        yield return [new[] { new Reservation("a@example.com", "A", 1), new Reservation(" ", "B", 1) },
            typeof(ArgumentException)];
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public void ReserveBatch_InvalidInput_ThrowsExactTypeWithoutCallingDependencies(
        Reservation[]? requests, Type exceptionType)
    {
        // Arrange
        var availability = new SeatAvailabilityStub();
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        // Act
        var exception = Record.Exception(() => service.ReserveBatch(requests!));
        // Assert
        Assert.NotNull(exception);
        Assert.Equal(exceptionType, exception.GetType());
        Assert.Empty(availability.Calls);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.Sent);
    }
}
