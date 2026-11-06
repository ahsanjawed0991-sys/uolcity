using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace UOLCity
{
    /// <summary>
    /// Handles local JSON persistence, input validation, searches, and calculations
    /// for all modules in UOL CITY — Designed to Evolve.
    /// </summary>
    public class TownDataStore
    {
        // JSON serializer settings: indented output makes saved JSON easy to read in Notepad
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _dataDirectory;
        private readonly string _booksFilePath;
        private readonly string _transactionsFilePath;
        private readonly string _busSchedulesFilePath;

        /// <summary>
        /// UOL CITY always has exactly 4 functional locations:
        /// 1. Library, 2. Bank, 3. Café, 4. Bus / Transport Hub.
        /// </summary>
        public int TotalLocations => 4;

        /// <summary>
        /// Exposes the local folder path where JSON data files are saved.
        /// </summary>
        public string DataDirectory => _dataDirectory;

        /// <summary>
        /// Holds an informational warning if a corrupted JSON file had to be backed up and reset.
        /// </summary>
        public string? LastStorageNotice { get; private set; }

        public TownDataStore(string? customDataDirectory = null)
        {
            // Store data in %LocalAppData%\UOLCity by default
            if (string.IsNullOrWhiteSpace(customDataDirectory))
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                _dataDirectory = Path.Combine(localAppData, "UOLCity");
            }
            else
            {
                _dataDirectory = customDataDirectory;
            }

            _booksFilePath = Path.Combine(_dataDirectory, "library_books.json");
            _transactionsFilePath = Path.Combine(_dataDirectory, "bank_transactions.json");
            _busSchedulesFilePath = Path.Combine(_dataDirectory, "bus_schedules.json");

            EnsureDataDirectoryExists();
        }

        // =====================================================================
        // 1. LIBRARY MODULE METHODS (Add, Search, Remove, Save & Load JSON)
        // =====================================================================

        public List<BookItem> LoadBooks()
        {
            return LoadJsonList(_booksFilePath, CreateStarterBooks);
        }

        public void SaveBooks(List<BookItem> books)
        {
            SaveJsonList(_booksFilePath, books);
        }

        /// <summary>
        /// Validates book inputs, adds a new BookItem to the list, and saves to JSON.
        /// </summary>
        public bool TryAddBook(List<BookItem> books, string? title, string? author, out string message)
        {
            string cleanTitle = (title ?? string.Empty).Trim();
            string cleanAuthor = (author ?? string.Empty).Trim();

            // Conditional validation for incomplete form submissions
            if (string.IsNullOrWhiteSpace(cleanTitle))
            {
                message = "Please enter a book title before adding.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cleanAuthor))
            {
                message = "Please enter the author's name before adding.";
                return false;
            }

            if (cleanTitle.Length > 120 || cleanAuthor.Length > 100)
            {
                message = "Book title or author name is too long (maximum 120 characters).";
                return false;
            }

            // Check if the exact same book already exists using a beginner-friendly foreach loop
            foreach (BookItem existing in books)
            {
                if (string.Equals(existing.Title, cleanTitle, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.Author, cleanAuthor, StringComparison.OrdinalIgnoreCase))
                {
                    message = $"\"{cleanTitle}\" by {cleanAuthor} is already in your library.";
                    return false;
                }
            }

            BookItem newBook = new BookItem
            {
                Title = cleanTitle,
                Author = cleanAuthor,
                AddedDate = DateTime.Now.ToString("yyyy-MM-dd")
            };

            books.Add(newBook);
            SaveBooks(books);

            message = $"Added \"{cleanTitle}\" by {cleanAuthor} to your library.";
            return true;
        }

        /// <summary>
        /// Filters books whose Title or Author contains the search text (case-insensitive).
        /// </summary>
        public List<BookItem> SearchBooks(List<BookItem> books, string? query)
        {
            string search = (query ?? string.Empty).Trim();

            // If search box is empty, return all books
            if (string.IsNullOrWhiteSpace(search))
            {
                return new List<BookItem>(books);
            }

            List<BookItem> matches = new List<BookItem>();

            foreach (BookItem book in books)
            {
                bool titleMatch = book.Title.Contains(search, StringComparison.OrdinalIgnoreCase);
                bool authorMatch = book.Author.Contains(search, StringComparison.OrdinalIgnoreCase);

                if (titleMatch || authorMatch)
                {
                    matches.Add(book);
                }
            }

            return matches;
        }

        /// <summary>
        /// Removes a book by its unique ID and updates the local JSON file.
        /// </summary>
        public bool RemoveBookById(List<BookItem> books, string? bookId, out string message)
        {
            if (string.IsNullOrWhiteSpace(bookId))
            {
                message = "Please select a book from the list to remove.";
                return false;
            }

            for (int i = 0; i < books.Count; i++)
            {
                if (books[i].Id == bookId)
                {
                    string removedTitle = books[i].Title;
                    books.RemoveAt(i);
                    SaveBooks(books);
                    message = $"Removed \"{removedTitle}\" from the library.";
                    return true;
                }
            }

            message = "Could not find the selected book to remove.";
            return false;
        }

        // =====================================================================
        // 2. BANK MODULE METHODS (Income, Expenses, Balance Calculation, JSON)
        // =====================================================================

        public List<BankTransaction> LoadTransactions()
        {
            return LoadJsonList(_transactionsFilePath, CreateStarterTransactions);
        }

        public void SaveTransactions(List<BankTransaction> transactions)
        {
            SaveJsonList(_transactionsFilePath, transactions);
        }

        /// <summary>
        /// Calculates the current wallet balance directly from saved transaction records.
        /// Income adds to the balance; Expense subtracts from the balance.
        /// </summary>
        public decimal CalculateWalletBalance(List<BankTransaction> transactions)
        {
            decimal balance = 0m;

            foreach (BankTransaction tx in transactions)
            {
                if (string.Equals(tx.Type, "Income", StringComparison.OrdinalIgnoreCase))
                {
                    balance += tx.Amount;
                }
                else if (string.Equals(tx.Type, "Expense", StringComparison.OrdinalIgnoreCase))
                {
                    balance -= tx.Amount;
                }
            }

            return balance;
        }

        /// <summary>
        /// Calculates the sum of all Income transactions.
        /// </summary>
        public decimal CalculateTotalIncome(List<BankTransaction> transactions)
        {
            decimal totalIncome = 0m;
            foreach (BankTransaction tx in transactions)
            {
                if (string.Equals(tx.Type, "Income", StringComparison.OrdinalIgnoreCase))
                {
                    totalIncome += tx.Amount;
                }
            }
            return totalIncome;
        }

        /// <summary>
        /// Calculates the sum of all Expense transactions.
        /// </summary>
        public decimal CalculateTotalExpenses(List<BankTransaction> transactions)
        {
            decimal totalExpenses = 0m;
            foreach (BankTransaction tx in transactions)
            {
                if (string.Equals(tx.Type, "Expense", StringComparison.OrdinalIgnoreCase))
                {
                    totalExpenses += tx.Amount;
                }
            }
            return totalExpenses;
        }

        /// <summary>
        /// Validates description and monetary input, records an Income or Expense transaction,
        /// and persists the updated transaction history to JSON.
        /// </summary>
        public bool TryAddTransaction(
            List<BankTransaction> transactions,
            string transactionType,
            string? description,
            string? amountText,
            out string message)
        {
            string cleanType = string.Equals(transactionType, "Expense", StringComparison.OrdinalIgnoreCase)
                ? "Expense"
                : "Income";

            string cleanDescription = (description ?? string.Empty).Trim();
            string cleanAmountText = (amountText ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(cleanDescription))
            {
                message = "Please enter a short description for this transaction.";
                return false;
            }

            if (cleanDescription.Length > 100)
            {
                message = "Description is too long (maximum 100 characters).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cleanAmountText))
            {
                message = "Please enter an amount in Rupees (for example, 500 or 1250.50).";
                return false;
            }

            // Parse monetary input safely using TryParse
            bool parsed = decimal.TryParse(
                cleanAmountText,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal amount)
                || decimal.TryParse(
                    cleanAmountText,
                    NumberStyles.Number,
                    CultureInfo.CurrentCulture,
                    out amount);

            if (!parsed)
            {
                message = "Invalid amount entered. Please enter numbers only (for example, 1500).";
                return false;
            }

            if (amount <= 0m)
            {
                message = "Transaction amount must be greater than zero.";
                return false;
            }

            if (amount > 10_000_000m)
            {
                message = "Amount exceeds the maximum single transaction limit (Rs 10,000,000).";
                return false;
            }

            decimal roundedAmount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);

            BankTransaction newTx = new BankTransaction
            {
                Type = cleanType,
                Description = cleanDescription,
                Amount = roundedAmount,
                Date = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt")
            };

            // Insert newest transaction at the top of history for convenient viewing
            transactions.Insert(0, newTx);
            SaveTransactions(transactions);

            message = $"{cleanType} of {FormatCurrency(roundedAmount)} recorded successfully.";
            return true;
        }

        /// <summary>
        /// Removes a transaction entry by ID and updates the saved JSON file.
        /// </summary>
        public bool RemoveTransactionById(List<BankTransaction> transactions, string? transactionId, out string message)
        {
            if (string.IsNullOrWhiteSpace(transactionId))
            {
                message = "Please select a transaction from the history table to remove.";
                return false;
            }

            for (int i = 0; i < transactions.Count; i++)
            {
                if (transactions[i].Id == transactionId)
                {
                    BankTransaction removed = transactions[i];
                    transactions.RemoveAt(i);
                    SaveTransactions(transactions);
                    message = $"Removed {removed.Type.ToLowerInvariant()} entry \"{removed.Description}\".";
                    return true;
                }
            }

            message = "Could not find the selected transaction.";
            return false;
        }

        // =====================================================================
        // 3. CAFÉ MODULE METHODS (Menu, Temporary Order Cart, Bill & Receipt)
        // =====================================================================

        /// <summary>
        /// Returns the curated UOL CITY Café menu of food and drinks.
        /// </summary>
        public List<CafeMenuItem> GetDefaultCafeMenu()
        {
            return new List<CafeMenuItem>
            {
                new CafeMenuItem { Name = "Espresso Shot", Category = "Drinks", Price = 220m },
                new CafeMenuItem { Name = "Hot Cappuccino", Category = "Drinks", Price = 320m },
                new CafeMenuItem { Name = "Karak Cardamom Chai", Category = "Drinks", Price = 150m },
                new CafeMenuItem { Name = "Fresh Mint Margarita", Category = "Drinks", Price = 240m },
                new CafeMenuItem { Name = "Grilled Club Sandwich", Category = "Food", Price = 450m },
                new CafeMenuItem { Name = "Crispy Zinger Burger", Category = "Food", Price = 520m },
                new CafeMenuItem { Name = "Chicken Alfredo Pasta", Category = "Food", Price = 650m },
                new CafeMenuItem { Name = "Warm Fudge Brownie", Category = "Food", Price = 280m }
            };
        }

        /// <summary>
        /// Validates menu item selection and quantity, then adds or updates the item in the order list.
        /// </summary>
        public bool TryAddOrderItem(
            List<CafeOrderItem> currentOrder,
            CafeMenuItem? selectedMenuItem,
            string? quantityText,
            out string message)
        {
            if (selectedMenuItem == null)
            {
                message = "Please select a food or drink item from the menu first.";
                return false;
            }

            string cleanQty = (quantityText ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(cleanQty) || !int.TryParse(cleanQty, out int quantity))
            {
                message = "Please enter a valid whole-number quantity (for example, 1 or 2).";
                return false;
            }

            if (quantity <= 0)
            {
                message = "Quantity must be at least 1.";
                return false;
            }

            if (quantity > 50)
            {
                message = "Maximum quantity per addition is 50 items.";
                return false;
            }

            // If item already exists in current order, increase its quantity
            foreach (CafeOrderItem existing in currentOrder)
            {
                if (string.Equals(existing.ItemName, selectedMenuItem.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (existing.Quantity + quantity > 99)
                    {
                        message = "Total quantity for a single item cannot exceed 99.";
                        return false;
                    }

                    existing.Quantity += quantity;
                    message = $"Updated {existing.ItemName} quantity to {existing.Quantity}.";
                    return true;
                }
            }

            currentOrder.Add(new CafeOrderItem
            {
                ItemName = selectedMenuItem.Name,
                UnitPrice = selectedMenuItem.Price,
                Quantity = quantity
            });

            message = $"Added {quantity} x {selectedMenuItem.Name} to your order.";
            return true;
        }

        /// <summary>
        /// Calculates the grand total of all items currently in the Café order.
        /// </summary>
        public decimal CalculateOrderTotal(List<CafeOrderItem> currentOrder)
        {
            decimal total = 0m;
            foreach (CafeOrderItem item in currentOrder)
            {
                total += item.LineTotal;
            }
            return total;
        }

        /// <summary>
        /// Generates a clean, formatted plain-text receipt suitable for live demonstration.
        /// </summary>
        public string GenerateReceiptText(List<CafeOrderItem> currentOrder, int orderNumber)
        {
            StringBuilder sb = new StringBuilder();
            string divider = new string('-', 44);
            string doubleDivider = new string('=', 44);

            sb.AppendLine(doubleDivider);
            sb.AppendLine("              UOL CITY CAFÉ");
            sb.AppendLine("            Designed to Evolve");
            sb.AppendLine(doubleDivider);
            sb.AppendLine($"Order No : #{orderNumber:D4}");
            sb.AppendLine($"Date     : {DateTime.Now:yyyy-MM-dd hh:mm tt}");
            sb.AppendLine(divider);
            sb.AppendLine($"{"Item",-22} {"Qty",4} {"Amount",16}");
            sb.AppendLine(divider);

            int totalUnits = 0;
            foreach (CafeOrderItem item in currentOrder)
            {
                string shortName = item.ItemName.Length > 22
                    ? item.ItemName.Substring(0, 19) + "..."
                    : item.ItemName;

                string lineAmount = FormatCurrency(item.LineTotal);
                sb.AppendLine($"{shortName,-22} {item.Quantity,4} {lineAmount,16}");
                totalUnits += item.Quantity;
            }

            decimal totalBill = CalculateOrderTotal(currentOrder);

            sb.AppendLine(divider);
            sb.AppendLine($"Total Items : {totalUnits}");
            sb.AppendLine($"TOTAL BILL  : {FormatCurrency(totalBill)}");
            sb.AppendLine(doubleDivider);
            sb.AppendLine("   Thank you for visiting UOL CITY Café!");
            sb.AppendLine(doubleDivider);

            return sb.ToString();
        }

        // =====================================================================
        // 4. BUS / TRANSPORT HUB METHODS (Add, Remove, Validate Time, JSON)
        // =====================================================================

        public List<BusSchedule> LoadBusSchedules()
        {
            return LoadJsonList(_busSchedulesFilePath, CreateStarterBusSchedules);
        }

        public void SaveBusSchedules(List<BusSchedule> schedules)
        {
            SaveJsonList(_busSchedulesFilePath, schedules);
        }

        /// <summary>
        /// Validates bus number, route name, destination, and departure time,
        /// then adds a new BusSchedule entry and saves to JSON.
        /// </summary>
        public bool TryAddBusSchedule(
            List<BusSchedule> schedules,
            string? busNumber,
            string? routeName,
            string? destination,
            string? departureTimeText,
            out string message)
        {
            string cleanBusNo = (busNumber ?? string.Empty).Trim().ToUpperInvariant();
            string cleanRoute = (routeName ?? string.Empty).Trim();
            string cleanDestination = (destination ?? string.Empty).Trim();
            string cleanTimeInput = (departureTimeText ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(cleanBusNo))
            {
                message = "Please enter a Bus Number or Identifier (for example, UOL-04).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cleanRoute))
            {
                message = "Please enter a Route Name (for example, Route D - City Express).";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cleanDestination))
            {
                message = "Please enter a Destination name.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(cleanTimeInput))
            {
                message = "Please enter a Departure Time (for example, 08:30 AM or 14:30).";
                return false;
            }

            if (!TryNormalizeDepartureTime(cleanTimeInput, out string formattedTime))
            {
                message = "Invalid departure time. Use a valid format like 08:30 AM or 14:30.";
                return false;
            }

            BusSchedule newEntry = new BusSchedule
            {
                BusNumber = cleanBusNo,
                RouteName = cleanRoute,
                Destination = cleanDestination,
                DepartureTime = formattedTime
            };

            schedules.Add(newEntry);
            SaveBusSchedules(schedules);

            message = $"Bus {cleanBusNo} to {cleanDestination} ({formattedTime}) added.";
            return true;
        }

        /// <summary>
        /// Removes a bus schedule entry by ID and updates the saved JSON file.
        /// </summary>
        public bool RemoveBusScheduleById(List<BusSchedule> schedules, string? scheduleId, out string message)
        {
            if (string.IsNullOrWhiteSpace(scheduleId))
            {
                message = "Please select a bus schedule entry from the table to remove.";
                return false;
            }

            for (int i = 0; i < schedules.Count; i++)
            {
                if (schedules[i].Id == scheduleId)
                {
                    BusSchedule removed = schedules[i];
                    schedules.RemoveAt(i);
                    SaveBusSchedules(schedules);
                    message = $"Removed schedule for Bus {removed.BusNumber} ({removed.Destination}).";
                    return true;
                }
            }

            message = "Could not find the selected schedule entry.";
            return false;
        }

        /// <summary>
        /// Parses common time formats (e.g., "08:30 AM", "8:30 pm", "14:15") into "hh:mm tt".
        /// </summary>
        public static bool TryNormalizeDepartureTime(string input, out string formattedTime)
        {
            string[] acceptedFormats =
            {
                "h:mm tt", "hh:mm tt", "H:mm", "HH:mm",
                "h:mmtt", "hh:mmtt", "h tt", "hh tt"
            };

            if (DateTime.TryParseExact(
                input.Trim(),
                acceptedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.NoCurrentDateDefault,
                out DateTime parsedExact))
            {
                formattedTime = parsedExact.ToString("hh:mm tt", CultureInfo.InvariantCulture);
                return true;
            }

            if (TimeSpan.TryParse(input.Trim(), CultureInfo.InvariantCulture, out TimeSpan span) &&
                span >= TimeSpan.Zero &&
                span < TimeSpan.FromHours(24))
            {
                DateTime dt = DateTime.Today.Add(span);
                formattedTime = dt.ToString("hh:mm tt", CultureInfo.InvariantCulture);
                return true;
            }

            formattedTime = string.Empty;
            return false;
        }

        // =====================================================================
        // 5. FORMATTING & SAFE LOCAL JSON FILE HELPERS
        // =====================================================================

        /// <summary>
        /// Formats decimal monetary amounts consistently in Pakistani Rupees (Rs).
        /// </summary>
        public static string FormatCurrency(decimal amount)
        {
            if (amount < 0m)
            {
                return $"-Rs {Math.Abs(amount):N2}";
            }
            return $"Rs {amount:N2}";
        }

        private void EnsureDataDirectoryExists()
        {
            try
            {
                if (!Directory.Exists(_dataDirectory))
                {
                    Directory.CreateDirectory(_dataDirectory);
                }
            }
            catch (Exception ex)
            {
                LastStorageNotice = $"Could not create data folder: {ex.Message}";
            }
        }

        /// <summary>
        /// Safely loads a List&lt;T&gt; from a JSON file.
        /// Handles:
        /// - Missing files (first-time initialization with starter records)
        /// - Empty files (returns empty list without overwriting user intent)
        /// - Invalid or corrupted JSON (backs up corrupted file and returns empty list safely)
        /// - Existing valid JSON (returns saved user records intact)
        /// </summary>
        private List<T> LoadJsonList<T>(string filePath, Func<List<T>> starterFactory)
        {
            EnsureDataDirectoryExists();

            try
            {
                // Case 1: Missing file -> First-time initialization
                if (!File.Exists(filePath))
                {
                    List<T> starterItems = starterFactory();
                    SaveJsonList(filePath, starterItems);
                    return starterItems;
                }

                string rawJson = File.ReadAllText(filePath);

                // Case 2: Empty file -> Return empty list cleanly
                if (string.IsNullOrWhiteSpace(rawJson))
                {
                    return new List<T>();
                }

                // Case 3: Existing JSON -> Deserialize saved user data
                List<T>? loaded = JsonSerializer.Deserialize<List<T>>(rawJson, JsonOptions);
                return loaded ?? new List<T>();
            }
            catch (JsonException)
            {
                // Case 4: Corrupted JSON -> Preserve a backup copy instead of crashing
                BackupCorruptedFile(filePath);
                LastStorageNotice = $"Recovered from unreadable JSON in {Path.GetFileName(filePath)} (backup saved).";
                List<T> cleanList = new List<T>();
                SaveJsonList(filePath, cleanList);
                return cleanList;
            }
            catch (Exception ex)
            {
                LastStorageNotice = $"Storage read warning ({Path.GetFileName(filePath)}): {ex.Message}";
                return new List<T>();
            }
        }

        private void SaveJsonList<T>(string filePath, List<T> items)
        {
            EnsureDataDirectoryExists();

            try
            {
                string json = JsonSerializer.Serialize(items, JsonOptions);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                LastStorageNotice = $"Storage save warning ({Path.GetFileName(filePath)}): {ex.Message}";
            }
        }

        private static void BackupCorruptedFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string backupPath = $"{filePath}.{DateTime.Now:yyyyMMddHHmmss}.corrupt.bak";
                    File.Copy(filePath, backupPath, overwrite: true);
                }
            }
            catch
            {
                // Ignore secondary backup errors so startup remains crash-free
            }
        }

        // =====================================================================
        // 6. FIRST-TIME INITIALIZATION STARTER RECORDS
        // =====================================================================

        private static List<BookItem> CreateStarterBooks()
        {
            return new List<BookItem>
            {
                new BookItem { Title = "C# 12 in a Nutshell", Author = "Joseph Albahari", AddedDate = "2026-10-15" },
                new BookItem { Title = "Clean Code", Author = "Robert C. Martin", AddedDate = "2026-10-22" },
                new BookItem { Title = "The Pragmatic Programmer", Author = "Andrew Hunt & David Thomas", AddedDate = "2026-11-01" }
            };
        }

        private static List<BankTransaction> CreateStarterTransactions()
        {
            return new List<BankTransaction>
            {
                new BankTransaction
                {
                    Type = "Expense",
                    Description = "Programming Textbooks & Lab Notes",
                    Amount = 1200m,
                    Date = "2026-11-03 11:15 AM"
                },
                new BankTransaction
                {
                    Type = "Income",
                    Description = "Monthly Student Allowance",
                    Amount = 5000m,
                    Date = "2026-11-01 09:00 AM"
                }
            };
        }

        private static List<BusSchedule> CreateStarterBusSchedules()
        {
            return new List<BusSchedule>
            {
                new BusSchedule
                {
                    BusNumber = "UOL-01",
                    RouteName = "Route A - Morning Shuttle",
                    Destination = "Sargodha City Center",
                    DepartureTime = "08:00 AM"
                },
                new BusSchedule
                {
                    BusNumber = "UOL-02",
                    RouteName = "Route B - Campus Connector",
                    Destination = "Satellite Town Block C",
                    DepartureTime = "01:30 PM"
                },
                new BusSchedule
                {
                    BusNumber = "UOL-03",
                    RouteName = "Route C - Evening Express",
                    Destination = "University Road Terminal",
                    DepartureTime = "04:45 PM"
                }
            };
        }
    }
}
