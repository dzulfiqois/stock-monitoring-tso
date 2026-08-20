using FluentAssertions;
using StockMonitorTso.Domain.Entities;
using StockMonitorTso.Infrastructure.Seed;

namespace StockMonitorTso.UnitTests;

public class AgenMockSeederTests
{
    [Theory]
    [InlineData(1000, 2)]
    [InlineData(500, 3)]
    [InlineData(1803, 3)]
    public void SplitEqual_IntegerTotal_PartSumEqualsTotal(long total, int n)
    {
        var splits = AgenMockSeeder.SplitEqual(total, n);

        splits.Should().HaveCount(n);
        splits.Should().OnlyContain(x => x == Math.Truncate(x), "tabung harus bulat");
        splits.Sum().Should().Be(total);
    }

    [Theory]
    [InlineData(0.25, 3)]
    [InlineData(0.5, 2)]
    [InlineData(1.5, 3)]
    public void SplitEqual_DecimalTotal_PartSumEqualsTotal(decimal total, int n)
    {
        var splits = AgenMockSeeder.SplitEqual(total, n);

        splits.Should().HaveCount(n);
        splits.Sum().Should().Be(total);
    }

    [Fact]
    public void SplitEqual_NonPositifTotal_ReturnsZeros()
    {
        var splits = AgenMockSeeder.SplitEqual(0, 3);

        splits.Should().HaveCount(3);
        splits.Should().OnlyContain(x => x == 0m);
    }

    [Theory]
    [InlineData(Wilayah.Maluku, 3)]
    [InlineData(Wilayah.PapuaBarat, 2)]
    [InlineData(Wilayah.PapuaTengah, 3)]
    public void AgenCount_AlwaysTwoOrThree(Wilayah wilayah, int expected)
    {
        AgenMockSeeder.AgenCount(wilayah).Should().Be(expected);
    }

    [Fact]
    public void AgenCount_EveryWilayah_BetweenTwoAndThree()
    {
        foreach (var wilayah in WilayahInfo.All)
        {
            AgenMockSeeder.AgenCount(wilayah).Should().BeInRange(2, 3, $"wilayah {wilayah} harus 2-3 agen");
        }
    }

    [Fact]
    public void AgenName_ContainsUrutanAndWilayah()
    {
        AgenMockSeeder.AgenName(Wilayah.Maluku, 1).Should().Be("Agen 1 Maluku");
        AgenMockSeeder.AgenName(Wilayah.Papua, 2).Should().Be("Agen 2 Papua");
    }
}
