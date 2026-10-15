namespace attendence.Services;
public static class CsvExportFormatter
{
    public static string EscapeCell(string value)
    {
        var safe=value.TrimStart(' ','\t','\r','\n');
        if(safe.Length>0&&safe[0] is '=' or '+' or '-' or '@' or '\t' or '\r')value="'"+value;
        return "\""+value.Replace("\"","\"\"")+"\"";
    }
}
