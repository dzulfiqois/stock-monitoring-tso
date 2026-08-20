using FluentAssertions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Domain.Services;

namespace StockMonitorTso.UnitTests;

public class StockCalculatorTests
{
    [Theory]
    [InlineData(1000, 100, 10)]
    [InlineData(1110, 34, 32.647058823529412)]
    [InlineData(507, 95, 5.336842105263158)]
    public void CoverageDays_StokDibagiDot(decimal stok, decimal dot, decimal expected)
    {
        StockCalculator.CoverageDays(stok, dot).Should().BeApproximately(expected, 0.000001m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CoverageDays_DotNonPositif_ReturnsNull(decimal dot)
    {
        StockCalculator.CoverageDays(100, dot).Should().BeNull();
    }

    [Fact]
    public void ExhaustDate_TanggalAwalPlusCd()
    {
        var tanggal = new DateTime(2026, 8, 5);
        StockCalculator.ExhaustDate(tanggal, 10m).Should().Be(new DateTime(2026, 8, 15));
    }

    [Fact]
    public void ExhaustDate_CdNull_ReturnsNull()
    {
        StockCalculator.ExhaustDate(new DateTime(2026, 8, 5), null).Should().BeNull();
    }

    [Theory]
    [InlineData(2.9, Status.Kritis)]
    [InlineData(3, Status.Warning)]
    [InlineData(6.9, Status.Warning)]
    [InlineData(7, Status.Aman)]
    [InlineData(100, Status.Aman)]
    public void StatusFor_Thresholds(decimal cd, Status expected)
    {
        StockCalculator.StatusFor(cd).Should().Be(expected);
    }

    [Fact]
    public void StatusFor_NullCd_ReturnsNull()
    {
        StockCalculator.StatusFor(null).Should().BeNull();
    }

    [Fact]
    public void CoverageDaysAfterRencana_SisaStokDitambahPasokan()
    {
        // Stok 1000, DOT 100/hari, tanggal awal 1 Jan, pasokan 500 tiba 6 Jan.
        // Sisa saat ETA = 1000 - 100*5 = 500; +500 = 1000; /100 = 10.
        StockCalculator.CoverageDaysAfterRencana(
                stok: 1000,
                dot: 100,
                tanggalStokAwal: new DateTime(2026, 1, 1),
                nextSupply: 500,
                eta: new DateTime(2026, 1, 6))
            .Should()
            .Be(10m);
    }

    [Fact]
    public void CoverageDaysAfterRencana_DotNol_ReturnsNull()
    {
        StockCalculator.CoverageDaysAfterRencana(
                stok: 1000,
                dot: 0,
                tanggalStokAwal: new DateTime(2026, 1, 1),
                nextSupply: 500,
                eta: new DateTime(2026, 1, 6))
            .Should()
            .BeNull();
    }

    [Fact]
    public void ExhaustDateAfterRencana_EtaPlusCd()
    {
        StockCalculator.ExhaustDateAfterRencana(new DateTime(2026, 1, 6), 10m)
            .Should()
            .Be(new DateTime(2026, 1, 16));
    }

    [Theory]
    [InlineData(Produk.Lpg5_5Kg, 1000, 5.5)]
    [InlineData(Produk.Lpg12Kg, 507, 6.084)]
    [InlineData(Produk.Lpg50Kg, 125, 6.25)]
    public void MetricTon_Lpg(Produk produk, decimal stok, decimal expected)
    {
        StockCalculator.MetricTon(produk, stok).Should().Be(expected);
    }

    [Fact]
    public void MetricTon_MinyakTanah_ReturnsNull()
    {
        StockCalculator.MetricTon(Produk.MinyakTanah, 1m).Should().BeNull();
    }
}
