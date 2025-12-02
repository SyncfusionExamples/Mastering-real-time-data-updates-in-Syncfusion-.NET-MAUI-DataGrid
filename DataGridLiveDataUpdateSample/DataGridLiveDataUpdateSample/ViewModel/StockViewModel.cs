using Microsoft.Maui.Dispatching;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DataGridLiveDataUpdateSample
{
    /// <summary>
    /// ViewModel for managing live stock data updates in a .NET MAUI DataGrid.
    /// Implements <see cref="INotifyPropertyChanged"/> for UI binding and <see cref="IDisposable"/> for resource cleanup.
    /// </summary>
    public class StockViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Members

        private ObservableCollection<Stock> data;
        private IDispatcherTimer? timer;
        private Random r = new Random(123345345);
        private int noOfUpdates = 500;
        private List<string> stockSymbols = new List<string>();
        private string[] accounts = new string[]
        {
            "American Funds",
            "College Savings",
            "Day Trading",
            "Mountain Range",
            "Fidelity Funds",
            "Mortages",
            "Housing Loans"
        };

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="StockViewModel"/> class.
        /// Populates initial stock data and starts the refresh timer.
        /// </summary>
        public StockViewModel()
        {
            this.data = new ObservableCollection<Stock>();
            this.AddRows(200);
            this.ResetRefreshFrequency(2500);
        }

        #endregion

        /// <summary>
        /// Event triggered when a property value changes for data binding updates.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets the collection of stock data displayed in the DataGrid.
        /// </summary>
        public ObservableCollection<Stock> Stocks => this.data;

        /// <summary>
        /// Gets or sets the selected item (used for UI binding).
        /// </summary>
        public object SelectedItem
        {
            get => this.noOfUpdates;
            set
            {
                this.noOfUpdates = 2;
                this.RaisePropertyChanged(nameof(SelectedItem));
            }
        }

        /// <summary>
        /// Gets a collection of refresh frequency options for the UI ComboBox.
        /// </summary>
        public List<int> ComboCollection => new List<int> { 500, 5000, 50000, 500000 };

        #region Timer and Updating Code

        /// <summary>
        /// Starts the timer for periodic stock data updates.
        /// </summary>
        public void StartTimer()
        {
            timer = Dispatcher.GetForCurrentThread()!.CreateTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Start();
            timer.Tick += Timer_Elapsed;
        }

        /// <summary>
        /// Handles the timer tick event and triggers data updates.
        /// </summary>
        private void Timer_Elapsed(object? sender, EventArgs e)
        {
            this.Timer_Tick();
        }

        /// <summary>
        /// Resets the refresh frequency and restarts the timer.
        /// </summary>
        /// <param name="changesPerTick">Number of changes per tick.</param>
        public void ResetRefreshFrequency(int changesPerTick)
        {
            this.noOfUpdates = changesPerTick;
            this.StartTimer();
        }

        /// <summary>
        /// Updates random rows in the stock collection on each timer tick.
        /// </summary>
        private void Timer_Tick()
        {
            this.noOfUpdates = 100;
            this.ChangeRows(this.noOfUpdates);
        }

        /// <summary>
        /// Adds initial rows of stock data to the collection.
        /// </summary>
        /// <param name="count">Number of rows to add.</param>
        private void AddRows(int count)
        {
            for (int i = 0; i < count; ++i)
            {
                var newRec = new Stock();
                newRec.Symbol = this.ChangeSymbol();
                newRec.Account = this.ChangeAccount(i);
                newRec.Open = Math.Round(this.r.NextDouble() * 30, 2);
                newRec.LastTrade = Math.Round(1 + (this.r.NextDouble() * 50));
                double d = this.r.NextDouble();
                if (d < .5)
                {
                    newRec.StockChange = string.Format(" {0:N2}", d);
                }
                else
                {
                    newRec.StockChange = string.Format("-{0:N2}", d);
                }

                newRec.PreviousClose = Math.Round(this.r.NextDouble() * 30, 2);
                newRec.Volume = this.r.Next();
                this.data.Add(newRec);
            }
        }

        /// <summary>
        /// Generates a unique random stock symbol.
        /// </summary>
        /// <returns>A 4-character stock symbol.</returns>
        private string ChangeSymbol()
        {
            StringBuilder builder;
            Random random = new Random();
            char ch;

            do
            {
                builder = new StringBuilder();
                for (int i = 0; i < 4; i++)
                {
                    ch = Convert.ToChar(Convert.ToInt32(Math.Floor((26 * random.NextDouble()) + 65)));
                    builder.Append(ch);
                }
            }
            while (this.stockSymbols.Contains(builder.ToString()));

            this.stockSymbols.Add(builder.ToString());
            return builder.ToString();
        }

        /// <summary>
        /// Returns an account name based on the given index.
        /// </summary>
        /// <param name="index">Index for account selection.</param>
        private string ChangeAccount(int index)
        {
            return this.accounts[index % this.accounts.Length];
        }

        /// <summary>
        /// Randomly updates existing rows in the stock collection.
        /// </summary>
        /// <param name="count">Number of rows to update.</param>
        private void ChangeRows(int count)
        {
            if (this.data.Count < count)
            {
                count = this.data.Count;
            }

            for (int i = 0; i < count; ++i)
            {
                int recNo = this.r.Next(this.data.Count);
                Stock recRow = this.data[recNo];

                this.data[recNo].LastTrade = Math.Round(1 + (this.r.NextDouble() * 50));

                double d = this.r.NextDouble();
                if (d < .5)
                {
                    this.data[recNo].StockChange = string.Format(" {0:N2}", d);
                }
                else
                {
                    this.data[recNo].StockChange = string.Format("-{0:N2}", d);
                }

                this.data[recNo].Open = Math.Round(this.r.NextDouble() * 50, 2);
                this.data[recNo].PreviousClose = Math.Round(this.r.NextDouble() * 30, 2);
                this.data[recNo].Volume = this.r.Next();
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="name">The property name that changed.</param>
        private void RaisePropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Disposes resources and stops the timer.
        /// </summary>
        public void Dispose()
        {
            if (timer != null)
            {
                timer.Tick -= Timer_Elapsed;
                timer.Stop();
                timer = null;
            }
        }

        #endregion
    }
}

