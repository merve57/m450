

## Aufgabe 1

ReservationService bekommt seine Abhängigkeiten über den Konstruktor als Schnittstellen und kennt keine konkrete Klasse. Deshalb können wir im Test einen eigenen ISeatAvailability-Stub übergeben und den Service schon jetzt testen.

Was die Schnittstelle bereits festlegt: Es gibt eine Methode GetAvailableSeats, die einen Veranstaltungscode string bekommt und eine ganze Zahl int liefert. Über Fehlerfälle, unbekannte Codes oder negative Werte sagt sie nichts.


## Aufgabe 2

Das Repository-Double zählt Speicherversuche mit, und der Spy zeichnet versendete Bestätigungen auf. Nach dem Test prüfen wir, dass beide leer sind. Dafür muss der Produktionscode nicht verändert werden.

## Aufgabe 3

Der Stub liefert dem Service vorgegebene Daten, während der Spy aufzeichnet, was der Service mit ihm macht, damit der Test es danach prüfen kann.

## Aufgabe 5 – Handgeschrieben oder Moq?

Für Verfügbarkeit und Sender verwenden wir unsere handgeschriebenen Doubles, weil sie schon existieren und die Prüfung „Spy-Liste ist leer“ ohne Setup gut lesbar ist. Für das Repository nehmen wir Moq, weil Verify(..., Times.Never()) ohne eigene Klasse genau ausdrückt, dass kein Speicherversuch stattfinden darf.