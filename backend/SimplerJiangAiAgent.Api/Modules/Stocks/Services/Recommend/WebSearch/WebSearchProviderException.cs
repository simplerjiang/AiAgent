namespace SimplerJiangAiAgent.Api.Modules.Stocks.Services.Recommend.WebSearch;

public sealed class WebSearchProviderException : Exception
{
    public string Provider { get; }

    public WebSearchProviderException(string provider, string message)
        : base($"[{provider}] {message}")
    {
        Provider = provider;
    }

    public WebSearchProviderException(string provider, string message, Exception innerException)
        : base($"[{provider}] {message}", innerException)
    {
        Provider = provider;
    }
}
