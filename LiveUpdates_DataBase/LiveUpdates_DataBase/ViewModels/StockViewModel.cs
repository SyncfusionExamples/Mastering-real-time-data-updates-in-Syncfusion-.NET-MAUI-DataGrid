using System.Collections.ObjectModel;
using System.Dynamic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Syncfusion.Maui.DataGrid;
using LiveUpdates_DataBase.Services;

namespace LiveUpdates_DataBase.ViewModels;

/// <summary>
/// Represents a view model for managing and displaying real-time stock data, including headers and rows for tabular
/// presentation, and supporting live updates, simulation, and remote data synchronization.
/// </summary>
public class StockViewModel
{
    #region Fields
    public ObservableCollection<string> Headers { get; } = new();
    public ObservableCollection<ExpandoObject> Rows { get; } = new();
    private readonly StocksService _service = new();
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
    /// Builds and configures the columns of the specified SfDataGrid based on the current headers and rows
    /// </summary>
    /// <param name="grid"></param>
    public void BuildGridColumns(SfDataGrid grid)
    {
        var mappingNames = BuildMappingNames(this);
        var changeKey = DetermineChangeKey(mappingNames);
        var orderedKeys = BuildOrderedKeys(mappingNames);
        grid.Columns.Clear();

        foreach (var mapping in orderedKeys)
        {
            var mappedHeader = MapHeader(mapping);
            var indexerPath = $"[{mapping}]";
            var key = Canon(mapping);

            if (ColumnFactories.TryGetValue(key, out var factory))
            {
                grid.Columns.Add(factory(mapping, mappedHeader, indexerPath, changeKey));
            }
            else
            {
                grid.Columns.Add(CreateDefaultColumn(mapping, mappedHeader, indexerPath));
            }
        }
    }

    /// <summary>
    /// Builds a list of unique mapping names from the headers and rows of the specified StockViewModel,
    /// </summary>
    /// <param name="vm"></param>
    /// <returns></returns>
    private static List<string> BuildMappingNames(StockViewModel vm)
    {
        var mappingNames = new List<string>();

        if (vm.Headers.Count > 0)
            mappingNames.AddRange(vm.Headers);

        for (int r = 0; r < vm.Rows.Count; r++)
        {
            var dict = (IDictionary<string, object?>)vm.Rows[r];
            foreach (var k in dict.Keys)
                if (!mappingNames.Contains(k)) mappingNames.Add(k);
        }

        if (mappingNames.Count == 0 && vm.Rows.Count > 0)
        {
            var dict = (IDictionary<string, object?>)vm.Rows[0];
            mappingNames.AddRange(dict.Keys);
        }

        return mappingNames
            .Where(k => ((k ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant() != "date"))
            .ToList();
    }

    /// <summary>
    /// Determines the appropriate change key based on the provided list of mapping names.
    /// </summary>
    private static string? DetermineChangeKey(List<string> mappingNames)
    {
        return mappingNames.Any(x => Canon(x) == "stockchange") ? "stockChange" :
               mappingNames.Any(x => Canon(x) == "change") ? "change" : null;
    }

    /// <summary>
    /// Builds an ordered list of mapping names prioritizing common stock attributes.
    /// </summary>
    /// <param name="mappingNames"></param>
    /// <returns></returns>
    private static List<string> BuildOrderedKeys(List<string> mappingNames)
    {
        var ordered = new List<string>();

        void AddIfNotNull(string? k)
        {
            if (!string.IsNullOrEmpty(k) && !ordered.Contains(k)) ordered.Add(k);
        }

        AddIfNotNull(FindKey(mappingNames, "id", "Id"));
        AddIfNotNull(FindKey(mappingNames, "symbol", "Symbol"));
        AddIfNotNull(FindKey(mappingNames, "lastTrade", "lasttrade"));
        AddIfNotNull(FindKey(mappingNames, "open"));
        AddIfNotNull(FindKey(mappingNames, "previousClose", "previousclose", "previous"));
        AddIfNotNull(FindKey(mappingNames, "stockChange", "stockchange", "change"));

        foreach (var k in mappingNames)
        {
            if (!ordered.Contains(k))
            {
                ordered.Add(k);
            }
        }

        return ordered;
    }

    private static string Canon(string s) => (s ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

    /// <summary>
    /// Finds the first matching key from the provided candidates within the given collection of keys.
    /// </summary>
    /// <param name="keys"></param>
    /// <param name="candidates"></param>
    /// <returns></returns>
    private static string? FindKey(IEnumerable<string> keys, params string[] candidates)
    {
        var set = keys.ToDictionary(Canon, k => k);
        foreach (var c in candidates)
        {
            var cc = Canon(c);
            if (set.TryGetValue(cc, out var found))
            {
                return found;
            }
        }
        return null;
    }

    /// <summary>
    /// Maps common stock attribute keys to user-friendly header names.
    /// </summary>
    /// <param name="keyOrHeader"></param>
    /// <returns></returns>
    private static string MapHeader(string originalKeyOrHeader)
    {
        var trimmedHeader = (originalKeyOrHeader ?? string.Empty).Trim();
        var normalizedHeader = Canon(trimmedHeader);

        return normalizedHeader switch
        {
            "id" => "ID",
            "symbol" => "Symbol",
            "lasttrade" => "Last Trade",
            "open" => "Open",
            "previousclose" => "Previous",
            "stockchange" or "change" => "Stock",
            _ => trimmedHeader
        };
    }

    /// <summary>
    /// Provides a mapping of column type names to factory functions that create corresponding DataGridColumn instances.
    /// </summary>
    private static readonly Dictionary<string, Func<string, string, string, string?, DataGridColumn>> ColumnFactories = new()
    {
        ["stockchange"] = (mappingName, headerText, indexerPath, changeKey) => CreateStockChangeColumn(mappingName, headerText, indexerPath),
        ["change"] = (mappingName, headerText, indexerPath, changeKey) => CreateStockChangeColumn(mappingName, headerText, indexerPath),
        ["lasttrade"] = (mappingName, headerText, indexerPath, changeKey) => CreateLastTradeColumn(mappingName, headerText, indexerPath, changeKey),
        ["previousclose"] = (mappingName, headerText, indexerPath, changeKey) => CreatePreviousColumn(mappingName, headerText, indexerPath),
        ["previous"] = (mappingName, headerText, indexerPath, changeKey) => CreatePreviousColumn(mappingName, headerText, indexerPath),
        ["open"] = (mappingName, headerText, indexerPath, changeKey) => CreateOpenColumn(mappingName, headerText, indexerPath),
        ["id"] = (mappingName, headerText, indexerPath, changeKey) => CreateIdColumn(mappingName, headerText, indexerPath),
        ["symbol"] = (mappingName, headerText, indexerPath, changeKey) => CreateSymbolColumn(mappingName, headerText, indexerPath),
    };

    private static DataGridTemplateColumn CreateStockChangeColumn(string mapping, string header, string indexerPath)
    {
        return new DataGridTemplateColumn
        {
            MappingName = mapping,
            HeaderText = header,
            HeaderTemplate = new DataTemplate(() =>
                new Label
                {
                    Text = header,
                    FontAttributes = FontAttributes.Bold,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Padding = new Thickness(8, 0)
                }),
            CellTemplate = new DataTemplate(() =>
            {
                var h = new HorizontalStackLayout
                {
                    Spacing = 6,
                    VerticalOptions = LayoutOptions.Center,
                    HorizontalOptions = LayoutOptions.Center,
                    Padding = new Thickness(8, 0)
                };

                var arrow = new Label
                {
                    FontSize = 14,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                arrow.SetBinding(Label.TextProperty,
                    new Binding(indexerPath) { Converter = new SignToArrowConverter() });
                arrow.SetBinding(Label.TextColorProperty,
                    new Binding(indexerPath)
                    {
                        Converter = new SignToColorConverter(),
                        FallbackValue = Colors.Gray,
                        TargetNullValue = Colors.Gray
                    });

                var valueLbl = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                valueLbl.SetBinding(Label.TextProperty,
                    new Binding(indexerPath) { StringFormat = "{0:0.00}" });
                valueLbl.SetBinding(Label.TextColorProperty,
                    new Binding(indexerPath)
                    {
                        Converter = new SignToColorConverter(),
                        FallbackValue = Colors.Gray,
                        TargetNullValue = Colors.Gray
                    });

                h.Add(arrow);
                h.Add(valueLbl);
                return h;
            })
        };
    }

    private static DataGridTemplateColumn CreateLastTradeColumn(string mapping, string header, string indexerPath, string? changeKey)
    {
        return new DataGridTemplateColumn
        {
            MappingName = mapping,
            HeaderText = header,
            HeaderTemplate = new DataTemplate(() =>
                new Label
                {
                    Text = header,
                    FontAttributes = FontAttributes.Bold,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Padding = new Thickness(8, 0)
                }),
            CellTemplate = new DataTemplate(() =>
            {
                var lbl = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Padding = new Thickness(8, 0)
                };

                lbl.SetBinding(Label.TextProperty,
                    new Binding(indexerPath) { StringFormat = "{0:0.00}" });

                if (!string.IsNullOrEmpty(changeKey))
                {
                    var changeIndexer = $"[{changeKey}]";
                    lbl.SetBinding(Label.TextColorProperty,
                        new Binding(changeIndexer)
                        {
                            Converter = new SignToColorConverter(),
                            FallbackValue = Colors.Gray,
                            TargetNullValue = Colors.Gray
                        });
                }
                else
                {
                    lbl.TextColor = Colors.Gray;
                }

                return lbl;
            })
        };
    }

    private static DataGridTemplateColumn CreatePreviousColumn(string mapping, string header, string indexerPath)
    {
        return new DataGridTemplateColumn
        {
            MappingName = mapping,
            HeaderTemplate = new DataTemplate(() => new Label
            {
                Text = header,
                FontAttributes = FontAttributes.Bold,
                LineBreakMode = LineBreakMode.NoWrap,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }),
            CellTemplate = new DataTemplate(() =>
            {
                var lbl = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                lbl.SetBinding(Label.TextProperty, new Binding(indexerPath) { StringFormat = "{0:0.##}" });
                return lbl;
            })
        };
    }

    private static DataGridTemplateColumn CreateOpenColumn(string mapping, string header, string indexerPath)
    {
        return new DataGridTemplateColumn
        {
            MappingName = mapping,
            HeaderText = header,
            CellTemplate = new DataTemplate(() =>
            {
                var lbl = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                lbl.SetBinding(Label.TextProperty, new Binding(indexerPath) { StringFormat = "{0:0.##}" });
                return lbl;
            })
        };
    }

    private static DataGridTemplateColumn CreateIdColumn(string mapping, string header, string indexerPath)
    {
        return new DataGridTemplateColumn
        {
            MappingName = mapping,
            HeaderText = header,
            CellTemplate = new DataTemplate(() =>
            {
                var lbl = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                lbl.SetBinding(Label.TextProperty, new Binding(indexerPath) { StringFormat = "{0}" });
                return lbl;
            })
        };
    }

    private static DataGridTemplateColumn CreateSymbolColumn(string mapping, string header, string indexerPath)
    {
        return new DataGridTemplateColumn
        {
            MappingName = mapping,
            HeaderText = header,
            CellTemplate = new DataTemplate(() =>
            {
                var lbl = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center
                };
                lbl.SetBinding(Label.TextProperty, new Binding(indexerPath));
                return lbl;
            })
        };
    }

    private static DataGridTemplateColumn CreateDefaultColumn(string mapping, string header, string indexerPath)
    {
        return new DataGridTemplateColumn
        {
            MappingName = mapping,
            HeaderText = header,
            CellTemplate = new DataTemplate(() =>
            {
                var lbl = new Label
                {
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Start
                };
                lbl.SetBinding(Label.TextProperty, new Binding(indexerPath));
                return lbl;
            })
        };
    }

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
                    {
                        if (idVal > maxId)
                        {
                            maxId = idVal;
                        }
                    }
                }
            }
            _nextSequentialId = maxId + 1;
        }
        catch
        {
            _nextSequentialId = 1;
        }
    }

    private async Task PushSimulatedRecordAsync(CancellationToken ct)
    {
        var rec = GenerateSimulatedStock();
        await _service.PostStockAsync(rec, ct).ConfigureAwait(false);
    }

    private async Task RemoteSimLoopAsync(TimeSpan period, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await PushSimulatedRecordAsync(ct).ConfigureAwait(false);
            try { await Task.Delay(period, ct).ConfigureAwait(false); }
            catch (OperationCanceledException) { break; }
        }
    }

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

    private static bool LooksLikeJson(string? stringValue)
    {
        if (string.IsNullOrWhiteSpace(stringValue))
        {
            return false;
        }

        var trim = stringValue.TrimStart();
        return trim.StartsWith('{') || trim.StartsWith('[');
    }

    private async Task LoadSnapshotAsync(CancellationToken cancelToken)
    {
        var json = await _service.GetStocksJsonAsync(cancelToken).ConfigureAwait(false);
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
            foreach (var prop in o.Properties())
                dict[prop.Name] = ToNet(prop.Value);
        }

        return expando;
    }

    /// <summary>
    /// Converts a JToken to its corresponding .NET object representation.
    /// </summary>
    /// <param name="token">The JToken to convert. Must not be null.</param>
    private static object? ToNet(JToken token) => token.Type switch
    {
        JTokenType.Null => null,
        JTokenType.Integer => (long)token,
        JTokenType.Float => (double)token,
        JTokenType.Boolean => (bool)token,
        JTokenType.String => (string)token!,
        JTokenType.Object => ((JObject)token).Properties().ToDictionary(prop => prop.Name, p => (object?)ToNet(p.Value)),
        JTokenType.Array => ((JArray)token).Select(ToNet).ToList(),
        _ => token.ToString()
    };

    private void SetHeaders(IEnumerable<string> headers)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Headers.Clear();
            foreach (var header in headers) Headers.Add(header);
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
                foreach (var row in rows)
                {
                    Rows.Add(row);
                }

                return;
            }

            string KeyName(IEnumerable<string> keys)
            {
                foreach (var k in keys)
                {
                    var lower = k.ToLowerInvariant();
                    if (lower == "id" || lower == "symbol")
                    {
                        return k;
                    }
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
                    if (!indexByKey.ContainsKey(k))
                    {
                        indexByKey[k] = i;
                    }
                }
            }

            var seenKeys = new HashSet<string>();
            for (int i = 0; i < rows.Count; i++)
            {
                var value = (IDictionary<string, object?>)rows[i];
                var key = value.ContainsKey(keyName) ? value[keyName]?.ToString() ?? string.Empty : string.Empty;
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
    /// <param name="cancelToken">A cancellation token that can be used to request cancellation of the streaming operation.</param>
    private async Task StreamLoopAsync(CancellationToken cancelToken)
    {
        while (!cancelToken.IsCancellationRequested)
        {
            try
            {
                using var stream = await _service.TryOpenSseAsync(cancelToken).ConfigureAwait(false);
                if (stream == null)
                {
                    await Task.Delay(2000, cancelToken).ConfigureAwait(false);
                    continue;
                }
                using var reader = new StreamReader(stream);

                string? eventName = null;
                var dataBuilder = new StringBuilder();
                while (!cancelToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(cancelToken).ConfigureAwait(false);
                    if (line == null)
                    {
                        break;
                    }
                    if (line.StartsWith("event:"))
                    {
                        eventName = line[6..].Trim();
                    }
                    else if (line.StartsWith("data:"))
                    {
                        dataBuilder.AppendLine(line[5..].Trim());
                    }
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
                await Task.Delay(2000, cancelToken).ConfigureAwait(false);
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
        var event_name = JsonConvert.DeserializeObject<JObject>(dataJson) ?? new JObject();
        var data = event_name["data"];
        var path = (string?)event_name["path"] ?? "/";
        switch (eventName)
        {
            case "put":
                if (!string.Equals(path, "/", StringComparison.Ordinal))
                {
                    LoadSnapshotAsync();
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
                LoadSnapshotAsync();
                break;
        }
    }
}
