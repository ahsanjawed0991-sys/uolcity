using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace UOLCity
{
    /// <summary>
    /// Library Module: Personal book collection management interface.
    /// Supports adding books, searching by title or author, removing books,
    /// displaying empty states, and saving records locally to JSON.
    /// </summary>
    public sealed class LibraryForm : Form
    {
        private readonly TownDataStore _dataStore;
        private readonly List<BookItem> _books;

        // Input controls
        private TextBox _txtTitle = null!;
        private TextBox _txtAuthor = null!;
        private TextBox _txtSearch = null!;
        private Label _lblStatusBanner = null!;
        private Label _lblCountBadge = null!;

        // Collection display & empty state controls
        private DataGridView _gridBooks = null!;
        private Panel _emptyStatePanel = null!;
        private Label _lblEmptyTitle = null!;
        private Label _lblEmptyHint = null!;

        public LibraryForm(TownDataStore dataStore)
        {
            _dataStore = dataStore;
            _books = _dataStore.LoadBooks();

            InitializeWindow();
            BuildUserInterface();
            RefreshBookGrid();
        }

        private void InitializeWindow()
        {
            Text = "UOL CITY — Library (Book Collection)";
            ClientSize = new Size(1060, 640);
            MinimumSize = new Size(920, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UiTheme.WarmOffWhite;
            Font = UiTheme.GetBodyFont(9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;
            KeyPreview = true;

            // Allow pressing Escape to return quickly to the town map
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
            // Top navigation header with "Back to Town" button
            Panel header = UiTheme.CreateModuleHeader(
                "UOL CITY LIBRARY",
                "Personal Book Collection — Add, search, and manage saved books",
                LocationType.Library,
                (_, _) => Close());

            // Two-column responsive layout: Left (Add Book form) | Right (Collection & Search)
            TableLayoutPanel mainSplit = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(16),
                BackColor = UiTheme.WarmOffWhite
            };
            mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35f));
            mainSplit.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65f));
            mainSplit.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            Panel leftCard = BuildAddBookCard();
            Panel rightCard = BuildCollectionCard();

            mainSplit.Controls.Add(leftCard, 0, 0);
            mainSplit.Controls.Add(rightCard, 1, 0);

            Controls.Add(mainSplit);
            Controls.Add(header);
        }

        private Panel BuildAddBookCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(20));
            card.Dock = DockStyle.Fill;

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label lblSection = new Label
            {
                Text = "Add a New Book",
                Font = UiTheme.GetBodyFont(13f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            Label lblDesc = new Label
            {
                Text = "Enter a book title and author name to save it to your local library JSON file.",
                Font = UiTheme.GetBodyFont(9f),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                MaximumSize = new Size(300, 0),
                Margin = new Padding(0, 0, 0, 18)
            };

            Label lblTitle = new Label
            {
                Text = "Book Title *",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            _txtTitle = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10.5f),
                PlaceholderText = "e.g., Introduction to Algorithms",
                Margin = new Padding(0, 0, 0, 16)
            };

            Label lblAuthor = new Label
            {
                Text = "Author Name *",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.TextPrimary,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            _txtAuthor = new TextBox
            {
                Dock = DockStyle.Top,
                Font = UiTheme.GetBodyFont(10.5f),
                PlaceholderText = "e.g., Thomas H. Cormen",
                Margin = new Padding(0, 0, 0, 20)
            };

            // Allow pressing Enter in the Author box to submit the form
            _txtAuthor.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    HandleAddBook();
                }
            };

            Button btnAddBook = new Button
            {
                Text = "+  Add Book to Library",
                Dock = DockStyle.Top,
                Height = 42,
                Margin = new Padding(0, 0, 0, 16)
            };
            UiTheme.StyleActionButton(btnAddBook, UiTheme.DeepNavy, Color.White);
            btnAddBook.Click += (_, _) => HandleAddBook();

            _lblStatusBanner = new Label
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 38),
                Padding = new Padding(10, 8, 10, 8),
                Font = UiTheme.GetBodyFont(9f, FontStyle.Bold),
                Visible = false
            };

            layout.Controls.Add(lblSection);
            layout.Controls.Add(lblDesc);
            layout.Controls.Add(lblTitle);
            layout.Controls.Add(_txtTitle);
            layout.Controls.Add(lblAuthor);
            layout.Controls.Add(_txtAuthor);
            layout.Controls.Add(btnAddBook);
            layout.Controls.Add(_lblStatusBanner);

            card.Controls.Add(layout);
            return card;
        }

        private Panel BuildCollectionCard()
        {
            Panel card = UiTheme.CreateCardPanel(new Padding(20));
            card.Dock = DockStyle.Fill;

            // Top bar: Section title + live count + Search bar
            TableLayoutPanel topBar = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 86,
                ColumnCount = 2,
                RowCount = 2
            };
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
            topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            topBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));
            topBar.RowStyles.Add(new RowStyle(SizeType.Absolute, 44f));

            Label lblHeading = new Label
            {
                Text = "Saved Book Collection",
                Font = UiTheme.GetBodyFont(13f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.Left
            };

            _lblCountBadge = new Label
            {
                Text = "0 Books",
                Font = UiTheme.GetBodyFont(9.5f, FontStyle.Bold),
                ForeColor = UiTheme.MutedGreen,
                AutoSize = true,
                Anchor = AnchorStyles.Right
            };

            // Search box + Clear button row
            Panel searchRow = new Panel { Dock = DockStyle.Fill };
            _txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = UiTheme.GetBodyFont(10f),
                PlaceholderText = "Search books by title or author..."
            };
            _txtSearch.TextChanged += (_, _) => RefreshBookGrid();

            Button btnClearSearch = new Button
            {
                Text = "Clear",
                Dock = DockStyle.Right,
                Width = 84
            };
            UiTheme.StyleActionButton(btnClearSearch, Color.FromArgb(230, 236, 245), UiTheme.DeepNavy);
            btnClearSearch.Click += (_, _) =>
            {
                _txtSearch.Clear();
                _txtSearch.Focus();
            };

            Panel spacer = new Panel { Dock = DockStyle.Right, Width = 8 };
            searchRow.Controls.Add(_txtSearch);
            searchRow.Controls.Add(spacer);
            searchRow.Controls.Add(btnClearSearch);

            topBar.Controls.Add(lblHeading, 0, 0);
            topBar.Controls.Add(_lblCountBadge, 1, 0);
            topBar.Controls.Add(searchRow, 0, 1);
            topBar.SetColumnSpan(searchRow, 2);

            // Bottom bar: Remove Selected Book action
            Panel bottomBar = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                Padding = new Padding(0, 10, 0, 0)
            };

            Button btnRemove = new Button
            {
                Text = "Remove Selected Book",
                Dock = DockStyle.Right,
                Width = 195
            };
            UiTheme.StyleActionButton(btnRemove, UiTheme.DangerRed, Color.White);
            btnRemove.Click += (_, _) => HandleRemoveSelectedBook();

            bottomBar.Controls.Add(btnRemove);

            // Middle container holding DataGridView + Empty State overlay
            Panel gridHost = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 8, 0, 4)
            };

            _gridBooks = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UiTheme.StyleDataGridView(_gridBooks);

            _gridBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Id",
                HeaderText = "ID",
                Visible = false
            });
            _gridBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Title",
                HeaderText = "Book Title",
                FillWeight = 48f
            });
            _gridBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Author",
                HeaderText = "Author",
                FillWeight = 34f
            });
            _gridBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "AddedDate",
                HeaderText = "Date Added",
                FillWeight = 18f
            });

            _emptyStatePanel = BuildEmptyStatePanel();

            gridHost.Controls.Add(_emptyStatePanel);
            gridHost.Controls.Add(_gridBooks);

            card.Controls.Add(gridHost);
            card.Controls.Add(bottomBar);
            card.Controls.Add(topBar);

            return card;
        }

        private Panel BuildEmptyStatePanel()
        {
            Panel empty = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 253),
                Visible = false
            };

            TableLayoutPanel centerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            centerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            _lblEmptyTitle = new Label
            {
                Text = "No Books in Your Library Yet",
                Font = UiTheme.GetBodyFont(12f, FontStyle.Bold),
                ForeColor = UiTheme.DeepNavy,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                Margin = new Padding(12, 0, 12, 6)
            };

            _lblEmptyHint = new Label
            {
                Text = "Add a book title and author on the left to start building your personal collection.",
                Font = UiTheme.GetBodyFont(9.5f),
                ForeColor = UiTheme.TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.None,
                TextAlign = ContentAlignment.MiddleCenter,
                MaximumSize = new Size(440, 0)
            };

            centerLayout.Controls.Add(_lblEmptyTitle, 0, 1);
            centerLayout.Controls.Add(_lblEmptyHint, 0, 2);
            empty.Controls.Add(centerLayout);

            return empty;
        }

        // =====================================================================
        // EVENT HANDLERS & UI REFRESH METHODS
        // =====================================================================

        private void HandleAddBook()
        {
            bool added = _dataStore.TryAddBook(
                _books,
                _txtTitle.Text,
                _txtAuthor.Text,
                out string feedbackMessage);

            UiTheme.ShowStatusBanner(_lblStatusBanner, feedbackMessage, isSuccess: added);

            if (!added)
            {
                if (string.IsNullOrWhiteSpace(_txtTitle.Text))
                {
                    _txtTitle.Focus();
                }
                else
                {
                    _txtAuthor.Focus();
                }
                return;
            }

            // Clear input fields after successful addition
            _txtTitle.Clear();
            _txtAuthor.Clear();
            _txtTitle.Focus();

            RefreshBookGrid();
        }

        private void HandleRemoveSelectedBook()
        {
            if (_gridBooks.SelectedRows.Count == 0)
            {
                UiTheme.ShowStatusBanner(
                    _lblStatusBanner,
                    "Please select a book row in the table first before clicking Remove.",
                    isSuccess: false);
                return;
            }

            string? selectedId = _gridBooks.SelectedRows[0].Cells["Id"].Value?.ToString();
            string selectedTitle = _gridBooks.SelectedRows[0].Cells["Title"].Value?.ToString() ?? "this book";

            DialogResult confirm = MessageBox.Show(
                this,
                $"Are you sure you want to remove \"{selectedTitle}\" from your library?",
                "Confirm Book Removal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }

            bool removed = _dataStore.RemoveBookById(_books, selectedId, out string message);
            UiTheme.ShowStatusBanner(_lblStatusBanner, message, isSuccess: removed);
            RefreshBookGrid();
        }

        private void RefreshBookGrid()
        {
            List<BookItem> visibleBooks = _dataStore.SearchBooks(_books, _txtSearch.Text);

            _gridBooks.Rows.Clear();
            foreach (BookItem book in visibleBooks)
            {
                _gridBooks.Rows.Add(book.Id, book.Title, book.Author, book.AddedDate);
            }

            if (string.IsNullOrWhiteSpace(_txtSearch.Text))
            {
                _lblCountBadge.Text = _books.Count == 1 ? "1 Saved Book" : $"{_books.Count} Saved Books";
            }
            else
            {
                _lblCountBadge.Text = $"Showing {visibleBooks.Count} of {_books.Count} Books";
            }

            // Toggle appropriate Empty State when collection or search result is empty
            if (_books.Count == 0)
            {
                _lblEmptyTitle.Text = "Your Personal Library Is Empty";
                _lblEmptyHint.Text = "Enter a book title and author name on the left to add your first book.";
                _emptyStatePanel.Visible = true;
                _emptyStatePanel.BringToFront();
                _gridBooks.Visible = false;
            }
            else if (visibleBooks.Count == 0)
            {
                _lblEmptyTitle.Text = "No Matching Books Found";
                _lblEmptyHint.Text = $"No saved books matched \"{_txtSearch.Text.Trim()}\". Try another title or author, or click Clear.";
                _emptyStatePanel.Visible = true;
                _emptyStatePanel.BringToFront();
                _gridBooks.Visible = false;
            }
            else
            {
                _emptyStatePanel.Visible = false;
                _gridBooks.Visible = true;
                _gridBooks.BringToFront();
            }
        }
    }
}
