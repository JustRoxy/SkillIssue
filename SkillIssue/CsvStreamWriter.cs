namespace SkillIssue;

public class CsvStreamWriter<T>
{
    public string Header { get; init; }
    private readonly List<Func<T, object?>> _parameters;
    private readonly string _separator;
    public CsvStreamWriter(string header, List<Func<T, object?>> parameters, string separator = ",")
    {
        Header = header;
        _parameters = parameters;
        _separator = separator;
    }

    public async Task StreamToResponse(IAsyncEnumerable<T> stream, HttpResponse response, string filename, bool forceFlush = false, CancellationToken cancellationToken = default)
    {
        response.StatusCode = 200;
        response.ContentType = "text/csv";
        response.Headers.ContentDisposition = $"attachment; filename={filename}";
        response.Headers.ContentEncoding = "gzip";

        await using var gzip = new System.IO.Compression.GZipStream(
            response.Body,
            System.IO.Compression.CompressionLevel.Fastest, leaveOpen: true);

        await using var streamWriter = new StreamWriter(gzip, leaveOpen: true);

        await streamWriter.WriteLineAsync(Header);

        await foreach (var value in stream.WithCancellation(cancellationToken))
        {
            var parameterValues = _parameters.Select(parameterFunction => process_value(parameterFunction(value)));
            await streamWriter.WriteLineAsync(string.Join(_separator, parameterValues));

            if (!forceFlush) continue;

            await streamWriter.FlushAsync(cancellationToken);
            await gzip.FlushAsync(cancellationToken);
        }
    }

    private string process_value(object? value)
    {
        // replace " with \" in string representation
        var strValue = value?.ToString()?.Replace("\"", "\\\"");
        if (strValue is null) return "";

        // if _separator in the content then escape the whole line with quotes
        if (strValue.Contains(_separator)) return $"\"{strValue}\"";

        return strValue;
    }
}