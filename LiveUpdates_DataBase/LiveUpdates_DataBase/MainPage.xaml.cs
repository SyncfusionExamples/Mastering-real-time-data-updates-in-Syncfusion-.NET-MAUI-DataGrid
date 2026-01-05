
using Syncfusion.Maui.DataGrid;
using System;
using System.Collections.Specialized;
using System.Dynamic;

namespace LiveUpdates_DataBase
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContextChanged += OnBindingContextChanged;
            Loaded += async (view, stock) =>
            {
                if (BindingContext is StockViewModel viewModel)
                {
                    await viewModel.InitializeAsync();
                    RebuildColumns(viewModel);
                }
            };
        }

        /// <summary>
        /// Handles changes to the binding context by subscribing to collection change events on the associated view
        /// model.
        /// </summary>
        private void OnBindingContextChanged(object? sender, EventArgs e)
        {
            if (BindingContext is StockViewModel viewModel)
            {
                viewModel.Headers.CollectionChanged += OnHeadersChanged;
                viewModel.Rows.CollectionChanged += (view, stock) =>
                {
                    grid?.View?.Refresh();
                };
            }
        }

        /// <summary>
        /// Handles changes to the headers collection by updating the column definitions.
        /// </summary>
        private void OnHeadersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (BindingContext is not StockViewModel viewModel)
            {
                return;
            }

            RebuildColumns(viewModel);
        }

        /// <summary>
        /// Rebuilds the columns of the data grid to match the structure and content of the specified stock view model.
        /// </summary>
        private void RebuildColumns(StockViewModel viewModel)
        {
            var mappingNames = BuildMappingNames(viewModel);
            var changeKey = DetermineChangeKey(mappingNames);
            var orderedKeys = BuildOrderedKeys(mappingNames);
            grid.Columns.Clear();

            foreach (var mapping in orderedKeys)
            {
                var mappedHeader = MapHeader(mapping);
                var keyLower = Canon(mapping);
                var indexerPath = $"[{mapping}]";

                switch (keyLower)
                {
                    case "stockchange":
                    case "change":
                        grid.Columns.Add(CreateStockChangeColumn(mapping, mappedHeader, indexerPath));
                        break;

                    case "lasttrade":
                        grid.Columns.Add(CreateLastTradeColumn(mapping, mappedHeader, indexerPath, changeKey));
                        break;

                    case "previousclose":
                    case "previous":
                        grid.Columns.Add(CreatePreviousColumn(mapping, mappedHeader, indexerPath));
                        break;

                    case "open":
                        grid.Columns.Add(CreateOpenColumn(mapping, mappedHeader, indexerPath));
                        break;

                    case "id":
                        grid.Columns.Add(CreateIdColumn(mapping, mappedHeader, indexerPath));
                        break;

                    case "symbol":
                        grid.Columns.Add(CreateSymbolColumn(mapping, mappedHeader, indexerPath));
                        break;

                    default:
                        grid.Columns.Add(CreateDefaultColumn(mapping, mappedHeader, indexerPath));
                        break;
                }
            }
        }

        /// <summary>
        /// Builds a list of mapping names from the specified stock view model, excluding any entries named "date" (case
        /// and space insensitive).
        /// </summary>
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
        /// Builds an ordered list of mapping keys based on common field names and the provided mapping names.
        /// </summary>
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
                if (!ordered.Contains(k)) ordered.Add(k);

            return ordered;
        }

        private static string Canon(string s) => (s ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant();

        /// <summary>
        /// Searches for the first string in the specified candidates that matches a key from the provided collection,
        /// using canonical string comparison.
        /// </summary>
        private static string? FindKey(IEnumerable<string> keys, params string[] candidates)
        {
            var set = keys.ToDictionary(Canon, k => k);
            foreach (var c in candidates)
            {
                var cc = Canon(c);
                if (set.TryGetValue(cc, out var found)) return found;
            }
            return null;
        }

        /// <summary>
        /// Maps common key names to user-friendly header titles.
        /// </summary>
        private static string MapHeader(string keyOrHeader)
        {
            var s = (keyOrHeader ?? string.Empty).Trim();
            var lower = Canon(s);
            return lower switch
            {
                "id" => "ID",
                "symbol" => "Symbol",
                "lasttrade" => "Last Trade",
                "open" => "Open",
                "previousclose" => "Previous",
                "stockchange" or "change" => "Stock",
                _ => s
            };
        }

        /// <summary>
        /// Creates a DataGridTemplateColumn for displaying stock change information with appropriate formatting and
        /// </summary>
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

        /// <summary>
        /// Creates a DataGridTemplateColumn for displaying last trade information with appropriate formatting and
        /// </summary>
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

        /// <summary>
        /// Creates a DataGridTemplateColumn for displaying previous close information.
        /// </summary>
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

        /// <summary>
        /// Creates a DataGridTemplateColumn for displaying open price information.
        /// </summary>
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

        /// <summary>
        /// Creates a DataGridTemplateColumn for displaying ID information.
        /// </summary>
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

        /// <summary>
        /// Creates a DataGridTemplateColumn for displaying symbol information.
        /// </summary>
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

        /// <summary>
        /// Creates a default DataGridTemplateColumn for displaying generic information.
        /// </summary>
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
    }
}
