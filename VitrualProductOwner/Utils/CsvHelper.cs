using System.Text;

namespace BlazorApp1.Utils;

public static class CsvHelper
{
    public static IEnumerable<string> ParseCsvLine(string input)
    {
        if (input is null) yield break;
        var inQuotes = false;
        var sb = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < input.Length && input[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                yield return sb.ToString();
                sb.Clear();
            }
            else
            {
                sb.Append(ch);
            }
        }
        yield return sb.ToString();
    }
}
