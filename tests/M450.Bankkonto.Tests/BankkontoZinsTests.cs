using Konto = M450.Bankkonto.Bankkonto;

namespace M450.Bankkonto.Tests;

/// <summary>
/// Tests zur Zinsstaffelung nach Guthaben und Status (Aufgabe 5-2).
/// Die Testfälle sind aus der Äquivalenzklassenbildung und der
/// Grenzwertanalyse abgeleitet. Genauigkeit der Grenzwerte: 0.01.
/// </summary>
public sealed class BankkontoZinsTests
{
    private const decimal AktivZins = 2.00m;
    private const decimal PassivZins = 5.00m;
    private const int GanzesJahr = 360;

    // Hinweis: decimal ist als Attributargument nicht erlaubt, deshalb werden
    // die Werte als double übergeben und im Test nach decimal konvertiert.
    [Theory]
    // AeK1: Guthaben negativ -> Passivzins, Status ohne Wirkung
    [InlineData(-1_000.00, KontoStatus.Standard, 5.00)]
    [InlineData(-1_000.00, KontoStatus.Vip, 5.00)]
    // GW: unterer Rand von AeK2
    [InlineData(-0.01, KontoStatus.Standard, 5.00)]
    [InlineData(0.00, KontoStatus.Standard, 2.00)]
    [InlineData(0.01, KontoStatus.Standard, 2.00)]
    // AeK2: 0 <= g < 10'000 -> Aktivzins, Status ohne Wirkung
    [InlineData(5_000.00, KontoStatus.Standard, 2.00)]
    [InlineData(5_000.00, KontoStatus.Vip, 2.00)]
    // GW: Uebergang AeK2 -> AeK3
    [InlineData(9_999.99, KontoStatus.Standard, 2.00)]
    [InlineData(10_000.00, KontoStatus.Standard, 2.50)]
    [InlineData(10_000.01, KontoStatus.Standard, 2.50)]
    // AeK3: 10'000 <= g < 50'000 -> Aktivzins + 0.5, Status ohne Wirkung
    [InlineData(25_000.00, KontoStatus.Standard, 2.50)]
    [InlineData(25_000.00, KontoStatus.Vip, 2.50)]
    // GW: Uebergang AeK3 -> AeK4/AeK5, hier wird der Status relevant
    [InlineData(49_999.99, KontoStatus.Standard, 2.50)]
    [InlineData(49_999.99, KontoStatus.Vip, 2.50)]
    [InlineData(50_000.00, KontoStatus.Standard, 2.75)]
    [InlineData(50_000.00, KontoStatus.Vip, 3.50)]
    [InlineData(50_000.01, KontoStatus.Standard, 2.75)]
    [InlineData(50_000.01, KontoStatus.Vip, 3.50)]
    // AeK4/AeK5: 50'000 <= g < 100'000
    [InlineData(75_000.00, KontoStatus.Standard, 2.75)]
    [InlineData(75_000.00, KontoStatus.Vip, 3.50)]
    // GW: oberer Rand der Spezifikation
    [InlineData(99_999.99, KontoStatus.Standard, 2.75)]
    [InlineData(99_999.99, KontoStatus.Vip, 3.50)]
    public void AktuellerZinsSatz_ProAequivalenzklasse_LiefertKorrektenSatz(
        double guthaben,
        KontoStatus status,
        double erwarteterZinsSatz)
    {
        // Arrange
        var konto = NeuesKonto(
            startGuthaben: (decimal)guthaben,
            status: status);

        // Act
        var zinsSatz = konto.AktuellerZinsSatz;

        // Assert
        Assert.Equal((decimal)erwarteterZinsSatz, zinsSatz);
    }

    // Spezialfall: Die Zinstabelle endet bei 100'000. Dieser Test haelt die
    // getroffene Annahme fest, dass hoehere Guthaben wie die oberste Stufe
    // behandelt werden. Faellt der Entscheid der Fachabteilung anders aus,
    // muss dieser Test angepasst werden.
    [Theory]
    [InlineData(100_000.00, KontoStatus.Standard, 2.75)]
    [InlineData(100_000.00, KontoStatus.Vip, 3.50)]
    [InlineData(1_000_000.00, KontoStatus.Vip, 3.50)]
    public void AktuellerZinsSatz_UeberSpezifiziertemBereich_BehaeltObersteStufe(
        double guthaben,
        KontoStatus status,
        double erwarteterZinsSatz)
    {
        // Arrange
        var konto = NeuesKonto(
            startGuthaben: (decimal)guthaben,
            status: status);

        // Act
        var zinsSatz = konto.AktuellerZinsSatz;

        // Assert
        Assert.Equal((decimal)erwarteterZinsSatz, zinsSatz);
    }

    [Fact]
    public void SchreibeZinsGut_MittleresGuthaben_VerwendetZuschlagVonEinemHalbenProzent()
    {
        // Arrange
        var konto = NeuesKonto(startGuthaben: 10_000m);

        // Act
        konto.SchreibeZinsGut(GanzesJahr);

        // Assert
        // 10'000 * 2.5 % * 360/360 = 250
        Assert.Equal(250m, konto.AufgelaufenerZins);
    }

    [Fact]
    public void SchreibeZinsGut_HohesGuthabenVip_VerwendetZuschlagVonEinemHalbenUndEinemProzent()
    {
        // Arrange
        var konto = NeuesKonto(
            startGuthaben: 50_000m,
            status: KontoStatus.Vip);

        // Act
        konto.SchreibeZinsGut(GanzesJahr);

        // Assert
        // 50'000 * 3.5 % * 360/360 = 1750
        Assert.Equal(1_750m, konto.AufgelaufenerZins);
    }

    [Fact]
    public void SchreibeZinsGut_HohesGuthabenStandard_VerwendetKleinerenZuschlag()
    {
        // Arrange
        var konto = NeuesKonto(
            startGuthaben: 50_000m,
            status: KontoStatus.Standard);

        // Act
        konto.SchreibeZinsGut(GanzesJahr);

        // Assert
        // 50'000 * 2.75 % * 360/360 = 1375
        Assert.Equal(1_375m, konto.AufgelaufenerZins);
    }

    [Fact]
    public void SchreibeZinsGut_BeiNegativemGuthabenUndVip_VerwendetPassivZinsOhneZuschlag()
    {
        // Arrange
        var konto = NeuesKonto(
            startGuthaben: -1_000m,
            status: KontoStatus.Vip);

        // Act
        konto.SchreibeZinsGut(GanzesJahr);

        // Assert
        // -1'000 * 5 % * 360/360 = -50
        Assert.Equal(-50m, konto.AufgelaufenerZins);
    }

    [Fact]
    public void SchreibeZinsGut_MitNullTagen_ErzeugtKeinenZins()
    {
        // Arrange
        var konto = NeuesKonto(startGuthaben: 50_000m, status: KontoStatus.Vip);

        // Act
        konto.SchreibeZinsGut(0);

        // Assert
        Assert.Equal(0m, konto.AufgelaufenerZins);
    }

    [Fact]
    public void SchreibeZinsGut_MitNegativenTagen_WirftException()
    {
        // Arrange
        var konto = NeuesKonto(startGuthaben: 50_000m);

        // Act
        var exception = Record.Exception(() => konto.SchreibeZinsGut(-1));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    // Der Zinssatz haengt vom aktuellen Guthaben ab, nicht vom Startguthaben.
    [Fact]
    public void SchreibeZinsGut_NachEinzahlungUeberDieGrenze_VerwendetHoeherenSatz()
    {
        // Arrange
        var konto = NeuesKonto(startGuthaben: 9_999.99m);

        // Act
        konto.ZahleEin(0.01m);

        // Assert
        Assert.Equal(2.50m, konto.AktuellerZinsSatz);
    }

    [Fact]
    public void Status_NachAenderungAufVip_VerwendetHoeherenSatz()
    {
        // Arrange
        var konto = NeuesKonto(
            startGuthaben: 50_000m,
            status: KontoStatus.Standard);

        // Act
        konto.Status = KontoStatus.Vip;

        // Assert
        Assert.Equal(3.50m, konto.AktuellerZinsSatz);
    }

    [Fact]
    public void Konstruktor_MitUngueltigemStatus_WirftException()
    {
        // Arrange
        var ungueltigerStatus = (KontoStatus)99;

        // Act
        var exception = Record.Exception(
            () => new Konto(AktivZins, PassivZins, 0m, ungueltigerStatus));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
    }

    [Fact]
    public void Status_MitUngueltigemWert_WirftExceptionUndAendertNichts()
    {
        // Arrange
        var konto = NeuesKonto(status: KontoStatus.Standard);

        // Act
        var exception = Record.Exception(() => konto.Status = (KontoStatus)99);

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
        Assert.Equal(KontoStatus.Standard, konto.Status);
    }

    [Fact]
    public void Konstruktor_OhneStatusAngabe_ErzeugtStandardKonto()
    {
        // Arrange

        // Act
        var konto = new Konto(AktivZins, PassivZins);

        // Assert
        Assert.Equal(KontoStatus.Standard, konto.Status);
    }

    private static Konto NeuesKonto(
        decimal aktivZins = AktivZins,
        decimal passivZins = PassivZins,
        decimal startGuthaben = 0m,
        KontoStatus status = KontoStatus.Standard)
    {
        return new Konto(aktivZins, passivZins, startGuthaben, status);
    }
}