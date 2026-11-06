using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UOLCity
{
    /// <summary>
    /// Bank Module: Personal wallet management interface.
    /// Calculates the live wallet balance directly from saved Income and Expense records,
    /// validates monetary inputs, displays transaction history, and persists to JSON.
    /// </summary>
    public sealed class BankForm : Form
    {
        private readonly TownDataStore _dataStore;
        private readonly List<BankTransaction> _transactions;

        // Summary labels (calculated from real transaction data, never hardcoded)
        private Label _lblBalanceValue = null!;
        private Label _lblIncomeValue = null!;
        private Label _lblExpenseValue = null!;

        // Form inputs
        private TextBox _txtDescription = null!;
        private TextBox _txtAmount = null!;
        private Label _lblStatusBanner = null!;
        private Label _lblHistoryCount = null!;

        // History grid & empty state
        private DataGridView _gridTransactions = null!;
        private Panel _emptyStatePanel = null!;

        public BankForm(TownDataStore dataStore)
        {
            _dataStore = dataStore;
            _transactions = _dataStore.LoadTransactions();

            InitializeWindow();
            BuildUserInterface();
            RefreshWalletDisplay();
        }

        private void InitializeWindow()
        {
            Text = "UOL CITY — Bank (Personal Wallet)";
            ClientSize = new Size(1080, 660);
            MinimumSize = new Size(940, 580);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.WarmOffWhite;
            Font = UiTheme.GetBodyFont(9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;

            KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };
        }

        private void BuildUserInterface()
        {
            Panel header = UiTheme.CreateModuleHeader(
                "UOL CITY BANK",
                "Personal Wallet Management — Track income, expenses, and live wallet balance",
                LocationType.Bank,
                (_, _) => Close());

            // Top summary row with 3 calculated cards: Current Balance | Total Income | Total Expenses
            TableLayoutPanel summaryStrip = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 112,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(16, 14, 16, 4),
                BackColor = UiTheme.WarmOffWhite
            };
            summaryStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            summaryStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31f));
            summaryStrip.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 31f));

            Panel balanceCard = CreateSummaryCard(
                "Current Wallet Balance",
                out _lblBalanceValue,
                UiTheme.DeepNavy,
                Color.White,
                UiTheme.GoldenYellow);

            Panel incomeCard = CreateSummaryCard(
                "Total Recorded Income",
                out _lblIncomeValue,
                UiTheme.CardWhite,
                UiTheme.TextMuted,
                UiTheme.MutedGreen);

            Panel expenseCard = CreateSummaryCard(
                "Total Recorded Expenses",
                out _lblExpenseValue,
                UiTheme.CardWhite,
                UiTheme.TextMuted,
                UiTheme.DangerRed);

            summaryStrip.Controls.Add(balanceCard, 0, 0);
            summaryStrip.Controls.Add(incomeCard, 1, 0);
            summaryStrip.Controls.Add(expenseCard, 2, 0);

            // Main lower area: Left (Record Transaction) | Right (Transaction History)
            TableLayoutPanel mainSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16, 4, 16, 16),
                BackColor = UiTheme.WarmOffWhite
            };
            mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            mainSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            mainSplit.Controls.Add(BuildTransactionFormCard(), 0, 0);
            mainSplit.Controls.Add(BuildHistoryCard(), 1, 0);

            Controls.Add(mainSplit);
            Controls.Add(summaryStrip);
            Controls.Add(header);
        }

        private static Panel CreateSummaryCard(
            string labelText,
            out Label valueLabel,
            Color cardBackground,
            Color captionColor,
            Color valueColor)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = cardBackground,
                Padding = new Padding(18, 14, 18, 12),
                Margin = new Padding(8, 0, 8, 0)
            };
            UiTheme.EnableDoubleBuffering(card);
            card.Paint += (_, e) =>
            {
                Rectangle border = new Rectangle(0, 0, card.ClientSize.Width - 1, card.ClientSize.Height - 1);
                using Pen pen = new Pen(UiTheme.SoftBorder, 1f);
                e.Graphics.DrawRectangle(pen, border);
            };

            Label lblCaption = new Label
            {
                Text = labelText,
                Font = UiTheme.GetBodyFont(9f, FontStyle.Bold),
                ForeColor = captionColor,
                AutoSize = true,
                Dock = DockStyle.Top
            };

            valueLabel = new Label
            {
                Text = "Rs 0.00",
                Font = UiTheme.GetBodyFont(18f, FontStyle.Bold),
                ForeColor = valueColor,
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblCaption);
            return card;
        }

        private Panel BuildTransactionFormCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(20));
            card.Dock = DockStyle.Fill;

            TableLayoutPanel formLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1
            };
            formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label lblHeading = new Label
            {
                Text = "Record a Transaction",
                Font = UiTheme.GetBodyFont(13f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            Label lblSub = new Label
            {
                Text = "Enter a short note and monetary amount, then record it as Income or Expense.",
                Font = UiTheme.GetBodyFont(9f),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                MaximumSize = new Size(310, 0),
                Margin = new Padding(0, 0, 0, 18)
            };

            Label lblDesc = new Label
            {
                Text = "Description / Category *",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            _txtDescription = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10.5f),
                PlaceholderText = "e.g., Monthly Allowance or Lab Printing",
                Margin = new Padding(0, 0, 0, 16)
            };

            Label lblAmount = new Label
            {
                Text = "Amount (Rs) *",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            _txtAmount = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10.5f),
                PlaceholderText = "e.g., 1500.00",
                Margin = new Padding(0, 0, 0, 20)
            };

            // Side-by-side Income & Expense buttons
            TableLayoutPanel buttonRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 16)
            };
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            buttonRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

            Button btnAddIncome = new Button
            {
                Text = "+  Add Income",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 6, 0)
            };
            UiTheme.StyleActionButton(btnAddIncome, UiTheme.MutedGreen, Color.White);
            btnAddIncome.Click += (_, _) => HandleTransaction("Income");

            Button btnAddExpense = new Button
            {
                Text = "−  Add Expense",
                Dock = DockStyle.Fill,
                Margin = new Padding(6, 0, 0, 0)
            };
            UiTheme.StyleActionButton(btnAddExpense, UiTheme.DangerRed, Color.White);
            btnAddExpense.Click += (_, _) => HandleTransaction("Expense");

            buttonRow.Controls.Add(btnAddIncome, 0, 0);
            buttonRow.Controls.Add(btnAddExpense, 1, 0);

            _lblStatusBanner = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 38),
                Padding = new Padding(10, 8, 10, 8),
                Font = UiTheme.GetBodyFont(9f, FontStyle.Bold),
                Visible = false
            };

            formLayout.Controls.Add(lblHeading);
            formLayout.Controls.Add(lblSub);
            formLayout.Controls.Add(lblDesc);
            formLayout.Controls.Add(_txtDescription);
            formLayout.Controls.Add(lblAmount);
            formLayout.Controls.Add(_txtAmount);
            formLayout.Controls.Add(buttonRow);
            formLayout.Controls.Add(_lblStatusBanner);

            card.Controls.Add(formLayout);
            return card;
        }

        private Panel BuildHistoryCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(20));
            card.Dock = DockStyle.Fill;

            TableLayoutPanel headerRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                ColumnCount = 2
            };
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            Label lblHeading = new Label
            {
                Text = "Transaction History",
                Font = UiTheme.GetBodyFont(13f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            _lblHistoryCount = new Label
            {
                Text = "0 Transactions",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            headerRow.Controls.Add(lblHeading, 0, 0);
            headerRow.Controls.Add(_lblHistoryCount, 1, 0);

            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(0, 8, 0, 0)
            };

            Button btnRemoveTx = new Button
            {
                Text = "Delete Selected Entry",
                Dock = DockStyle.Right,
                Width = 185
            };
            UiTheme.StyleActionButton(btnRemoveTx, Color.FromArgb(232, 237, 245), UiTheme.DeepNavy);
            btnRemoveTx.Click += (_, _) => HandleRemoveSelectedTransaction();
            bottomBar.Controls.Add(btnRemoveTx);

            Panel gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 4)
            };

            _gridTransactions = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UiTheme.StyleDataGridView(_gridTransactions);

            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                Visible = false
            });
            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Date",
                HeaderText = "Date & Time",
                FillWeight = 27f
            });
            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Type",
                HeaderText = "Type",
                FillWeight = 16f
            });
            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Description",
                FillWeight = 35f
            });
            _gridTransactions.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Amount",
                HeaderText = "Amount",
                FillWeight = 22f,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold)
                }
            });

            // Color-code Income (green) vs Expense (red) rows for clarity
            _gridTransactions.CellFormatting += (_, e) =>
            {
                if (e.RowIndex < 0 || e.RowIndex >= _gridTransactions.Rows.Count)
                {
                    return;
                }

                string? txType = _gridTransactions.Rows[e.RowIndex].Cells["Type"].Value?.ToString();
                string colName = _gridTransactions.Columns[e.ColumnIndex].Name;

                if ((colName == "Type" || colName == "Amount") && e.CellStyle != null)
                {
                    bool isIncome = string.Equals(txType, "Income", StringComparison.OrdinalIgnoreCase);
                    e.CellStyle.ForeColor = isIncome ? UiTheme.MutedGreen : UiTheme.DangerRed;
                    e.CellStyle.SelectionForeColor = isIncome
                        ? Color.FromArgb(16, 108, 72)
                        : Color.FromArgb(162, 34, 42);
                }
            };

            _emptyStatePanel = BuildEmptyStatePanel();

            gridHost.Controls.Add(_emptyStatePanel);
            gridHost.Controls.Add(_gridTransactions);

            card.Controls.Add(gridHost);
            card.Controls.Add(bottomBar);
            card.Controls.Add(headerRow);

            return card;
        }

        private static Panel BuildEmptyStatePanel()
        {
            Panel empty = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 253),
                Visible = false
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            Label title = new Label
            {
                Text = "No Wallet Transactions Yet",
                Font = UiTheme.GetBodyFont(12f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(12, 0, 12, 6)
            };

            Label hint = new Label
            {
                Text = "Use the form on the left to record an Income or Expense transaction.",
                Font = UiTheme.GetBodyFont(9.5f),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.None
            };

            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(hint, 0, 2);
            empty.Controls.Add(layout);

            return empty;
        }

        // =====================================================================
        // EVENT HANDLERS & BALANCE RECALCULATION
        // =====================================================================

        private void HandleTransaction(string transactionType)
        {
            bool ok = _dataStore.TryAddTransaction(
                _transactions,
                transactionType,
                _txtDescription.Text,
                _txtAmount.Text,
                out string message);

            UiTheme.ShowStatusBanner(_lblStatusBanner, message, isSuccess: ok);

            if (!ok)
            {
                if (string.IsNullOrWhiteSpace(_txtDescription.Text))
                {
                    _txtDescription.Focus();
                }
                else
                {
                    _txtAmount.Focus();
                    _txtAmount.SelectAll();
                }
                return;
            }

            _txtDescription.Clear();
            _txtAmount.Clear();
            _txtDescription.Focus();

            RefreshWalletDisplay();
        }

        private void HandleRemoveSelectedTransaction()
        {
            if (_gridTransactions.SelectedRows.Count == 0)
            {
                UiTheme.ShowStatusBanner(
                    _lblStatusBanner,
                    "Please select a transaction entry in the history table first.",
                    isSuccess: false);
                return;
            }

            string? id = _gridTransactions.SelectedRows[0].Cells["Id"].Value?.ToString();
            string desc = _gridTransactions.SelectedRows[0].Cells["Description"].Value?.ToString() ?? "entry";

            DialogResult confirm = MessageBox.Show(
                this,
                $"Delete transaction \"{desc}\" and recalculate wallet balance?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            bool removed = _dataStore.RemoveTransactionById(_transactions, id, out string message);
            UiTheme.ShowStatusBanner(_lblStatusBanner, message, isSuccess: removed);
            RefreshWalletDisplay();
        }

        private void RefreshWalletDisplay()
        {
            // Calculate totals dynamically from saved records (never hardcoded)
            decimal balance = _dataStore.CalculateWalletBalance(_transactions);
            decimal totalIncome = _dataStore.CalculateTotalIncome(_transactions);
            decimal totalExpenses = _dataStore.CalculateTotalExpenses(_transactions);

            _lblBalanceValue.Text = TownDataStore.FormatCurrency(balance);
            _lblBalanceValue.ForeColor = balance >= 0m
                ? UiTheme.GoldenYellow
                : Color.FromArgb(255, 132, 132);

            _lblIncomeValue.Text = $"+ {TownDataStore.FormatCurrency(totalIncome)}";
            _lblExpenseValue.Text = $"− {TownDataStore.FormatCurrency(totalExpenses)}";

            _lblHistoryCount.Text = _transactions.Count == 1
                ? "1 Transaction"
                : $"{_transactions.Count} Transactions";

            _gridTransactions.Rows.Clear();
            foreach (BankTransaction tx in _transactions)
            {
                bool isIncome = string.Equals(tx.Type, "Income", StringComparison.OrdinalIgnoreCase);
                string formattedAmount = isIncome
                    ? $"+ {TownDataStore.FormatCurrency(tx.Amount)}"
                    : $"− {TownDataStore.FormatCurrency(tx.Amount)}";

                _gridTransactions.Rows.Add(tx.Id, tx.Date, tx.Type, tx.Description, formattedAmount);
            }

            if (_transactions.Count == 0)
            {
                _emptyStatePanel.Visible = true;
                _emptyStatePanel.BringToFront();
                _gridTransactions.Visible = false;
            }
            else
            {
                _emptyStatePanel.Visible = false;
                _gridTransactions.Visible = true;
                _gridTransactions.BringToFront();
            }
        }
    }
}
