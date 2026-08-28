using System.Xml.Serialization;
namespace M450.UnitTesting.App.Tests;
 
public class PriceCalculatorTests {
    [Fact]
    public void CalculateTotal_Scenario_ExpectedBehaviour()
    {
        // Arrange
        var calculator = new PriceCalculator();
        var unitPrice = 5;
        var quantity = 6;
        var discountPercentage = 10;
        var expectedTotal = 27;
 
        // Act
        var actualTotal = calculator.CalculateTotal(
            unitPrice,
            quantity,
            discountPercentage);
 
        // Assert
        Assert.Equal(expectedTotal, actualTotal);
    }
 
    [Fact]
    public void CalculateTotal_WithoutDiscount_ReturnsSubtotal()
    {
        // Arrange
        var calculator = new PriceCalculator();
        var unitPrice = 25.50m;
        var quantity = 3;
        var expectedTotal = 76.50m;
 
        // Act
        var actualTotal = calculator.CalculateTotal(
            unitPrice,
            quantity);
 
        // Assert
        Assert.Equal(expectedTotal, actualTotal);
    }
 
    [Fact]
    public void CalculateTotal_WithFullDiscount_ReturnsZero()
    {
    // Arrange
    var calculator = new PriceCalculator();
    var unitPrice = 10m;
    var quantity = 4;
    var discountPercentage = 100m;
    var expectedTotal = 0m;
 
    // Act
    var actualTotal = calculator.CalculateTotal(
        unitPrice,
        quantity,
        discountPercentage);
 
    // Assert
    Assert.Equal(expectedTotal, actualTotal);
    }
 
    [Fact]
    public void CalculateTotal_WithNegativeUnitPrice_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var calculator = new PriceCalculator();
        var unitPrice = -5m;
        var quantity = 3;
 
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => calculator.CalculateTotal(unitPrice, quantity));
    }
}