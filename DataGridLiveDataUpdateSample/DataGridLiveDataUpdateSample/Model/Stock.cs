using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataGridLiveDataUpdateSample
{
    /// <summary>
    /// Represents a stock entity with properties for symbol, account, trade details, and volume.
    /// Implements <see cref="INotifyPropertyChanged"/> for real-time UI updates in .NET MAUI DataGrid.
    /// </summary>
    public class Stock : INotifyPropertyChanged
    {
        #region Private Members
        private string? symbol;
        private string? account;
        private double lastTrade;
        private string? stockChange;
        private double previousClose;
        private double open;
        private long volume;
        #endregion

        /// <summary>
        /// Event triggered when a property value changes.
        /// Used for data binding updates in the UI.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #region Public Properties

        /// <summary>
        /// Gets or sets the stock change indicator.
        /// </summary>
        public string? StockChange
        {
            get => this.stockChange;
            set
            {
                this.stockChange = value;
                this.RaisePropertyChanged(nameof(StockChange));
            }
        }

        /// <summary>
        /// Gets or sets the opening price of the stock.
        /// </summary>
        public double Open
        {
            get => this.open;
            set
            {
                this.open = value;
                this.RaisePropertyChanged(nameof(Open));
            }
        }

        /// <summary>
        /// Gets or sets the last traded price of the stock.
        /// </summary>
        public double LastTrade
        {
            get => this.lastTrade;
            set
            {
                this.lastTrade = value;
                this.RaisePropertyChanged(nameof(LastTrade));
            }
        }

        /// <summary>
        /// Gets or sets the previous closing price of the stock.
        /// </summary>
        public double PreviousClose
        {
            get => this.previousClose;
            set
            {
                this.previousClose = value;
                this.RaisePropertyChanged(nameof(PreviousClose));
            }
        }

        /// <summary>
        /// Gets or sets the stock symbol
        /// </summary>
        public string? Symbol
        {
            get => this.symbol;
            set
            {
                this.symbol = value;
                this.RaisePropertyChanged(nameof(Symbol));
            }
        }

        /// <summary>
        /// Gets or sets the account associated with the stock.
        /// </summary>
        public string? Account
        {
            get => this.account;
            set
            {
                this.account = value;
                this.RaisePropertyChanged(nameof(Account));
            }
        }

        /// <summary>
        /// Gets or sets the trading volume for the stock.
        /// </summary>
        public long Volume
        {
            get => this.volume;
            set
            {
                this.volume = value;
                this.RaisePropertyChanged(nameof(Volume));
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

