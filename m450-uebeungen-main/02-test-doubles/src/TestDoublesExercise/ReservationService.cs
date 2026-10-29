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

    // Speichert die gesamte Gruppe atomar: entweder alle Reservierungen oder keine.
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
        ValidateRequest(customerEmail, eventCode, seatCount);

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
        Reservation[] reservations = ValidateAndCopyBatch(requests);

        if (!HasEnoughSeatsForEveryEvent(reservations))
        {
            return false;
        }

        if (!_repository.TrySaveBatch(reservations))
        {
            return false;
        }

        SendConfirmations(reservations);
        return true;
    }

    private static void ValidateRequest(string customerEmail, string eventCode, int seatCount)
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
            throw new ArgumentOutOfRangeException(nameof(seatCount), "Die Anzahl Plätze muss positiv sein.");
        }
    }

    private static Reservation[] ValidateAndCopyBatch(IReadOnlyList<Reservation> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (requests.Count == 0)
        {
            throw new ArgumentException("Mindestens eine Reservierung ist erforderlich.", nameof(requests));
        }

        // Die Kopie verhindert, dass der Aufrufer die Gruppe während der Verarbeitung verändert.
        Reservation[] reservations = requests.ToArray();
        foreach (Reservation? reservation in reservations)
        {
            ValidateBatchReservation(reservation, nameof(requests));
        }

        return reservations;
    }

    private static void ValidateBatchReservation(Reservation? reservation, string parameterName)
    {
        if (reservation is null)
        {
            throw new ArgumentException("Ein Eintrag darf nicht null sein.", parameterName);
        }

        if (string.IsNullOrWhiteSpace(reservation.CustomerEmail))
        {
            throw new ArgumentException("Jede Reservierung benötigt eine E-Mail-Adresse.", parameterName);
        }

        if (string.IsNullOrWhiteSpace(reservation.EventCode))
        {
            throw new ArgumentException("Jede Reservierung benötigt einen Veranstaltungscode.", parameterName);
        }

        if (reservation.SeatCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Die Anzahl Plätze muss in jeder Reservierung positiv sein.");
        }
    }

    private bool HasEnoughSeatsForEveryEvent(IEnumerable<Reservation> reservations)
    {
        IEnumerable<IGrouping<string, Reservation>> reservationsByEvent = reservations
            .GroupBy(reservation => reservation.EventCode, StringComparer.Ordinal);

        bool enoughSeatsForEveryEvent = true;

        // Nicht vorzeitig abbrechen: Jede unterschiedliche Veranstaltung wird genau einmal abgefragt.
        foreach (IGrouping<string, Reservation> eventReservations in reservationsByEvent)
        {
            long requestedSeats = eventReservations.Sum(reservation => (long)reservation.SeatCount);
            int availableSeats = _availability.GetAvailableSeats(eventReservations.Key);

            if (availableSeats < requestedSeats)
            {
                enoughSeatsForEveryEvent = false;
            }
        }

        return enoughSeatsForEveryEvent;
    }

    private void SendConfirmations(IEnumerable<Reservation> reservations)
    {
        foreach (Reservation reservation in reservations)
        {
            _sender.Send(reservation);
        }
    }
}
