
namespace M450.Bankkonto;
 
/// <summary>
/// Repräsentiert ein Bankkonto mit einfacher, täglich berechneter Verzinsung.
/// Die Zinssätze werden als Prozentwerte angegeben (zum Beispiel 1.5 für 1,5 %).
/// Ab einem Guthaben von CHF 10'000 gilt ein gestaffelter Zuschlag auf den
/// Aktivzins, ab CHF 50'000 zusätzlich abhängig vom <see cref="KontoStatus"/>.
/// </summary>
public sealed class Bankkonto
{
    private const int BankjahrInTagen = 360;
 
    // Untere Grenzen der Guthabenstufen gemäss Zinstabelle.
    private const decimal GrenzeMittleresGuthaben = 10_000m;
    private const decimal GrenzeHohesGuthaben = 50_000m;
 
    // Zuschläge in Prozentpunkten auf den Aktivzins.
    private const decimal ZuschlagMittleresGuthaben = 0.5m;
    private const decimal ZuschlagHohesGuthabenStandard = 0.75m;
    private const decimal ZuschlagHohesGuthabenVip = 1.5m;
 
    private static long _letzteKontoNummer = 100_000;
 
    private KontoStatus _status;
 
    /// <summary>
    /// Erstellt ein neues Konto mit einer automatisch vergebenen Kontonummer.
    /// </summary>
    /// <param name="aktivZins">Jährlicher Habenzins in Prozent.</param>
    /// <param name="passivZins">Jährlicher Sollzins in Prozent.</param>
    /// <param name="startGuthaben">Anfänglicher Kontostand.</param>
    /// <param name="status">Kundenstatus des Kontos.</param>
    public Bankkonto(
        decimal aktivZins,
        decimal passivZins,
        decimal startGuthaben = 0m,
        KontoStatus status = KontoStatus.Standard)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(aktivZins);
        ArgumentOutOfRangeException.ThrowIfNegative(passivZins);
        StelleGueltigenStatusSicher(status);
 
        KontoNummer = Interlocked.Increment(ref _letzteKontoNummer);
        AktivZins = aktivZins;
        PassivZins = passivZins;
        Guthaben = startGuthaben;
        _status = status;
    }
 
    /// <summary>
    /// Eindeutige, nach dem Erstellen nicht mehr veränderbare Kontonummer.
    /// </summary>
    public long KontoNummer { get; }
 
    /// <summary>
    /// Aktueller Kontostand. Er kann nur durch Kontooperationen verändert werden.
    /// </summary>
    public decimal Guthaben { get; private set; }
 
    /// <summary>
    /// Jährlicher Habenzins in Prozent.
    /// </summary>
    public decimal AktivZins { get; }
 
    /// <summary>
    /// Jährlicher Sollzins in Prozent.
    /// </summary>
    public decimal PassivZins { get; }
 
    /// <summary>
    /// Kundenstatus des Kontos. Er kann nachträglich geändert werden, weil ein
    /// Kunde vom Standard- zum VIP-Kunden werden kann und umgekehrt.
    /// </summary>
    public KontoStatus Status
    {
        get => _status;
        set
        {
            StelleGueltigenStatusSicher(value);
            _status = value;
        }
    }
 
    /// <summary>
    /// Seit dem letzten Abschluss aufgelaufener, noch nicht verbuchter Zins.
    /// </summary>
    public decimal AufgelaufenerZins { get; private set; }
 
    /// <summary>
    /// Zinssatz in Prozent, der aktuell auf das Guthaben angewendet wird.
    /// Er ergibt sich aus Guthaben und Status gemäss Zinstabelle.
    /// </summary>
    public decimal AktuellerZinsSatz => ErmittleZinsSatz();
 
    /// <summary>
    /// Zahlt einen positiven Betrag auf das Konto ein.
    /// </summary>
    public void ZahleEin(decimal betrag)
    {
        StellePositivenBetragSicher(betrag);
        Guthaben += betrag;
    }
 
    /// <summary>
    /// Bezieht einen positiven Betrag. Eine Überziehung ist zulässig.
    /// </summary>
    public void Beziehe(decimal betrag)
    {
        StellePositivenBetragSicher(betrag);
        Guthaben -= betrag;
    }
 
    /// <summary>
    /// Überweist einen positiven Betrag auf ein anderes Konto.
    /// </summary>
    public void Transferiere(Bankkonto konto, decimal betrag)
    {
        ArgumentNullException.ThrowIfNull(konto);
        StellePositivenBetragSicher(betrag);
 
        if (ReferenceEquals(this, konto))
        {
            throw new ArgumentException(
                "Quell- und Zielkonto müssen verschieden sein.",
                nameof(konto));
        }
 
        Beziehe(betrag);
        konto.ZahleEin(betrag);
    }
 
    /// <summary>
    /// Berechnet den Zins für die angegebene Anzahl Tage nach der 360-Tage-Methode.
    /// Der Zins wird separat gesammelt und noch nicht dem Guthaben zugeschlagen.
    /// </summary>
    public void SchreibeZinsGut(int anzTage)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(anzTage);
 
        var zinsSatz = ErmittleZinsSatz();
        AufgelaufenerZins += Guthaben * zinsSatz * anzTage
            / (100m * BankjahrInTagen);
    }
 
    /// <summary>
    /// Verbucht den aufgelaufenen Zins und beginnt eine neue Zinsperiode.
    /// </summary>
    public void SchliesseKontoAb()
    {
        Guthaben += AufgelaufenerZins;
        AufgelaufenerZins = 0m;
    }
 
    // Bildet die Zinstabelle ab. Die Reihenfolge der Abfragen entspricht den
    // Äquivalenzklassen von unten nach oben, dadurch muss jede Stufe nur ihre
    // obere Grenze prüfen.
    private decimal ErmittleZinsSatz()
    {
        if (Guthaben < 0m)
        {
            return PassivZins;
        }
 
        if (Guthaben < GrenzeMittleresGuthaben)
        {
            return AktivZins;
        }
 
        if (Guthaben < GrenzeHohesGuthaben)
        {
            return AktivZins + ZuschlagMittleresGuthaben;
        }
 
        // Annahme: Die Spezifikation endet bei CHF 100'000. Guthaben darüber
        // werden gleich behandelt wie die oberste spezifizierte Stufe.
        return Status == KontoStatus.Vip
            ? AktivZins + ZuschlagHohesGuthabenVip
            : AktivZins + ZuschlagHohesGuthabenStandard;
    }
 
    private static void StelleGueltigenStatusSicher(KontoStatus status)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Der Status muss Standard oder Vip sein.");
        }
    }
 
    private static void StellePositivenBetragSicher(decimal betrag)
    {
        if (betrag <= 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(betrag),
                "Der Betrag muss grösser als null sein.");
        }
    }
}
 
