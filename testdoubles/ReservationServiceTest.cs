using System;
using Moq;
using TestDoublesExercise.Tests.Doubles;
using Xunit;

namespace TestDoublesExercise.Tests;

/*
  Aufgabe 1
  ReservationService hängt nur von der Schnittstelle ISeatAvailability ab, nicht von
  einer konkreten Klasse. Deshalb können wir im Test einen eigenen Stub übergeben und
  den Service testen, obwohl die produktive Implementierung noch fehlt.
  Die Schnittstelle legt bereits fest: Es gibt eine Methode GetAvailableSeats, die einen
  Veranstaltungscode (string) entgegennimmt und eine ganze Zahl (int) zurückgibt.
 
  Aufgabe 3
  Der Stub liefert dem Service vorgegebene Daten, während der Spy aufzeichnet,
  was der Service mit ihm macht, damit der Test es danach prüfen kann.
 */
public class ReservationServiceTests
{
    private const string Email = "anna@example.com";
    private const string EventCode = "KONZERT-2026";

  
    [Fact]
    public void Reserve_ThreeSeatsAvailable_TwoRequested_ReturnsTrue()
    {
        // Arrange
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 3),
            new FakeReservationRepository(),
            new ConfirmationSenderSpy());

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Reserve_OneSeatAvailable_TwoRequested_ReturnsFalse()
    {
        // Arrange
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 1),
            new FakeReservationRepository(),
            new ConfirmationSenderSpy());

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Reserve_ExactlyTwoSeatsAvailable_TwoRequested_ReturnsTrue()
    {
        // Arrange
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 2),
            new FakeReservationRepository(),
            new ConfirmationSenderSpy());

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.True(result);
    }



    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Reserve_SeatCountNotPositive_ThrowsAndCallsNoDependency(int seatCount)
    {
        // Arrange
        var repository = new FakeReservationRepository();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 10), repository, sender);

        // Act
        Action action = () => service.Reserve(Email, EventCode, seatCount);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_EmptyEventCode_ThrowsAndCallsNoDependency()
    {
        // Arrange
        var repository = new FakeReservationRepository();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 10), repository, sender);

        // Act
        Action action = () => service.Reserve(Email, "", 2);

        // Assert
        Assert.Throws<ArgumentException>(action);
        Assert.Equal(0, repository.SaveCalls);
        Assert.Empty(sender.SentReservations);
    }


    [Fact]
    public void Reserve_SaveSucceeds_SendsExactlyOneConfirmationWithCorrectData()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 3), new FakeReservationRepository(), sender);

        // Act
        service.Reserve(Email, EventCode, 2);

        // Assert
        var sent = Assert.Single(sender.SentReservations);
        Assert.Equal(Email, sent.CustomerEmail);
        Assert.Equal(EventCode, sent.EventCode);
        Assert.Equal(2, sent.SeatCount);
    }

    [Fact]
    public void Reserve_NotEnoughSeats_SendsNoConfirmation()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 1), new FakeReservationRepository(), sender);

        // Act
        service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_SaveFails_SendsNoConfirmation()
    {
        // Arrange
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 3), new FakeReservationRepository(result: false), sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        Assert.Empty(sender.SentReservations);
    }


    [Fact]
    public void Reserve_EnoughSeats_SaveSucceeds_SavesCorrectReservationOnce()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(r => r.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 3), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.True(result);
        repository.Verify(r => r.TrySave(It.Is<Reservation>(x =>
                x.CustomerEmail == Email &&
                x.EventCode == EventCode &&
                x.SeatCount == 2)),
            Times.Once());
        Assert.Single(sender.SentReservations);
    }

    [Fact]
    public void Reserve_EnoughSeats_SaveFails_ReturnsFalseAndSendsNothing()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(r => r.TrySave(It.IsAny<Reservation>())).Returns(false);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 3), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        repository.Verify(r => r.TrySave(It.Is<Reservation>(x =>
                x.CustomerEmail == Email &&
                x.EventCode == EventCode &&
                x.SeatCount == 2)),
            Times.Once());
        Assert.Empty(sender.SentReservations);
    }

    [Fact]
    public void Reserve_NotEnoughSeats_NeverTriesToSave()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        repository.Setup(r => r.TrySave(It.IsAny<Reservation>())).Returns(true);
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 1), repository.Object, sender);

        // Act
        var result = service.Reserve(Email, EventCode, 2);

        // Assert
        Assert.False(result);
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }

   

    [Fact]
    public void Reserve_EmailOnlyWhitespace_ThrowsAndNeitherSavesNorSends()
    {
        // Arrange
        var repository = new Mock<IReservationRepository>();
        var sender = new ConfirmationSenderSpy();
        var service = new ReservationService(
            new SeatAvailabilityStub(EventCode, 10), repository.Object, sender);

        // Act
        Action action = () => service.Reserve("   ", EventCode, 2);

        // Assert
        Assert.Throws<ArgumentException>(action);
        repository.Verify(r => r.TrySave(It.IsAny<Reservation>()), Times.Never());
        Assert.Empty(sender.SentReservations);
    }
}