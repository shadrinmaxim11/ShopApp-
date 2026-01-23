using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShopApp.Services;

namespace ShopApp.Tests;

[TestClass]
public sealed class ReportRowTests
{
    // Тест 1: проверка формата даты
    [TestMethod]
    public void DateFormatted_ReturnsExpectedFormat()
    {
        var row = new ReportRow { Date = new DateTime(2025, 12, 17) };

        Assert.AreEqual("17.12.2025", row.DateFormatted);
    }

    // Тест 2: проверка форматирования суммы с двумя знаками после запятой
    [TestMethod]
    public void SumFormatted_FormatsWithTwoDecimals()
    {
        var row = new ReportRow { Sum = 1234.5m };

        // В приложении используется текущая культура (русская),
        // поэтому ожидаем запятую как разделитель: "1234,50"
        Assert.AreEqual("1234,50", row.SumFormatted);
    }

    // Тест 3: проверка значения по умолчанию для PickupPoint
    [TestMethod]
    public void PickupPoint_Default_IsEmptyString()
    {
        var row = new ReportRow();

        Assert.AreEqual(string.Empty, row.PickupPoint);
    }
}
