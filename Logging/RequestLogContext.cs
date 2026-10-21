namespace WebsiteAPI.Logging;

public sealed class RequestLogContext
{
    private readonly List<string> queries = [];
    private readonly object sync = new();

    public void AddQuery(string query)
    {
        lock (sync)
        {
            queries.Add(query);
        }
    }

    public IReadOnlyList<string> GetQueries()
    {
        lock (sync)
        {
            return queries.ToArray();
        }
    }
}
