using System.Collections;
using System.Globalization;

namespace Aircraft.Core.Models;

public readonly record struct RowRange : IEnumerable<int>
{
    public int Start { get; }
    public int End { get; }

    public RowRange(int start, int end)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(end, 1);

        if (start > end)
        {
            throw new ArgumentException("Start row must be less than or equal to end row.");
        }

        Start = start;
        End = end;
    }

    public static RowRange Parse(string s)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(s);

        var parts = s.Split('-', StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            1 => Single(parts[0]),
            2 => Range(parts[0], parts[1]),
            _ => throw new FormatException("Row range must be a single row or a start-end range.")
        };

        static RowRange Single(string value)
        {
            var row = int.Parse(value, CultureInfo.InvariantCulture);
            return new RowRange(row, row);
        }

        static RowRange Range(string start, string end) => new RowRange(
                int.Parse(start, CultureInfo.InvariantCulture),
                int.Parse(end, CultureInfo.InvariantCulture));
    }

    public IEnumerator<int> GetEnumerator()
    {
        for (var i = Start; i <= End; i++)
        {
            yield return i;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
