# Test Doubles mit xUnit und Moq

Diese Lösung testet einen Reservierungsservice ohne Datenbank, produktive
Sitzplatzabfrage oder echten Nachrichtenversand. Dafür kommen ein selbst
geschriebener Stub, ein selbst geschriebener Spy und Moq zum Einsatz.

Umgesetzt sind alle fünf Grundaufgaben sowie die freiwillige
Fortgeschrittenenaufgabe zur Gruppenreservierung.

## Voraussetzungen

- .NET 10 SDK
- Eine Kommandozeile im Verzeichnis `02-test-doubles`

Die installierte SDK-Version lässt sich so prüfen:

```shell
dotnet --version
```

## Tests ausführen

Vom Verzeichnis `02-test-doubles` aus:

```shell
dotnet test tests/TestDoublesExercise.Tests/TestDoublesExercise.Tests.csproj
```

Alternativ aus dem Wurzelverzeichnis dieses Git-Repositories:

```shell
dotnet test m450-uebeungen-main/02-test-doubles/tests/TestDoublesExercise.Tests/TestDoublesExercise.Tests.csproj
```

## Projektstruktur

```text
02-test-doubles/
├── src/TestDoublesExercise/
│   └── ReservationService.cs
└── tests/TestDoublesExercise.Tests/
    ├── ReservationServiceTests.cs
    ├── ReservationBatchTests.cs
    └── TestDoubles.cs
```

- `ReservationService.cs` enthält den Service, das Datenmodell und die drei
  Abhängigkeitsschnittstellen.
- `TestDoubles.cs` enthält die handgeschriebenen Test Doubles.
- `ReservationServiceTests.cs` prüft einzelne Reservierungen und ungültige Eingaben.
- `ReservationBatchTests.cs` prüft die Gruppenreservierung.

## Verwendete Test Doubles

### Stub für die Sitzplatzverfügbarkeit

`SeatAvailabilityStub` liefert pro Veranstaltungscode einen im Test festgelegten
Wert. Die Schnittstelle `ISeatAvailability` sagt bereits aus, dass ein
Veranstaltungscode übergeben und eine ganzzahlige Verfügbarkeit zurückgegeben wird.

Fachliche Annahme: `GetAvailableSeats` liefert die aktuell frei reservierbare,
nicht negative Anzahl Plätze für den angegebenen Veranstaltungscode. Der Stub
berechnet keine echte Verfügbarkeit. Ein nicht konfigurierter Code löst bewusst
einen Fehler aus, damit ein fehlerhafter Testaufbau sichtbar wird.

### Spy für Bestätigungen

`ConfirmationSenderSpy` zeichnet jede übergebene Reservierung in Aufrufreihenfolge
auf. So können die Tests nach der Ausführung Anzahl, Inhalt und Reihenfolge der
Bestätigungen prüfen.

Der Unterschied: Ein Stub liefert vorbereitete Daten an den getesteten Code; ein
Spy merkt sich Aufrufe, damit die Tests sie anschließend untersuchen können.

### Moq für das Repository

Die Repository-Mocks legen mit `Setup(...).Returns(...)` fest, ob das Speichern
gelingt. `Verify`, `Times.Once()` und `Times.Never()` prüfen, ob die richtige
Reservierung gespeichert beziehungsweise ein unerlaubter Speicherversuch
unterlassen wurde. Strikte Mocks machen zusätzliche, nicht erwartete Aufrufe sofort
sichtbar.

## Abdeckung der Grundaufgaben

| Aufgabe | Abgedecktes Verhalten |
| --- | --- |
| 1 | Mehr, weniger und genau gleich viele verfügbare Plätze; handgeschriebener Stub |
| 2 | Platzanzahl `0` und `-1`, fehlender Veranstaltungscode, keine Aufrufe bei ungültigen Eingaben |
| 3 | Bestätigung nach erfolgreichem Speichern, keine Bestätigung bei Platzmangel oder Speicherfehler |
| 4 | Erfolgreiches und fehlgeschlagenes Speichern mit Moq, genaue Argument- und Aufrufprüfung |
| 5 | E-Mail-Adresse nur aus Leerzeichen, Exception sowie kein Speichern und kein Versand |

Für Aufgabe 5 wird der handgeschriebene Stub verwendet, weil seine Aufrufliste eine
unerlaubte Verfügbarkeitsabfrage direkt sichtbar macht. Das Repository ist ein
strikter Moq-Mock, weil sich ein verbotener Speicherversuch damit präzise prüfen
lässt. Der handgeschriebene Spy zeigt einen unerlaubten Bestätigungsversand durch
seine ansonsten leere Liste.

## Gruppenreservierung

`ReserveBatch` erfüllt folgende Regeln:

1. Die vollständige Eingabe wird validiert, bevor eine Abhängigkeit aufgerufen wird.
2. Platzanfragen werden pro Veranstaltungscode addiert.
3. Jede unterschiedliche Veranstaltung wird genau einmal abgefragt.
4. Bei Platzmangel wird weder gespeichert noch eine Bestätigung versendet.
5. Alle Reservierungen werden gemeinsam und atomar an `TrySaveBatch` übergeben.
6. Bestätigungen werden erst nach erfolgreichem Speichern in Eingabereihenfolge
   versendet.

Die Summe wird als `long` berechnet, damit auch mehrere sehr große Platzanfragen
keinen `int`-Überlauf verursachen. Ein eigener Test prüft zwei Reservierungen für
dieselbe Veranstaltung, die einzeln passen würden, gemeinsam aber die Kapazität
überschreiten.

## Fehler beim Bestätigungsversand

Wirft `Send` nach erfolgreichem Speichern eine Exception, bleiben die
Reservierungen gespeichert, obwohl keine oder nur einige Bestätigungen versendet
wurden. Eine unkontrollierte Wiederholung könnte doppelte Buchungen oder Nachrichten
erzeugen.

Eine mögliche fachliche Lösung ist, den Versandstatus getrennt von der Buchung zu
speichern und fehlgeschlagene Bestätigungen über einen persistenten Versandauftrag
(Outbox) gezielt und idempotent zu wiederholen.
