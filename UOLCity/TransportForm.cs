using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UOLCity
{
    /// <summary>
    /// Bus / Transport Hub Module: Campus bus route and schedule management interface.
    /// Displays bus numbers, route names, destinations, and departure times,
    /// validates new schedule entries, supports removals, and saves locally to JSON.
    /// </summary>
    public sealed class TransportForm : Form
    {
        private readonly TownDataStore _dataStore;
        private readonly List<BusSchedule> _schedules;

        // Form inputs
        private TextBox _txtBusNumber = null!;
        private TextBox _txtRouteName = null!;
        private TextBox _txtDestination = null!;
        private TextBox _txtDepartureTime = null!;
        private Label _lblStatusBanner = null!;
        private Label _lblRouteCount = null!;

        // Schedule table & empty state
        private DataGridView _gridSchedules = null!;
        private Panel _emptyStatePanel = null!;

        public TransportForm(TownDataStore dataStore)
        {
            _dataStore = dataStore;
            _schedules = _dataStore.LoadBusSchedules();

            InitializeWindow();
            BuildUserInterface();
            RefreshScheduleGrid();
        }

        private void InitializeWindow()
        {
            Text = "UOL CITY — Bus & Transport Hub";
            ClientSize = new Size(1080, 650);
            MinimumSize = new Size(940, 570);
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
                "UOL CITY TRANSPORT HUB",
                "Campus Bus Routes & Departures — View, add, and manage shuttle schedules",
                LocationType.TransportHub,
                (_, _) => Close());

            TableLayoutPanel mainSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16),
                BackColor = UiTheme.WarmOffWhite
            };
            mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
            mainSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            mainSplit.Controls.Add(BuildAddScheduleCard(), 0, 0);
            mainSplit.Controls.Add(BuildScheduleTableCard(), 1, 0);

            Controls.Add(mainSplit);
            Controls.Add(header);
        }

        private Panel BuildAddScheduleCard()
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
                Text = "Add Bus Schedule",
                Font = UiTheme.GetBodyFont(13f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            Label lblDesc = new Label
            {
                Text = "Register a new bus route, destination, and departure time in the Transport Hub.",
                Font = UiTheme.GetBodyFont(9f),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                MaximumSize = new Size(310, 0),
                Margin = new Padding(0, 0, 0, 14)
            };

            Label lblBusNo = CreateFieldLabel("Bus Number / Identifier *");
            _txtBusNumber = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10f),
                PlaceholderText = "e.g., UOL-04",
                Margin = new Padding(0, 0, 0, 12)
            };

            Label lblRoute = CreateFieldLabel("Route Name *");
            _txtRouteName = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10f),
                PlaceholderText = "e.g., Route D - Campus Shuttle",
                Margin = new Padding(0, 0, 0, 12)
            };

            Label lblDestination = CreateFieldLabel("Destination *");
            _txtDestination = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10f),
                PlaceholderText = "e.g., Sargodha Railway Road",
                Margin = new Padding(0, 0, 0, 12)
            };

            Label lblTime = CreateFieldLabel("Departure Time *");
            _txtDepartureTime = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10f),
                PlaceholderText = "e.g., 02:30 PM or 14:30",
                Margin = new Padding(0, 0, 0, 18)
            };

            _txtDepartureTime.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    HandleAddSchedule();
                }
            };

            Button btnAdd = new Button
            {
                Text = "+  Add Schedule Entry",
                Dock = DockStyle.Top,
                Height = 42,
                Margin = new Padding(0, 0, 0, 14)
            };
            UiTheme.StyleActionButton(btnAdd, UiTheme.DeepNavy, Color.White);
            btnAdd.Click += (_, _) => HandleAddSchedule();

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
            formLayout.Controls.Add(lblDesc);
            formLayout.Controls.Add(lblBusNo);
            formLayout.Controls.Add(_txtBusNumber);
            formLayout.Controls.Add(lblRoute);
            formLayout.Controls.Add(_txtRouteName);
            formLayout.Controls.Add(lblDestination);
            formLayout.Controls.Add(_txtDestination);
            formLayout.Controls.Add(lblTime);
            formLayout.Controls.Add(_txtDepartureTime);
            formLayout.Controls.Add(btnAdd);
            formLayout.Controls.Add(_lblStatusBanner);

            card.Controls.Add(formLayout);
            return card;
        }

        private static Label CreateFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 5)
            };
        }

        private Panel BuildScheduleTableCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(20));
            card.Dock = DockStyle.Fill;

            TableLayoutPanel topBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                ColumnCount = 2
            };
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));

            Label lblHeading = new Label
            {
                Text = "Available Bus Routes & Departures",
                Font = UiTheme.GetBodyFont(13f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            _lblRouteCount = new Label
            {
                Text = "0 Routes",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.MutedGreen,
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            topBar.Controls.Add(lblHeading, 0, 0);
            topBar.Controls.Add(_lblRouteCount, 1, 0);

            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(0, 10, 0, 0)
            };

            Button btnRemove = new Button
            {
                Text = "Remove Selected Schedule",
                Dock = DockStyle.Right,
                Width = 210
            };
            UiTheme.StyleActionButton(btnRemove, UiTheme.DangerRed, Color.White);
            btnRemove.Click += (_, _) => HandleRemoveSchedule();
            bottomBar.Controls.Add(btnRemove);

            Panel gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 4)
            };

            _gridSchedules = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UiTheme.StyleDataGridView(_gridSchedules);

            _gridSchedules.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                Visible = false
            });
            _gridSchedules.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "BusNumber",
                HeaderText = "Bus No.",
                FillWeight = 18f,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                    ForeColor = UiTheme.DeepNavy
                }
            });
            _gridSchedules.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "RouteName",
                HeaderText = "Route",
                FillWeight = 32f
            });
            _gridSchedules.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Destination",
                HeaderText = "Destination",
                FillWeight = 30f
            });
            _gridSchedules.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "DepartureTime",
                HeaderText = "Departure",
                FillWeight = 20f,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                    ForeColor = UiTheme.MutedGreen
                }
            });

            _emptyStatePanel = BuildEmptyStatePanel();

            gridHost.Controls.Add(_emptyStatePanel);
            gridHost.Controls.Add(_gridSchedules);

            card.Controls.Add(gridHost);
            card.Controls.Add(bottomBar);
            card.Controls.Add(topBar);

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
                Text = "No Bus Schedules Registered",
                Font = UiTheme.GetBodyFont(12f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(12, 0, 12, 6)
            };

            Label hint = new Label
            {
                Text = "Add a bus number, route, destination, and departure time on the left to create a schedule.",
                Font = UiTheme.GetBodyFont(9.5f),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter,
                MaximumSize = new Size(440, 0)
            };

            layout.Controls.Add(title, 0, 1);
            layout.Controls.Add(hint, 0, 2);
            empty.Controls.Add(layout);

            return empty;
        }

        // =====================================================================
        // EVENT HANDLERS & GRID REFRESH
        // =====================================================================

        private void HandleAddSchedule()
        {
            bool added = _dataStore.TryAddBusSchedule(
                _schedules,
                _txtBusNumber.Text,
                _txtRouteName.Text,
                _txtDestination.Text,
                _txtDepartureTime.Text,
                out string message);

            UiTheme.ShowStatusBanner(_lblStatusBanner, message, isSuccess: added);

            if (!added)
            {
                if (string.IsNullOrWhiteSpace(_txtBusNumber.Text))
                {
                    _txtBusNumber.Focus();
                }
                else if (string.IsNullOrWhiteSpace(_txtRouteName.Text))
                {
                    _txtRouteName.Focus();
                }
                else if (string.IsNullOrWhiteSpace(_txtDestination.Text))
                {
                    _txtDestination.Focus();
                }
                else
                {
                    _txtDepartureTime.Focus();
                    _txtDepartureTime.SelectAll();
                }
                return;
            }

            _txtBusNumber.Clear();
            _txtRouteName.Clear();
            _txtDestination.Clear();
            _txtDepartureTime.Clear();
            _txtBusNumber.Focus();

            RefreshScheduleGrid();
        }

        private void HandleRemoveSchedule()
        {
            if (_gridSchedules.SelectedRows.Count == 0)
            {
                UiTheme.ShowStatusBanner(
                    _lblStatusBanner,
                    "Please select a schedule row in the table first before clicking Remove.",
                    isSuccess: false);
                return;
            }

            string? id = _gridSchedules.SelectedRows[0].Cells["Id"].Value?.ToString();
            string busNo = _gridSchedules.SelectedRows[0].Cells["BusNumber"].Value?.ToString() ?? "bus";
            string dest = _gridSchedules.SelectedRows[0].Cells["Destination"].Value?.ToString() ?? "route";

            DialogResult confirm = MessageBox.Show(
                this,
                $"Remove schedule entry for Bus {busNo} ({dest})?",
                "Confirm Schedule Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            bool removed = _dataStore.RemoveBusScheduleById(_schedules, id, out string message);
            UiTheme.ShowStatusBanner(_lblStatusBanner, message, isSuccess: removed);
            RefreshScheduleGrid();
        }

        private void RefreshScheduleGrid()
        {
            _gridSchedules.Rows.Clear();
            foreach (BusSchedule item in _schedules)
            {
                _gridSchedules.Rows.Add(
                    item.Id,
                    item.BusNumber,
                    item.RouteName,
                    item.Destination,
                    item.DepartureTime);
            }

            _lblRouteCount.Text = _schedules.Count == 1
                ? "1 Active Route"
                : $"{_schedules.Count} Active Routes";

            if (_schedules.Count == 0)
            {
                _emptyStatePanel.Visible = true;
                _emptyStatePanel.BringToFront();
                _gridSchedules.Visible = false;
            }
            else
            {
                _emptyStatePanel.Visible = false;
                _gridSchedules.Visible = true;
                _gridSchedules.BringToFront();
            }
        }
    }
}
