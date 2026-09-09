using ClientXMLApp.Services;
using System.Globalization;

namespace ClientXMLApp.Tests
{
    public class CalendarDateTests
    {
        [Theory]
        [InlineData("2001-09-02T01:00:00+05:00")]
        [InlineData("2001-09-02T23:30:00Z")]
        [InlineData("2001-09-02 13:45")]
        [InlineData("01.09.2001")]
        [InlineData("9/1/2001")]
        [InlineData("Sep 1 2001")]
        [InlineData("2001-9-1")]
        [InlineData("2001-09-02T00:00:00")]
        public void Refuses_anything_but_a_plain_calendar_date(string value)
        {
            Assert.False(CalendarDate.TryParse(value, out _));
        }

        [Theory]
        [InlineData("2001-09-02")]
        [InlineData("  2001-09-02  ")]
        public void Reads_a_plain_calendar_date(string value)
        {
            Assert.True(CalendarDate.TryParse(value, out var date));
            Assert.Equal(new DateTime(2001, 9, 2), date);
        }

        [Theory]
        [InlineData("tr-TR")]
        [InlineData("th-TH")]
        [InlineData("de-DE")]
        public void Reads_the_same_date_under_any_culture(string culture)
        {
            var previous = Thread.CurrentThread.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

            try
            {
                Assert.True(CalendarDate.TryParse("2001-09-02", out var date));
                Assert.Equal(new DateTime(2001, 9, 2), date);
                Assert.False(CalendarDate.TryParse("01.09.2001", out _));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }
    }
}
