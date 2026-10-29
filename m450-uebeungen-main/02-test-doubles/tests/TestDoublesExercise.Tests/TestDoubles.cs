using TestDoublesExercise;

namespace TestDoublesExercise.Tests;

internal sealed class SeatAvailabilityStub(params (string EventCode, int Seats)[] seats) : ISeatAvailability
{
    private readonly Dictionary<string, int> _seats = seats.ToDictionary(s => s.EventCode, s => s.Seats);
    public List<string> Calls { get; } = [];

    public int GetAvailableSeats(string eventCode)
    {
        Calls.Add(eventCode);
        return _seats[eventCode];
    }
}

internal sealed class ReservationRepositoryStub(bool result = true) : IReservationRepository
{
    public List<Reservation> Saved { get; } = [];
    public List<IReadOnlyList<Reservation>> Batches { get; } = [];
    public bool TrySave(Reservation reservation)
    {
        Saved.Add(reservation);
        return result;
    }
    public bool TrySaveBatch(IReadOnlyList<Reservation> reservations)
    {
        Batches.Add(reservations.ToArray());
        return result;
    }
}

internal sealed class ConfirmationSenderSpy : IConfirmationSender
{
    public List<Reservation> Sent { get; } = [];
    public void Send(Reservation reservation) => Sent.Add(reservation);
}
