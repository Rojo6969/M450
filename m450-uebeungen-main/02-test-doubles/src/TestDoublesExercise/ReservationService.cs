namespace TestDoublesExercise;

public sealed record Reservation(string CustomerEmail, string EventCode, int SeatCount);

// Für diese Schnittstelle existiert in der Übung noch keine produktive Implementierung.
public interface ISeatAvailability
{
    int GetAvailableSeats(string eventCode);
}

public interface IReservationRepository
{
    // Gibt false zurück, wenn die Reservierung nicht gespeichert werden konnte.
    bool TrySave(Reservation reservation);
    bool TrySaveBatch(IReadOnlyList<Reservation> reservations);
}

public interface IConfirmationSender
{
    void Send(Reservation reservation);
}

public sealed class ReservationService
{
    private readonly ISeatAvailability _availability;
    private readonly IReservationRepository _repository;
    private readonly IConfirmationSender _sender;

    public ReservationService(
        ISeatAvailability availability,
        IReservationRepository repository,
        IConfirmationSender sender)
    {
        _availability = availability ?? throw new ArgumentNullException(nameof(availability));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _sender = sender ?? throw new ArgumentNullException(nameof(sender));
    }

    public bool Reserve(string customerEmail, string eventCode, int seatCount)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new ArgumentException("Eine E-Mail-Adresse ist erforderlich.", nameof(customerEmail));
        }

        if (string.IsNullOrWhiteSpace(eventCode))
        {
            throw new ArgumentException("Ein Veranstaltungscode ist erforderlich.", nameof(eventCode));
        }

        if (seatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seatCount));
        }

        if (_availability.GetAvailableSeats(eventCode) < seatCount)
        {
            return false;
        }

        var reservation = new Reservation(customerEmail, eventCode, seatCount);
        if (!_repository.TrySave(reservation))
        {
            return false;
        }

        _sender.Send(reservation);
        return true;
    }

    public bool ReserveBatch(IReadOnlyList<Reservation> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        if (requests.Count == 0)
            throw new ArgumentException("Mindestens eine Reservierung ist erforderlich.", nameof(requests));

        // Erst alle Eingaben prüfen, bevor eine Abhängigkeit aufgerufen wird.
        var reservations = requests.ToArray();
        foreach (var request in reservations)
        {
            if (request is null)
                throw new ArgumentException("Ein Eintrag darf nicht null sein.", nameof(requests));
            if (string.IsNullOrWhiteSpace(request.CustomerEmail) ||
                string.IsNullOrWhiteSpace(request.EventCode))
                throw new ArgumentException("E-Mail und Veranstaltungscode sind erforderlich.", nameof(requests));
            if (request.SeatCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(requests));
        }

        var totals = reservations.GroupBy(r => r.EventCode)
            .Select(group => new { EventCode = group.Key, Seats = group.Sum(r => (long)r.SeatCount) });
        bool enoughSeats = true;
        foreach (var total in totals)
        {
            if (_availability.GetAvailableSeats(total.EventCode) < total.Seats)
                enoughSeats = false;
        }

        if (!enoughSeats || !_repository.TrySaveBatch(reservations))
            return false;

        foreach (var reservation in reservations)
            _sender.Send(reservation);
        return true;
    }
}

