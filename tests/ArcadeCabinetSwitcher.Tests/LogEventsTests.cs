using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace ArcadeCabinetSwitcher.Tests;

[TestClass]
public class LogEventsTests
{
    [TestMethod]
    public void LogEvents_AllEventIds_AreUnique()
    {
        var fields = typeof(LogEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(EventId))
            .Select(f => (EventId)f.GetValue(null)!)
            .ToList();

        var ids = fields.Select(e => e.Id).ToList();
        var duplicates = ids.GroupBy(id => id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

        Assert.AreEqual(0, duplicates.Count,
            $"Duplicate EventId values found: {string.Join(", ", duplicates)}");
    }

    [TestMethod]
    public void LogEvents_AllEventIds_HaveNames()
    {
        var fields = typeof(LogEvents)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(EventId))
            .Select(f => (EventId)f.GetValue(null)!)
            .ToList();

        var unnamed = fields.Where(e => string.IsNullOrEmpty(e.Name)).ToList();

        Assert.AreEqual(0, unnamed.Count,
            $"{unnamed.Count} EventId(s) have no name.");
    }
}
