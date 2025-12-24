using System.Collections;
using System.Globalization;
using Shared;

namespace Aircraft.Core.Models;

public readonly struct RowRange : IEnumerable<int>, IEquatable<RowRange>
{
    public int Start { get; }
    public int End { get; }
    public RowRange(int start, int end)
    {
        Ensure.GreaterThanZero(start);
        Ensure.GreaterThanZero(end);
        Ensure.LessThanOrEqualTo(start, end);
        Start = start;
        End = end;
    }
    public static RowRange Parse(string s)
    {
        if (s.Contains('-', StringComparison.OrdinalIgnoreCase))
        {
            var parts = s.Split('-', 2, StringSplitOptions.TrimEntries);
            return new RowRange(int.Parse(parts[0], CultureInfo.InvariantCulture), int.Parse(parts[1], CultureInfo.InvariantCulture));
        }
        var single = int.Parse(s.Trim(), CultureInfo.InvariantCulture);
        return new RowRange(single, single);
    }
    public IEnumerator<int> GetEnumerator()
    {
        for (var i = Start; i <= End; i++)
        {
            yield return i;
        }
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public override bool Equals(object? obj)
    {
        if (obj is not RowRange other)
        {
            return false;
        }
        return Start == other.Start && End == other.End;
    }
    public bool Equals(RowRange other) => Start == other.Start && End == other.End;
    public static bool operator ==(RowRange left, RowRange right) => left.Equals(right);
    public static bool operator !=(RowRange left, RowRange right) => !(left == right);
    public override int GetHashCode() => HashCode.Combine(Start, End);
}
