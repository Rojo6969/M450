using Moq;
using Xunit;

namespace TestDoublesExercise.Tests;

public sealed class ReservationServiceTests
{
    [Fact]
    public void Constructor_NullAvailability_Throws()
    {
        // Act
        Action action = () => new ReservationService(null!, new ReservationRepositoryStub(), new ConfirmationSenderSpy());
        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_NullRepository_Throws()
    {
        // Act
        Action action = () => new ReservationService(new SeatAvailabilityStub(), null!, new ConfirmationSenderSpy());
        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void Constructor_NullSender_Throws()
    {
        // Act
        Action action = () => new ReservationService(new SeatAvailabilityStub(), new ReservationRepositoryStub(), null!);
        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData(3, 2, true)]
    [InlineData(1, 2, false)]
    [InlineData(2, 2, true)]
    public void Reserve_ReturnsWhetherRequestedSeatsCanBeReserved(int available, int requested, bool expected)
    {
        // Arrange
        var availability = new SeatAvailabilityStub(("KONZERT", available));
        var repository = new ReservationRepositoryStub();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        // Act
        bool result = service.Reserve("anna@example.com", "KONZERT", requested);
        // Assert
        Assert.Equal(expected, result);
        Assert.Equal(expected ? 1 : 0, repository.Saved.Count);
        Assert.Equal(expected ? 1 : 0, sender.Sent.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_InvalidSeatCount_ThrowsWithoutCallingDependencies(int count)
    {
        // Arrange
        var availability = new SeatAvailabilityStub();
        var repository = new ReservationRepositoryStub();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        // Act
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => service.Reserve("anna@example.com", "KONZERT", count));
        // Assert
        Assert.Equal("seatCount", exception.ParamName);
        Assert.Empty(availability.Calls);
        Assert.Empty(repository.Saved);
        Assert.Empty(repository.Batches);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public void Reserve_EmptyEventCode_ThrowsWithoutCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub();
        var repository = new ReservationRepositoryStub();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        // Act
        var exception = Assert.Throws<ArgumentException>(
            () => service.Reserve("anna@example.com", "", 2));
        // Assert
        Assert.Equal("eventCode", exception.ParamName);
        Assert.Empty(availability.Calls);
        Assert.Empty(repository.Saved);
        Assert.Empty(repository.Batches);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public void Reserve_Success_SendsOneConfirmationWithCorrectDetailsAfterSaving()
    {
        // Arrange
        var availability = new SeatAvailabilityStub(("KONZERT", 3));
        var repository = new ReservationRepositoryStub();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository, sender);
        // Act
        bool result = service.Reserve("anna@example.com", "KONZERT", 2);
        // Assert
        Assert.True(result);
        var sent = Assert.Single(sender.Sent);
        Assert.Equal(new Reservation("anna@example.com", "KONZERT", 2), sent);
        Assert.Same(Assert.Single(repository.Saved), sent);
    }

    [Fact]
    public void Reserve_SaveFails_DoesNotSendConfirmation()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(("KONZERT", 3)),
            new ReservationRepositoryStub(false), sender);
        // Act
        bool result = service.Reserve("anna@example.com", "KONZERT", 2);
        // Assert
        Assert.False(result);
        Assert.Empty(sender.Sent);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Reserve_WithMoq_SavesCorrectReservationAndConfirmsOnlyOnSuccess(bool saved)
    {
        // Arrange
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        repository.Setup(r => r.TrySave(It.Is<Reservation>(x =>
            x.CustomerEmail == "anna@example.com" && x.EventCode == "KONZERT" && x.SeatCount == 2)))
            .Callback(() => Assert.Empty(sender.Sent))
            .Returns(saved);
        var service = new ReservationService(new SeatAvailabilityStub(("KONZERT", 3)), repository.Object, sender);
        // Act
        bool result = service.Reserve("anna@example.com", "KONZERT", 2);
        // Assert
        Assert.Equal(saved, result);
        repository.Verify(r => r.TrySave(It.Is<Reservation>(x =>
            x.CustomerEmail == "anna@example.com" && x.EventCode == "KONZERT" && x.SeatCount == 2)), Times.Once());
        repository.VerifyNoOtherCalls();
        Assert.Equal(saved ? 1 : 0, sender.Sent.Count);
        if (saved)
            Assert.Equal(new Reservation("anna@example.com", "KONZERT", 2), Assert.Single(sender.Sent));
    }

    [Fact]
    public void Reserve_InsufficientSeats_NeverSavesOrConfirms()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(new SeatAvailabilityStub(("KONZERT", 1)), repository.Object, sender);
        // Act
        bool result = service.Reserve("anna@example.com", "KONZERT", 2);
        // Assert
        Assert.False(result);
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public void Reserve_WhitespaceEmail_ThrowsWithoutCallingDependencies()
    {
        // Arrange
        var availability = new SeatAvailabilityStub();
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        // Act
        var exception = Assert.Throws<ArgumentException>(() => service.Reserve("   ", "KONZERT", 2));
        // Assert
        Assert.Equal("customerEmail", exception.ParamName);
        Assert.Empty(availability.Calls);
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.Sent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reserve_MissingEmail_ThrowsWithoutCallingDependencies(string? email)
    {
        // Arrange
        var availability = new SeatAvailabilityStub();
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        // Act
        var exception = Assert.Throws<ArgumentException>(() => service.Reserve(email!, "KONZERT", 2));
        // Assert
        Assert.Equal("customerEmail", exception.ParamName);
        Assert.Empty(availability.Calls);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.Sent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reserve_MissingEventCode_ThrowsWithoutCallingDependencies(string? eventCode)
    {
        // Arrange
        var availability = new SeatAvailabilityStub();
        var repository = new Mock<IReservationRepository>(MockBehavior.Strict);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(availability, repository.Object, sender);
        // Act
        var exception = Assert.Throws<ArgumentException>(() => service.Reserve("anna@example.com", eventCode!, 2));
        // Assert
        Assert.Equal("eventCode", exception.ParamName);
        Assert.Empty(availability.Calls);
        repository.VerifyNoOtherCalls();
        Assert.Empty(sender.Sent);
    }
}
