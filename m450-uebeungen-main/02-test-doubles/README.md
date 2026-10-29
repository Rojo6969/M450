# 02 – Test Doubles

Lösung mit xUnit und Moq für alle fünf Grundaufgaben sowie die Gruppenreservierung.
Quelle: https://github.com/gbssg-russo/m450/blob/main/test-doubles/README.md

## Ausführen

Voraussetzung: .NET 10 SDK.

```powershell
dotnet test tests/TestDoublesExercise.Tests/TestDoublesExercise.Tests.csproj
```

Der vorgegebene Ablauf von Reserve bleibt unverändert. ReserveBatch und TrySaveBatch
ergänzen die Fortgeschrittenenaufgabe. Alle Doubles liegen ausschliesslich im Testprojekt.
Es werden keine Datenbank und kein echter Nachrichtenversand verwendet.

## Aufgabe 1 – Nur die Schnittstelle ist bekannt

Wir ersetzen ISeatAvailability durch einen eigenen Stub mit festen Werten pro
Veranstaltungscode. Die Schnittstelle definiert einen Aufruf mit einem string als
Veranstaltungscode und einem int als Ergebnis; sie garantiert allein weder
Datenquelle noch Aktualität, Fehlerbehandlung oder die Bedeutung negativer Werte.
Unsere fachliche Annahme: GetAvailableSeats liefert für den angegebenen Code
die aktuell frei reservierbare, nicht negative Anzahl an Sitzplätzen.
Der Stub berechnet keine echte Verfügbarkeit und weist nicht konfigurierte Codes
durch einen Fehler zurück. So bemerken wir auch versehentlich falsche Codes im Test.

## Aufgabe 2 – Grenzfälle und Isolation

Für 0 und -1 erwarten wir ArgumentOutOfRangeException, für einen leeren
Veranstaltungscode ArgumentException. Aufruflisten in den handgeschriebenen Doubles
machen sichtbar, dass keine Abhängigkeit angesprochen wurde. Strikte Moq-Mocks
weisen zusätzlich unerwartete Repository-Aufrufe zurück.

## Aufgabe 3 – Stub und Spy

Der Stub liefert vorgegebene Verfügbarkeitswerte; der Spy zeichnet die versendeten
Reservierungen in ihrer Aufrufreihenfolge auf, damit wir sie nachher prüfen können.

## Aufgabe 4 – Moq

Die Tests konfigurieren TrySave mit Setup(...).Returns(true/false), übergeben
mock.Object und prüfen mit Times.Once() die E-Mail-Adresse, den Veranstaltungscode
und die Platzanzahl. Bei Platzmangel wird mit Times.Never() geprüft, dass kein
Speicherversuch stattfindet. Rückgabewert und Bestätigungen werden ebenfalls geprüft.

## Aufgabe 5 – Wahl der Doubles

Für die Verfügbarkeit verwende ich den handgeschriebenen Stub, dessen Aufrufliste
auch die ausbleibende Abfrage zeigt. Das Repository erhält einen strikten Moq-Mock,
weil sich ein verbotener Speicheraufruf damit direkt prüfen lässt.
Der handgeschriebene Bestätigungs-Spy macht ausbleibende Nachrichten mit einer
leeren Liste leicht nachvollziehbar.

## Gruppenreservierung

Zuerst werden sämtliche Einträge validiert, danach werden die Plätze je
Veranstaltungscode addiert und die Verfügbarkeiten genau einmal abgefragt.
Eine long-Summe verhindert einen int-Überlauf bei sehr grossen Gruppen.
Das Repository erhält die gesamte Gruppe genau einmal; nur bei Erfolg werden
Bestätigungen in Eingabereihenfolge versendet. Atomare Speicherung ist laut
Aufgabenstellung eine Garantie des Repositorys und wird hier als Vertrag angenommen.

Wirft Send nach erfolgreichem Speichern eine Exception, bleiben die Reservierungen
gespeichert, obwohl keine oder erst einige Bestätigungen versendet wurden.
Die Exception wird weitergegeben; eine unkontrollierte Wiederholung könnte doppelte
Reservierungen oder Nachrichten erzeugen. Eine fachliche Lösung ist, die Buchung
als bestätigt mit noch ausstehendem Versand zu führen und fehlgeschlagene
Bestätigungen gezielt und ohne Duplikate erneut zu versenden, beispielsweise
über einen persistenten Versandauftrag (Outbox).
