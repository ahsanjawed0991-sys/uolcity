using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UOLCity
{
    /// <summary>
    /// Café Module: Campus food and drinks ordering interface.
    /// Allows selecting menu items with validated quantities, calculates the live total bill,
    /// generates a readable formatted receipt, and supports completing or clearing an order.
    /// </summary>
    public sealed class CafeForm : Form
    {
        private readonly TownDataStore _dataStore;
        private readonly List<CafeMenuItem> _menuItems;
        private readonly List<CafeOrderItem> _currentOrder;
        private int _nextOrderNumber = 1;

        // Menu controls
        private DataGridView _gridMenu = null!;
        private TextBox _txtQuantity = null!;
        private Label _lblStatusBanner = null!;

        // Order cart controls
        private DataGridView _gridOrder = null!;
        private Panel _emptyOrderPanel = null!;
        private Label _lblTotalBill = null!;
        private Label _lblItemCount = null!;

        // Receipt preview control
        private TextBox _txtReceipt = null!;

        public CafeForm(TownDataStore dataStore)
        {
            _dataStore = dataStore;
            _menuItems = _dataStore.GetDefaultCafeMenu();
            _currentOrder = new List<CafeOrderItem>();

            InitializeWindow();
            BuildUserInterface();
            PopulateMenuGrid();
            RefreshOrderDisplay();
            ShowDefaultReceiptPlaceholder();
        }

        private void InitializeWindow()
        {
            Text = "UOL CITY — Café (Food & Drinks Ordering)";
            ClientSize = new Size(1160, 670);
            MinimumSize = new Size(1000, 590);
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
                "UOL CITY CAFÉ",
                "Food & Drinks Ordering — Build an order, calculate the bill, and generate a receipt",
                LocationType.Cafe,
                (_, _) => Close());

            // Three-column layout: 1. Menu | 2. Selected Order & Total | 3. Receipt Output
            TableLayoutPanel mainColumns = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Padding = new Padding(14),
                BackColor = UiTheme.WarmOffWhite
            };
            mainColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            mainColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            mainColumns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            mainColumns.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            mainColumns.Controls.Add(BuildMenuCard(), 0, 0);
            mainColumns.Controls.Add(BuildOrderCard(), 1, 0);
            mainColumns.Controls.Add(BuildReceiptCard(), 2, 0);

            Controls.Add(mainColumns);
            Controls.Add(header);
        }

        private Panel BuildMenuCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(16));
            card.Dock = DockStyle.Fill;

            Label lblTitle = new Label
            {
                Text = "1. Café Menu",
                Font = UiTheme.GetBodyFont(12.5f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                Dock = DockStyle.Top,
                Height = 28
            };

            Label lblHint = new Label
            {
                Text = "Select an item below, choose a quantity, and click Add to Order.",
                Font = UiTheme.GetBodyFont(8.8f),
                ForeColor = UiTheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 26
            };

            // Bottom area: Quantity input + Add button + Status feedback
            Panel controlsBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 148,
                Padding = new Padding(0, 10, 0, 0)
            };

            TableLayoutPanel qtyRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                ColumnCount = 4
            };
            qtyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            qtyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38f));
            qtyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62f));
            qtyRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38f));

            Label lblQty = new Label
            {
                Text = "Quantity:",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            Button btnMinus = new Button { Text = "−", Dock = DockStyle.Fill };
            UiTheme.StyleActionButton(btnMinus, Color.FromArgb(230, 236, 245), UiTheme.DeepNavy);
            btnMinus.Click += (_, _) => StepQuantity(-1);

            _txtQuantity = new TextBox
            {
                Text = "1",
                TextAlign = HorizontalAlignment.Center,
                Font = UiTheme.GetBodyFont(10.5f, FontStyle.Bold),
                Anchor = AnchorStyles.Left | AnchorStyles.Right
            };

            Button btnPlus = new Button { Text = "+", Dock = DockStyle.Fill };
            UiTheme.StyleActionButton(btnPlus, Color.FromArgb(230, 236, 245), UiTheme.DeepNavy);
            btnPlus.Click += (_, _) => StepQuantity(1);

            qtyRow.Controls.Add(lblQty, 0, 0);
            qtyRow.Controls.Add(btnMinus, 1, 0);
            qtyRow.Controls.Add(_txtQuantity, 2, 0);
            qtyRow.Controls.Add(btnPlus, 3, 0);

            Button btnAdd = new Button
            {
                Text = "+  Add Selected Item to Order",
                Dock = DockStyle.Top,
                Height = 42
            };
            UiTheme.StyleActionButton(btnAdd, UiTheme.MutedGreen, Color.White);
            btnAdd.Click += (_, _) => HandleAddItemToOrder();

            Panel gap = new Panel { Dock = DockStyle.Top, Height = 8 };
            Panel gap2 = new Panel { Dock = DockStyle.Top, Height = 8 };

            _lblStatusBanner = new Label
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(8, 6, 8, 6),
                Font = UiTheme.GetBodyFont(8.8f, FontStyle.Bold),
                Visible = false
            };

            controlsBottom.Controls.Add(_lblStatusBanner);
            controlsBottom.Controls.Add(gap2);
            controlsBottom.Controls.Add(btnAdd);
            controlsBottom.Controls.Add(gap);
            controlsBottom.Controls.Add(qtyRow);

            // Menu grid
            Panel menuGridHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 6, 0, 4)
            };

            _gridMenu = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UiTheme.StyleDataGridView(_gridMenu);

            _gridMenu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Menu Item",
                FillWeight = 50f
            });
            _gridMenu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Category",
                HeaderText = "Type",
                FillWeight = 22f
            });
            _gridMenu.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Price",
                HeaderText = "Price",
                FillWeight = 28f,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold)
                }
            });

            // Double-clicking a menu row adds it quickly
            _gridMenu.CellDoubleClick += (_, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    HandleAddItemToOrder();
                }
            };

            menuGridHost.Controls.Add(_gridMenu);

            card.Controls.Add(menuGridHost);
            card.Controls.Add(controlsBottom);
            card.Controls.Add(lblHint);
            card.Controls.Add(lblTitle);

            return card;
        }

        private Panel BuildOrderCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(16));
            card.Dock = DockStyle.Fill;

            TableLayoutPanel topHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                ColumnCount = 2
            };
            topHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            topHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            Label lblTitle = new Label
            {
                Text = "2. Current Order",
                Font = UiTheme.GetBodyFont(12.5f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            _lblItemCount = new Label
            {
                Text = "0 Items",
                Font = UiTheme.GetBodyFont(9f, FontStyle.Bold),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            topHeader.Controls.Add(lblTitle, 0, 0);
            topHeader.Controls.Add(_lblItemCount, 1, 0);

            // Bottom totals & order buttons
            Panel orderFooter = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 166,
                Padding = new Padding(0, 8, 0, 0)
            };

            // Total bill highlight strip (DPI-safe TableLayoutPanel alignment)
            TableLayoutPanel totalBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = UiTheme.DeepNavy,
                Padding = new Padding(14, 0, 14, 0)
            };
            totalBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
            totalBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
            totalBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Label lblTotalCaption = new Label
            {
                Text = "TOTAL BILL",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            _lblTotalBill = new Label
            {
                Text = "Rs 0.00",
                Font = UiTheme.GetBodyFont(13f, FontStyle.Bold),
                ForeColor = UiTheme.GoldenYellow,
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            totalBar.Controls.Add(lblTotalCaption, 0, 0);
            totalBar.Controls.Add(_lblTotalBill, 1, 0);

            // Action buttons grid
            TableLayoutPanel actionsGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 98,
                ColumnCount = 2,
                RowCount = 2
            };
            actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            actionsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            actionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            actionsGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            Button btnGenerateReceipt = new Button
            {
                Text = "Generate Receipt",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 4, 4)
            };
            UiTheme.StyleActionButton(btnGenerateReceipt, UiTheme.SurfaceNavy, Color.White);
            btnGenerateReceipt.Click += (_, _) => HandleGenerateReceipt();

            Button btnCompleteOrder = new Button
            {
                Text = "✓  Complete Order",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 4, 0, 4)
            };
            UiTheme.StyleActionButton(btnCompleteOrder, UiTheme.MutedGreen, Color.White);
            btnCompleteOrder.Click += (_, _) => HandleCompleteOrder();

            Button btnRemoveLine = new Button
            {
                Text = "Remove Selected",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 4, 4, 0)
            };
            UiTheme.StyleActionButton(btnRemoveLine, Color.FromArgb(230, 236, 245), UiTheme.DeepNavy);
            btnRemoveLine.Click += (_, _) => HandleRemoveOrderLine();

            Button btnClearOrder = new Button
            {
                Text = "Clear Order",
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 4, 0, 0)
            };
            UiTheme.StyleActionButton(btnClearOrder, UiTheme.DangerRed, Color.White);
            btnClearOrder.Click += (_, _) => HandleClearOrder();

            actionsGrid.Controls.Add(btnGenerateReceipt, 0, 0);
            actionsGrid.Controls.Add(btnCompleteOrder, 1, 0);
            actionsGrid.Controls.Add(btnRemoveLine, 0, 1);
            actionsGrid.Controls.Add(btnClearOrder, 1, 1);

            orderFooter.Controls.Add(actionsGrid);
            orderFooter.Controls.Add(totalBar);

            // Order items grid + empty state
            Panel cartHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 4)
            };

            _gridOrder = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UiTheme.StyleDataGridView(_gridOrder);

            _gridOrder.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ItemName",
                HeaderText = "Item",
                FillWeight = 44f
            });
            _gridOrder.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Qty",
                HeaderText = "Qty",
                FillWeight = 16f
            });
            _gridOrder.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "UnitPrice",
                HeaderText = "Unit",
                FillWeight = 20f
            });
            _gridOrder.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Subtotal",
                HeaderText = "Subtotal",
                FillWeight = 24f,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleRight,
                    Font = UiTheme.GetBodyFont(9f, FontStyle.Bold)
                }
            });

            _emptyOrderPanel = BuildEmptyOrderPanel();

            cartHost.Controls.Add(_emptyOrderPanel);
            cartHost.Controls.Add(_gridOrder);

            card.Controls.Add(cartHost);
            card.Controls.Add(orderFooter);
            card.Controls.Add(topHeader);

            return card;
        }

        private Panel BuildReceiptCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(16));
            card.Dock = DockStyle.Fill;

            Label lblTitle = new Label
            {
                Text = "3. Order Receipt",
                Font = UiTheme.GetBodyFont(12.5f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                Dock = DockStyle.Top,
                Height = 28
            };

            Label lblHint = new Label
            {
                Text = "Preview or complete an order to generate a formatted campus café receipt.",
                Font = UiTheme.GetBodyFont(8.8f),
                ForeColor = UiTheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 32
            };

            _txtReceipt = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(252, 252, 249),
                ForeColor = UiTheme.DarkCharcoal,
                BorderStyle = BorderStyle.FixedSingle,
                Font = UiTheme.GetMonoFont(9.2f),
                WordWrap = false
            };

            card.Controls.Add(_txtReceipt);
            card.Controls.Add(lblHint);
            card.Controls.Add(lblTitle);

            return card;
        }

        private static Panel BuildEmptyOrderPanel()
        {
            Panel empty = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 253),
                Visible = true
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
                Text = "No Items in Current Order",
                Font = UiTheme.GetBodyFont(11f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(8, 0, 8, 6)
            };

            Label hint = new Label
            {
                Text = "Select food or drinks from the Café Menu on the left and click Add.",
                Font = UiTheme.GetBodyFont(9f),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter,
                MaximumSize = new Size(280, 0)
            };

            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(hint, 0, 2);
            empty.Controls.Add(layout);

            return empty;
        }

        // =====================================================================
        // CAFÉ ORDER LOGIC & EVENT HANDLERS
        // =====================================================================

        private void PopulateMenuGrid()
        {
            _gridMenu.Rows.Clear();
            foreach (CafeMenuItem item in _menuItems)
            {
                _gridMenu.Rows.Add(item.Name, item.Category, TownDataStore.FormatCurrency(item.Price));
            }

            if (_gridMenu.Rows.Count > 0)
            {
                _gridMenu.Rows[0].Selected = true;
            }
        }

        private void StepQuantity(int delta)
        {
            if (!int.TryParse(_txtQuantity.Text.Trim(), out int current))
            {
                current = 1;
            }

            int updated = Math.Clamp(current + delta, 1, 50);
            _txtQuantity.Text = updated.ToString();
        }

        private void HandleAddItemToOrder()
        {
            CafeMenuItem? selected = null;
            if (_gridMenu.SelectedRows.Count > 0)
            {
                int index = _gridMenu.SelectedRows[0].Index;
                if (index >= 0 && index < _menuItems.Count)
                {
                    selected = _menuItems[index];
                }
            }

            bool added = _dataStore.TryAddOrderItem(
                _currentOrder,
                selected,
                _txtQuantity.Text,
                out string message);

            UiTheme.ShowStatusBanner(_lblStatusBanner, message, isSuccess: added);

            if (!added)
            {
                _txtQuantity.Focus();
                _txtQuantity.SelectAll();
                return;
            }

            _txtQuantity.Text = "1";
            RefreshOrderDisplay();
            _txtReceipt.Text = _dataStore.GenerateReceiptText(_currentOrder, _nextOrderNumber);
        }

        private void HandleRemoveOrderLine()
        {
            if (_currentOrder.Count == 0 || _gridOrder.SelectedRows.Count == 0)
            {
                UiTheme.ShowStatusBanner(
                    _lblStatusBanner,
                    "Please select an item in the Current Order list to remove.",
                    isSuccess: false);
                return;
            }

            int rowIndex = _gridOrder.SelectedRows[0].Index;
            if (rowIndex >= 0 && rowIndex < _currentOrder.Count)
            {
                string name = _currentOrder[rowIndex].ItemName;
                _currentOrder.RemoveAt(rowIndex);
                UiTheme.ShowStatusBanner(_lblStatusBanner, $"Removed {name} from current order.", isSuccess: true);
                RefreshOrderDisplay();

                if (_currentOrder.Count > 0)
                {
                    _txtReceipt.Text = _dataStore.GenerateReceiptText(_currentOrder, _nextOrderNumber);
                }
                else
                {
                    ShowDefaultReceiptPlaceholder();
                }
            }
        }

        private void HandleGenerateReceipt()
        {
            if (_currentOrder.Count == 0)
            {
                UiTheme.ShowStatusBanner(
                    _lblStatusBanner,
                    "Add at least one menu item before generating a receipt.",
                    isSuccess: false);
                return;
            }

            _txtReceipt.Text = _dataStore.GenerateReceiptText(_currentOrder, _nextOrderNumber);
            UiTheme.ShowStatusBanner(
                _lblStatusBanner,
                $"Receipt preview generated for Order #{_nextOrderNumber:D4}.",
                isSuccess: true);
        }

        private void HandleCompleteOrder()
        {
            if (_currentOrder.Count == 0)
            {
                UiTheme.ShowStatusBanner(
                    _lblStatusBanner,
                    "Cannot complete an empty order. Please add menu items first.",
                    isSuccess: false);
                return;
            }

            int completedNumber = _nextOrderNumber;
            decimal finalTotal = _dataStore.CalculateOrderTotal(_currentOrder);
            string finalReceipt = _dataStore.GenerateReceiptText(_currentOrder, completedNumber);

            _txtReceipt.Text = finalReceipt;
            _nextOrderNumber++;

            // Reset session cart for the next order while keeping the printed receipt visible
            _currentOrder.Clear();
            RefreshOrderDisplay();

            UiTheme.ShowStatusBanner(
                _lblStatusBanner,
                $"Order #{completedNumber:D4} completed ({TownDataStore.FormatCurrency(finalTotal)})! Receipt ready.",
                isSuccess: true);

            MessageBox.Show(
                this,
                $"Order #{completedNumber:D4} has been completed!\r\nTotal Bill: {TownDataStore.FormatCurrency(finalTotal)}\r\n\r\nYour formatted receipt is displayed in the Order Receipt panel.",
                "Order Completed — UOL CITY Café",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void HandleClearOrder()
        {
            if (_currentOrder.Count == 0)
            {
                ShowDefaultReceiptPlaceholder();
                UiTheme.ShowStatusBanner(_lblStatusBanner, "Order tray is already empty.", isSuccess: true);
                return;
            }

            _currentOrder.Clear();
            RefreshOrderDisplay();
            ShowDefaultReceiptPlaceholder();
            UiTheme.ShowStatusBanner(_lblStatusBanner, "Current order and receipt cleared.", isSuccess: true);
        }

        private void RefreshOrderDisplay()
        {
            _gridOrder.Rows.Clear();
            int totalUnits = 0;

            foreach (CafeOrderItem item in _currentOrder)
            {
                _gridOrder.Rows.Add(
                    item.ItemName,
                    item.Quantity,
                    TownDataStore.FormatCurrency(item.UnitPrice),
                    TownDataStore.FormatCurrency(item.LineTotal));
                totalUnits += item.Quantity;
            }

            decimal totalBill = _dataStore.CalculateOrderTotal(_currentOrder);
            _lblTotalBill.Text = TownDataStore.FormatCurrency(totalBill);
            _lblItemCount.Text = totalUnits == 1 ? "1 Item" : $"{totalUnits} Items";

            if (_currentOrder.Count == 0)
            {
                _emptyOrderPanel.Visible = true;
                _emptyOrderPanel.BringToFront();
                _gridOrder.Visible = false;
            }
            else
            {
                _emptyOrderPanel.Visible = false;
                _gridOrder.Visible = true;
                _gridOrder.BringToFront();
            }
        }

        private void ShowDefaultReceiptPlaceholder()
        {
            _txtReceipt.Text =
                "============================================\r\n" +
                "              UOL CITY CAFÉ\r\n" +
                "            Designed to Evolve\r\n" +
                "============================================\r\n\r\n" +
                "  No active receipt yet.\r\n\r\n" +
                "  1. Choose food or drinks from the menu.\r\n" +
                "  2. Enter a quantity and click Add.\r\n" +
                "  3. Click Generate Receipt or Complete Order.\r\n\r\n" +
                "============================================";
        }
    }
}
