using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LiveUpdates_DataBase;

/// <summary>
/// Represents a view model for managing and displaying real-time stock data, including headers and rows for tabular
/// presentation, and supporting live updates, simulation, and remote data synchronization.
/// </summary>
public class StockViewModel
{
    #region Fields
    private const string DbBase = "https://datagridlivedataupdates-default-rtdb.firebaseio.com";
    private const string PathStocks = "/stocks.json";
    private const string AuthToken = "";
    private string StocksUrl => string.IsNullOrWhiteSpace(AuthToken)
        ? $"{DbBase}{PathStocks}"
        : $"{DbBase}{PathStocks}?auth={AuthToken}";
    public ObservableCollection<string> Headers { get; } = new();
    public ObservableCollection<ExpandoObject> Rows { get; } = new();
    private readonly HttpClient _http = new();
    private CancellationTokenSource? _streamCts;
    private CancellationTokenSource? _pollCts;
    private CancellationTokenSource? _demoCts;
    private long _nextSequentialId = 1;
    private readonly Random _rand = new();
    public bool EnableRemoteSimulation { get; set; } = true;
    public TimeSpan RemoteSimulationPeriod { get; set; } = TimeSpan.FromSeconds(0.75);
    private CancellationTokenSource? _remoteSimCts;
    #endregion

    /// <summary>
    /// Recalculates the next sequential identifier based on the current set of rows.
    /// </summary>
    private void RecalculateNextIdFromRows()
    {
        try
        {
            long maxId = 0;
            for (int i = 0; i < Rows.Count; i++)
            {
                var dict = (IDictionary<string, object?>)Rows[i];
                if (dict.TryGetValue("id", out var idObj) && idObj != null)
                {
                    if (long.TryParse(idObj.ToString(), out var idVal))
                        if (idVal > maxId) maxId = idVal;
                }
            }
            _nextSequentialId = maxId + 1;
        }
        catch
        {
            _nextSequentialId = 1;
        }
    }

    /// <summary>
    /// Asynchronously sends a simulated stock record to the configured stocks endpoint using an HTTP POST request.
    /// </summary>
    private async Task PushSimulatedRecordAsync(CancellationToken ct)
    {
        var rec = GenerateSimulatedStock();
        using var content = new StringContent(
            JsonConvert.SerializeObject(rec),
            Encoding.UTF8,
            "application/json");

        using var resp = await _http.PostAsync(StocksUrl, content, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Periodically pushes simulated records at the specified interval until cancellation is requested.
    /// </summary>
    private async Task RemoteSimLoopAsync(TimeSpan period, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await PushSimulatedRecordAsync(ct).ConfigureAwait(false);
            try { await Task.Delay(period, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

    /// <summary>
    /// Initializes the service and starts all background processing loops asynchronously.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_streamCts != null)
        {
            _streamCts.Cancel();
            _streamCts.Dispose();
            _streamCts = null;
        }

        if (_pollCts != null)
        {
            _pollCts.Cancel();
            _pollCts.Dispose();
            _pollCts = null;
        }

        if (_demoCts != null)
        {
            _demoCts.Cancel();
            _demoCts.Dispose();
            _demoCts = null;
        }

        if (_remoteSimCts != null)
        {
            _remoteSimCts.Cancel(); _remoteSimCts.Dispose(); _remoteSimCts = null;
        }

        await LoadSnapshotAsync();

        _streamCts = new CancellationTokenSource();
        _ = Task.Run(() => StreamLoopAsync(_streamCts.Token));

        _pollCts = new CancellationTokenSource();
        _ = Task.Run(() => PollLoopAsync(TimeSpan.FromSeconds(3), _pollCts.Token));

        _demoCts = new CancellationTokenSource();
        _ = Task.Run(() => DemoChangeLoopAsync(TimeSpan.FromSeconds(1.5), _demoCts.Token));

        if (EnableRemoteSimulation)
        {
            _remoteSimCts = new CancellationTokenSource();
            _ = Task.Run(() => RemoteSimLoopAsync(RemoteSimulationPeriod, _remoteSimCts.Token));
        }
    }

    /// <summary>
    /// Determines whether the specified string appears to be a JSON object or array based on its leading character.
    /// </summary>
    private static bool LooksLikeJson(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        var t = s.TrimStart();
        return t.StartsWith('{') || t.StartsWith('[');
    }

    /// <summary>
    /// Asynchronously loads the latest stock snapshot data and updates the current headers and rows.
    /// </summary>
    private async Task LoadSnapshotAsync(CancellationToken ct)
    {
        var res = await _http.GetAsync(StocksUrl, ct);
        if (!res.IsSuccessStatusCode)
        {
            SetHeaders(Array.Empty<string>());
            SetRows(new List<ExpandoObject>());
            return;
        }

        var json = await res.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(json) || json == "null" || !LooksLikeJson(json))
        {
            SetHeaders(Array.Empty<string>());
            SetRows(new List<ExpandoObject>());
            return;
        }
        try
        {
            var token = JToken.Parse(json);
            ProcessToken(token);
        }
        catch
        {
            SetHeaders(Array.Empty<string>());
            SetRows(new List<ExpandoObject>());
        }
    }

    private Task LoadSnapshotAsync() => LoadSnapshotAsync(CancellationToken.None);

    /// <summary>
    /// Periodically loads a snapshot at the specified interval until cancellation is requested.
    /// </summary>
    /// <param name="period">The time interval to wait between each snapshot load operation. Must be greater than zero.</param>
    /// <param name="ct">A cancellation token that can be used to request cancellation of the polling loop.</param>
    private async Task PollLoopAsync(TimeSpan period, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await LoadSnapshotAsync();
            await Task.Delay(period, ct);
        }
    }

    /// <summary>
    /// Generates a simulated stock quote with random values for common stock attributes.
    /// </summary>
    private IDictionary<string, object?> GenerateSimulatedStock()
    {
        var symbols = new[] { "MSFT", "AAPL", "GOOG", "AMZN", "TSLA", "NFLX" };
        var symbol = symbols[_rand.Next(symbols.Length)];
        var previousClose = Math.Round(50 + _rand.NextDouble() * 150.0, 2);
        var pct = (_rand.NextDouble() - 0.5) * 0.06;
        var stockChange = Math.Round(previousClose * pct, 2);
        var lastTrade = Math.Round(previousClose + stockChange, 2);
        var openPct = (_rand.NextDouble() - 0.5) * 0.01;
        var open = Math.Round(previousClose * (1 + openPct), 2);
        var id = _nextSequentialId++;

        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["symbol"] = symbol,
            ["previousClose"] = previousClose,
            ["open"] = open,
            ["stockChange"] = stockChange,
            ["lastTrade"] = lastTrade,
        };
    }

    /// <summary>
    /// Periodically updates stock data rows with simulated changes until cancellation is requested.
    /// </summary>
    /// <param name="period">The interval to wait between each update cycle.</param>
    /// <param name="ct">A cancellation token that can be used to request cancellation of the update loop.</param>
    private async Task DemoChangeLoopAsync(TimeSpan period, CancellationToken ct)
    {
        var random = new Random();
        while (!ct.IsCancellationRequested)
        {
            if (Rows.Count > 0)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    for (int i = 0; i < Rows.Count; i++)
                    {
                        var dict = (IDictionary<string, object?>)Rows[i];
                        var newChange = Math.Round((random.NextDouble() - 0.5) * 10.0, 2);
                        double prevClose = 0.0;
                        bool hasPrevClose = false;
                        if (dict.TryGetValue("previousClose", out var pcObj))
                        {
                            try { prevClose = Convert.ToDouble(pcObj ?? 0, CultureInfo.InvariantCulture); hasPrevClose = true; } catch { hasPrevClose = false; }
                        }

                        dict["stockChange"] = newChange;
                        if (hasPrevClose)
                        {
                            dict["lastTrade"] = Math.Round(prevClose + newChange, 2);
                        }
                        else
                        {
                            if (dict.TryGetValue("lastTrade", out var ltObj))
                            {
                                double baseLt = 0.0;
                                baseLt = Convert.ToDouble(ltObj ?? 0, CultureInfo.InvariantCulture);
                                var deltaLt = Math.Round((random.NextDouble() - 0.5) * 2.0, 2);
                                dict["lastTrade"] = baseLt + deltaLt;
                            }
                        }

                        Rows[i] = Rows[i];
                    }
                });
            }

            await Task.Delay(period, ct);
        }
    }

    /// <summary>
    /// Processes the specified JSON token and updates the headers and rows based on its structure.
    /// </summary>
    /// <param name="token">The JSON token to process. Must represent an object or array containing tabular data, such as headers and rows,
    /// or a collection of stock objects.</param>
    private void ProcessToken(JToken token)
    {
        if (token is JObject obj && obj.Property("headers") != null && obj.Property("rows") != null)
        {
            var hdrs = obj["headers"]!
                .Values<string?>()
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Cast<string>()
                .ToArray();
            var rows = obj["rows"]!.Select(ToExpando).ToList();
            SetHeaders(hdrs);
            SetRows(rows);
            return;
        }

        if (token is JObject root && root.Property("stocks") != null)
        {
            token = root["stocks"]!;
        }

        IEnumerable<JObject> rowObjects;
        if (token is JArray arr)
        {
            rowObjects = arr.OfType<JObject>();
        }
        else if (token is JObject map)
        {
            rowObjects = map.Properties().Select(p => p.Value as JObject).Where(j => j != null)!.Cast<JObject>();
        }
        else
        {
            SetHeaders(Array.Empty<string>());
            SetRows(new List<ExpandoObject>());
            return;
        }

        var rowsList = rowObjects.Select(ToExpando).ToList();
        var headers = rowsList
            .SelectMany(e => ((IDictionary<string, object?>)e).Keys)
            .Distinct()
            .ToArray();
        SetHeaders(headers);
        SetRows(rowsList);
        RecalculateNextIdFromRows();
    }

    /// <summary>
    /// Converts a <see cref="JToken"/> representing a JSON object into an <see cref="ExpandoObject"/> with dynamic
    /// properties corresponding to the object's properties.
    /// </summary>
    /// <remarks>Properties in the resulting <see cref="ExpandoObject"/> are populated using the <c>ToNet</c>
    /// method to convert each property's value. Only properties from JSON objects are included; other token types
    /// result in an empty object.</remarks>
    /// <param name="token">The <see cref="JToken"/> to convert. Must represent a JSON object.</param>
    private static ExpandoObject ToExpando(JToken token)
    {
        var expando = new ExpandoObject();
        var dict = (IDictionary<string, object?>)expando;
        if (token is JObject o)
        {
            foreach (var p in o.Properties())
                dict[p.Name] = ToNet(p.Value);
        }

        return expando;
    }

    /// <summary>
    /// Converts a JToken to its corresponding .NET object representation.
    /// </summary>
    /// <param name="t">The JToken to convert. Must not be null.</param>
    private static object? ToNet(JToken t) => t.Type switch
    {
        JTokenType.Null => null,
        JTokenType.Integer => (long)t,
        JTokenType.Float => (double)t,
        JTokenType.Boolean => (bool)t,
        JTokenType.String => (string)t!,
        JTokenType.Object => ((JObject)t).Properties().ToDictionary(p => p.Name, p => (object?)ToNet(p.Value)),
        JTokenType.Array => ((JArray)t).Select(ToNet).ToList(),
        _ => t.ToString()
    };

    private void SetHeaders(IEnumerable<string> headers)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Headers.Clear();
            foreach (var h in headers) Headers.Add(h);
        });
    }

    /// <summary>
    /// Updates the collection of rows to match the specified list, adding, updating, or removing items as necessary to
    /// synchronize the data.
    /// </summary>
    /// <param name="rows">The list of dynamic row objects to synchronize with the current collection. Each row should be an ExpandoObject
    /// representing a data record.</param>
    private void SetRows(List<ExpandoObject> rows)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (Rows.Count == 0)
            {
                foreach (var r in rows) Rows.Add(r);
                return;
            }

            string KeyName(IEnumerable<string> keys)
            {
                foreach (var k in keys)
                {
                    var lower = k.ToLowerInvariant();
                    if (lower == "id" || lower == "symbol") return k;
                }
                return keys.First();
            }

            var firstKeys = rows.FirstOrDefault() is ExpandoObject eo
                ? ((IDictionary<string, object?>)eo).Keys
                : Array.Empty<string>();
            if (!firstKeys.Any())
            {
                Rows.Clear();
                return;
            }
            var keyName = KeyName(firstKeys);
            var indexByKey = new Dictionary<string, int>();
            for (int i = 0; i < Rows.Count; i++)
            {
                var dict = (IDictionary<string, object?>)Rows[i];
                if (dict.ContainsKey(keyName))
                {
                    var k = dict[keyName]?.ToString() ?? string.Empty;
                    if (!indexByKey.ContainsKey(k)) indexByKey[k] = i;
                }
            }

            var seenKeys = new HashSet<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                var nd = (IDictionary<string, object?>)rows[i];
                var key = nd.ContainsKey(keyName) ? nd[keyName]?.ToString() ?? string.Empty : string.Empty;
                seenKeys.Add(key);

                if (indexByKey.TryGetValue(key, out var existingIndex))
                {
                    Rows[existingIndex] = rows[i];
                }
                else
                {
                    Rows.Add(rows[i]);
                }
            }

            for (int i = Rows.Count - 1; i >= 0; i--)
            {
                var dict = (IDictionary<string, object?>)Rows[i];
                var key = dict.ContainsKey(keyName) ? dict[keyName]?.ToString() ?? string.Empty : string.Empty;
                if (!seenKeys.Contains(key)) Rows.RemoveAt(i);
            }

            RecalculateNextIdFromRows();
        });
    }

    /// <summary>
    /// Continuously streams and processes server-sent events from the stocks endpoint until cancellation is requested.
    /// </summary>
    /// <param name="ct">A cancellation token that can be used to request cancellation of the streaming operation.</param>
    private async Task StreamLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, StocksUrl);
                req.Headers.Add("Accept", "text/event-stream");
                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                if (!res.IsSuccessStatusCode)
                {
                    await Task.Delay(2000, ct);
                    continue;
                }
                using var stream = await res.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream);

                string? eventName = null;
                var dataBuilder = new StringBuilder();
                while (!ct.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(ct);
                    if (line == null)
                    {
                        break;
                    }
                    if (line.StartsWith("event:")) eventName = line[6..].Trim();
                    else if (line.StartsWith("data:")) dataBuilder.AppendLine(line[5..].Trim());
                    else if (string.IsNullOrWhiteSpace(line))
                    {
                        var data = dataBuilder.ToString();
                        dataBuilder.Clear();
                        if (!string.IsNullOrWhiteSpace(eventName) && !string.IsNullOrWhiteSpace(data))
                            ApplyFirebaseEvent(eventName!, data);
                        eventName = null;
                    }
                }
            }
            catch
            {
                await Task.Delay(2000, ct);
            }
        }
    }

    /// <summary>
    /// Applies a Firebase event to update the data.
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="dataJson"></param>
    private void ApplyFirebaseEvent(string eventName, string dataJson)
    {
        var evt = JsonConvert.DeserializeObject<JObject>(dataJson) ?? new JObject();
        var data = evt["data"];
        var path = (string?)evt["path"] ?? "/";
        switch (eventName)
        {
            case "put":
                if (!string.Equals(path, "/", StringComparison.Ordinal))
                {
                    _ = LoadSnapshotAsync();
                    return;
                }

                if (data == null || data.Type == JTokenType.Null)
                {
                    SetHeaders(Array.Empty<string>());
                    SetRows(new List<ExpandoObject>());
                }
                else
                {
                    ProcessToken(data);
                }
                break;
            case "patch":
                _ = LoadSnapshotAsync();
                break;
        }
    }
}
