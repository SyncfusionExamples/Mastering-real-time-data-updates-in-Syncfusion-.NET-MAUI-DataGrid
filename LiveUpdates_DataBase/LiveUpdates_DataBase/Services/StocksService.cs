using Newtonsoft.Json;
using System.Text;

namespace LiveUpdates_DataBase;
public class StocksService
{
    #region Fields

    private const string DbBase = "https://datagridlivedataupdates-default-rtdb.firebaseio.com";
    private const string PathStocks = "/stocks.json";
    private readonly string _authToken;
    private readonly HttpClient _http;
    private string StocksUrl => string.IsNullOrWhiteSpace(_authToken)
        ? $"{DbBase}{PathStocks}"
        : $"{DbBase}{PathStocks}?auth={_authToken}";

    #endregion

    public StocksService(string authToken = "", HttpClient? httpClient = null)
    {
        _authToken = authToken;
        _http = httpClient ?? new HttpClient();
        _http.Timeout = Timeout.InfiniteTimeSpan;
    }

    /// <summary>
    /// Get stocks json
    /// </summary>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    public async Task<string?> GetStocksJsonAsync(CancellationToken cancelToken)
    {
        using var res = await _http.GetAsync(StocksUrl, cancelToken).ConfigureAwait(false);
        if (!res.IsSuccessStatusCode)
        {
            return null;
        }

        return await res.Content.ReadAsStringAsync(cancelToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Post stock record
    /// </summary>
    /// <param name="record"></param>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    public async Task<bool> PostStockAsync(object record, CancellationToken cancelToken)
    {
        using var content = new StringContent(JsonConvert.SerializeObject(record), Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(StocksUrl, content, cancelToken).ConfigureAwait(false);
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Try open SSE stream
    /// </summary>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    public async Task<Stream?> TryOpenSseAsync(CancellationToken cancelToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, StocksUrl);
        request.Headers.Accept.Clear();
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Headers.ConnectionClose = false;
        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancelToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            return null;
        }

        return await response.Content.ReadAsStreamAsync(cancelToken).ConfigureAwait(false);
    }
}

