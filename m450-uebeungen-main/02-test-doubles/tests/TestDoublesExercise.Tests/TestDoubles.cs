using TestDoublesExercise;

namespace TestDoublesExercise.Tests;

internal sealed class SeatAvailabilityStub : ISeatAvailability
{
    private readonly Dictionary<string, int> _availableSeats;
    private readonly List<string> _calls = [];

    public SeatAvailabilityStub(params (string EventCode, int Seats)[] seats)
    {
        _availableSeats = seats.ToDictionary(entry => entry.EventCode, entry => entry.Seats);
    }

    public IReadOnlyList<string> Calls => _calls;

    public int GetAvailableSeats(string eventCode)
    {
        _calls.Add(eventCode);
        return _availableSeats[eventCode];
    }
}

internal sealed class ReservationRepositoryStub : IReservationRepository
{
    private readonly bool _saveResult;
    private readonly List<Reservation> _saved = [];
    private readonly List<IReadOnlyList<Reservation>> _savedBatches = [];

    public ReservationRepositoryStub(bool saveResult = true)
    {
        _saveResult = saveResult;
    }

    public IReadOnlyList<Reservation> Saved => _saved;

    public IReadOnlyList<IReadOnlyList<Reservation>> Batches => _savedBatches;

    public bool TrySave(Reservation reservation)
    {
        _saved.Add(reservation);
        return _saveResult;
    }

    public bool TrySaveBatch(IReadOnlyList<Reservation> reservations)
    {
        _savedBatches.Add(reservations.ToArray());
        return _saveResult;
    }
}

internal sealed class ConfirmationSenderSpy : IConfirmationSender
{
    private readonly List<Reservation> _sent = [];

    public IReadOnlyList<Reservation> Sent => _sent;

    public void Send(Reservation reservation)
    {
        _sent.Add(reservation);
    }
}
