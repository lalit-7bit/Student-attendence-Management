using attendence.Services;
using Xunit;
namespace SmartAttend.Tests;
public sealed class AttendanceRulesTests
{
    [Fact] public void EmptyAttendanceHasZeroRate()=>Assert.Equal(0,AttendanceRules.Percentage(0,0,0,0));
    [Fact] public void PresentAndLateCountAsAttended()=>Assert.Equal(75,AttendanceRules.Percentage(2,1,1,0));
    [Fact] public void ExcusedIsExcludedFromDenominator()=>Assert.Equal(100,AttendanceRules.Percentage(2,0,0,5));
    [Fact] public void ThresholdComparisonIsStrict()=>Assert.False(AttendanceRules.IsBelowThreshold(75));
    [Fact] public void LowerRateIsBelowThreshold()=>Assert.True(AttendanceRules.IsBelowThreshold(74.99));
    [Fact] public void NegativeTotalsAreRejected()=>Assert.Throws<ArgumentOutOfRangeException>(()=>AttendanceRules.Percentage(-1,0,0,0));
    [Fact] public void CsvCellsQuoteCommasQuotesAndNewlines(){Assert.Equal("\"A, B\"",CsvExportFormatter.EscapeCell("A, B"));Assert.Equal("\"He said \"\"yes\"\"\"",CsvExportFormatter.EscapeCell("He said \"yes\""));Assert.Equal("\"line1\r\nline2\"",CsvExportFormatter.EscapeCell("line1\r\nline2"));}
    [Theory] [InlineData("=1+1")] [InlineData("+cmd")] [InlineData("-1+2")] [InlineData("@SUM(A:A)")] public void CsvCellsNeutralizeSpreadsheetFormulas(string value)=>Assert.Equal("\"'"+value+"\"",CsvExportFormatter.EscapeCell(value));
}
