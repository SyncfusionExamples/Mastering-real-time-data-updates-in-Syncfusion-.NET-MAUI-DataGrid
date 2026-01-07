using Syncfusion.Maui.DataGrid;
using System;
using System.Collections.Specialized;
using LiveUpdates_DataBase.ViewModels;

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
                    viewModel.BuildGridColumns(grid);
                }
            };
        }

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

        private void OnHeadersChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (BindingContext is not StockViewModel viewModel)
            {
                return;
            }

            viewModel.BuildGridColumns(grid);
        }
    }
}
