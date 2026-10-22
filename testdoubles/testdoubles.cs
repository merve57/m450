using System.Collections.Generic;

namespace TestDoublesExercise.Tests.Doubles;


/* Liefert für einen festgelegten Veranstaltungscode eine festgelegte
Anzahl verfügbarer Plätze. Berechnet keine echte Verfügbarkeit.
Annahme: Für unbekannte Veranstaltungscodes sind 0 Plätze verfügbar.
*/

public sealed class SeatAvailabilityStub : ISeatAvailability
{
    private readonly string _eventCode;
    private readonly int _availableSeats;

    public SeatAvailabilityStub(string eventCode, int availableSeats)
    {
        _eventCode = eventCode;
        _availableSeats = availableSeats;
    }

    public int GetAvailableSeats(string eventCode) =>
        eventCode == _eventCode ? _availableSeats : 0;
}


//Zeichnet alle übergebenen Reservierungen in ihrer Reihenfolge auf.

public sealed class ConfirmationSenderSpy : IConfirmationSender
{
    public List<Reservation> SentReservations { get; } = new();

    public void Send(Reservation reservation)
    {
        SentReservations.Add(reservation);
    }
}


/* TrySave liefert ein festgelegtes Ergebnis (Standard: true).
Speicherversuche werden mitgezählt, damit Aufgabe 2 prüfen kann, dass keiner stattfand.
*/

public sealed class FakeReservationRepository : IReservationRepository
{
    private readonly bool _result;

    public FakeReservationRepository(bool result = true)
    {
        _result = result;
    }

    public int SaveCalls { get; private set; }

    public bool TrySave(Reservation reservation)
    {
        SaveCalls++;
        return _result;
    }
}