using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WebsiteAPI.Logging;

public sealed partial class RequestQueryLoggingInterceptor(RequestLogContext requestLog)
    : DbCommandInterceptor
{
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Capture(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Capture(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Capture(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Capture(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Capture(command);
        return result;
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Capture(command);
        return ValueTask.FromResult(result);
    }

    private void Capture(DbCommand command)
    {
        var sql = Whitespace().Replace(command.CommandText.Trim(), " ");
        var parameters = command.Parameters
            .Cast<DbParameter>()
            .Select(parameter => $"{parameter.ParameterName}={FormatParameter(parameter)}")
            .ToArray();

        requestLog.AddQuery(parameters.Length == 0
            ? sql
            : $"{sql} | {string.Join(", ", parameters)}");
    }

    private static string FormatParameter(DbParameter parameter)
    {
        var name = parameter.ParameterName;
        if (SensitiveName().IsMatch(name))
        {
            return "[REDACTED]";
        }

        if (parameter.Value is null or DBNull)
        {
            return "null";
        }

        var value = Convert.ToString(parameter.Value, System.Globalization.CultureInfo.InvariantCulture)
            ?? string.Empty;
        return value.Length <= 200 ? value : $"{value[..200]}…";
    }

    [GeneratedRegex(@"password|token|secret|authorization|jwt", RegexOptions.IgnoreCase)]
    private static partial Regex SensitiveName();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
