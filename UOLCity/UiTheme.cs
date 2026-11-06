using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace UOLCity
{
    /// <summary>
    /// Centralizes colors, fonts, resource path lookup, rounded shapes, minimalist vector icons,
    /// and shared WinForms control styling so all screens share a cohesive visual identity.
    /// </summary>
    public static class UiTheme
    {
        // =====================================================================
        // COLOR PALETTE (Consistent across Main Town and all 4 Location Modules)
        // =====================================================================

        public static readonly Color DarkCharcoal = Color.FromArgb(16, 22, 32);
        public static readonly Color DeepNavy = Color.FromArgb(20, 31, 49);
        public static readonly Color SurfaceNavy = Color.FromArgb(29, 43, 68);
        public static readonly Color WarmOffWhite = Color.FromArgb(244, 246, 249);
        public static readonly Color CardWhite = Color.FromArgb(255, 255, 255);
        public static readonly Color GoldenYellow = Color.FromArgb(255, 212, 71);
        public static readonly Color MutedGreen = Color.FromArgb(40, 150, 108);
        public static readonly Color CyanAccent = Color.FromArgb(56, 189, 248);

        public static readonly Color TextPrimary = Color.FromArgb(23, 33, 49);
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);
        public static readonly Color SoftBorder = Color.FromArgb(216, 224, 235);
        public static readonly Color DangerRed = Color.FromArgb(204, 59, 68);

        // Distinct accent color for each of the four locations (aligned with reference arrows)
        public static readonly Color LibraryAccent = Color.FromArgb(56, 189, 248);   // Blue arrow
        public static readonly Color BankAccent = Color.FromArgb(245, 197, 66);      // Yellow arrow
        public static readonly Color CafeAccent = Color.FromArgb(52, 188, 138);      // Muted green
        public static readonly Color BusAccent = Color.FromArgb(164, 128, 250);      // Purple arrow

        private static PrivateFontCollection? _privateFonts;
        private static FontFamily? _displayFontFamily;
        private static bool _fontLoadAttempted;

        // =====================================================================
        // RESOURCE RESOLUTION & TYPOGRAPHY
        // =====================================================================

        /// <summary>
        /// Locates a file inside the Resources folder whether launched from Visual Studio,
        /// dotnet run, or a published build output folder.
        /// </summary>
        public static string? ResolveResourcePath(string fileName)
        {
            string[] baseDirs =
            {
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory()
            };

            foreach (string startDir in baseDirs)
            {
                DirectoryInfo? current = new DirectoryInfo(startDir);
                for (int depth = 0; depth < 5 && current != null; depth++)
                {
                    string directCandidate = Path.Combine(current.FullName, "Resources", fileName);
                    if (File.Exists(directCandidate))
                    {
                        return directCandidate;
                    }

                    string nestedCandidate = Path.Combine(current.FullName, "UOLCity", "Resources", fileName);
                    if (File.Exists(nestedCandidate))
                    {
                        return nestedCandidate;
                    }

                    current = current.Parent;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the tall, bold, condensed sans-serif display font inspired by font.png.
        /// Loads bundled BebasNeue-Regular.ttf first, with clean Windows font fallbacks.
        /// </summary>
        public static Font GetDisplayFont(
            float size,
            FontStyle style = FontStyle.Regular,
            GraphicsUnit unit = GraphicsUnit.Point)
        {
            EnsureDisplayFontLoaded();

            if (_displayFontFamily != null)
            {
                FontStyle supportedStyle = _displayFontFamily.IsStyleAvailable(style)
                    ? style
                    : FontStyle.Regular;
                return new Font(_displayFontFamily, Math.Max(6f, size), supportedStyle, unit);
            }

            string[] fallbacks =
            {
                "Bahnschrift Condensed",
                "Bahnschrift SemiBold Condensed",
                "Impact",
                "Arial Narrow",
                "Segoe UI"
            };

            foreach (string familyName in fallbacks)
            {
                try
                {
                    using Font test = new Font(familyName, 12f, FontStyle.Bold);
                    if (string.Equals(test.Name, familyName, StringComparison.OrdinalIgnoreCase))
                    {
                        return new Font(familyName, Math.Max(6f, size), FontStyle.Bold, unit);
                    }
                }
                catch
                {
                    // Continue to next fallback font
                }
            }

            return new Font(FontFamily.GenericSansSerif, Math.Max(6f, size), FontStyle.Bold, unit);
        }

        /// <summary>
        /// Returns Segoe UI for clean secondary headings, labels, buttons, and form controls.
        /// </summary>
        public static Font GetBodyFont(
            float size,
            FontStyle style = FontStyle.Regular,
            GraphicsUnit unit = GraphicsUnit.Point)
        {
            return new Font("Segoe UI", Math.Max(6f, size), style, unit);
        }

        /// <summary>
        /// Returns a monospaced font for clean alignment in the Café receipt view.
        /// </summary>
        public static Font GetMonoFont(float size)
        {
            return new Font("Consolas", Math.Max(6f, size), FontStyle.Regular, GraphicsUnit.Point);
        }

        private static void EnsureDisplayFontLoaded()
        {
            if (_fontLoadAttempted)
            {
                return;
            }

            _fontLoadAttempted = true;

            try
            {
                string? fontPath = ResolveResourcePath("BebasNeue-Regular.ttf");
                if (!string.IsNullOrWhiteSpace(fontPath) && File.Exists(fontPath))
                {
                    _privateFonts = new PrivateFontCollection();
                    _privateFonts.AddFontFile(fontPath);
                    if (_privateFonts.Families.Length > 0)
                    {
                        _displayFontFamily = _privateFonts.Families[0];
                    }
                }
            }
            catch
            {
                _displayFontFamily = null;
            }
        }

        public static Color GetLocationAccent(LocationType type)
        {
            return type switch
            {
                LocationType.Library => LibraryAccent,
                LocationType.Bank => BankAccent,
                LocationType.Cafe => CafeAccent,
                LocationType.TransportHub => BusAccent,
                _ => GoldenYellow
            };
        }

        // =====================================================================
        // CUSTOM GDI+ ROUNDED SHAPES & MINIMALIST VECTOR ICONS
        // =====================================================================

        /// <summary>
        /// Builds a smooth rounded rectangle GraphicsPath safely at any size.
        /// </summary>
        public static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float cornerRadius)
        {
            GraphicsPath path = new GraphicsPath();

            if (bounds.Width <= 1f || bounds.Height <= 1f)
            {
                path.AddRectangle(new RectangleF(bounds.X, bounds.Y, 1f, 1f));
                return path;
            }

            float diameter = Math.Max(0f, cornerRadius * 2f);
            diameter = Math.Min(diameter, Math.Min(bounds.Width, bounds.Height));

            if (diameter < 2f)
            {
                path.AddRectangle(bounds);
                return path;
            }

            RectangleF arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);

            // Top-left corner
            path.AddArc(arc, 180f, 90f);

            // Top-right corner
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270f, 90f);

            // Bottom-right corner
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0f, 90f);

            // Bottom-left corner
            arc.X = bounds.Left;
            path.AddArc(arc, 90f, 90f);

            path.CloseFigure();
            return path;
        }

        /// <summary>
        /// Draws the minimalist vector icon corresponding to one of the four locations.
        /// </summary>
        public static void DrawLocationIcon(Graphics g, LocationType type, RectangleF bounds, Color color)
        {
            switch (type)
            {
                case LocationType.Library:
                    DrawLibraryIcon(g, bounds, color);
                    break;
                case LocationType.Bank:
                    DrawBankIcon(g, bounds, color);
                    break;
                case LocationType.Cafe:
                    DrawCafeIcon(g, bounds, color);
                    break;
                case LocationType.TransportHub:
                    DrawBusIcon(g, bounds, color);
                    break;
            }
        }

        /// <summary>
        /// Minimalist open-book vector icon for the Library.
        /// </summary>
        public static void DrawLibraryIcon(Graphics g, RectangleF r, Color color)
        {
            if (r.Width < 8f || r.Height < 8f) return;

            float stroke = Math.Max(1.6f, r.Width * 0.09f);
            using Pen pen = new Pen(color, stroke)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            float left = r.X + r.Width * 0.12f;
            float right = r.Right - r.Width * 0.12f;
            float midX = r.X + r.Width * 0.50f;
            float top = r.Y + r.Height * 0.20f;
            float bottom = r.Bottom - r.Height * 0.18f;

            // Left page
            PointF[] leftPage =
            {
                new PointF(midX, top + r.Height * 0.06f),
                new PointF(left, top),
                new PointF(left, bottom - r.Height * 0.05f),
                new PointF(midX, bottom)
            };
            g.DrawPolygon(pen, leftPage);

            // Right page
            PointF[] rightPage =
            {
                new PointF(midX, top + r.Height * 0.06f),
                new PointF(right, top),
                new PointF(right, bottom - r.Height * 0.05f),
                new PointF(midX, bottom)
            };
            g.DrawPolygon(pen, rightPage);

            // Spine line
            g.DrawLine(pen, midX, top + r.Height * 0.06f, midX, bottom);
        }

        /// <summary>
        /// Minimalist classical columns and roof pediment icon for the Bank.
        /// </summary>
        public static void DrawBankIcon(Graphics g, RectangleF r, Color color)
        {
            if (r.Width < 8f || r.Height < 8f) return;

            float stroke = Math.Max(1.6f, r.Width * 0.09f);
            using Pen pen = new Pen(color, stroke)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            float left = r.X + r.Width * 0.14f;
            float right = r.Right - r.Width * 0.14f;
            float midX = r.X + r.Width * 0.50f;
            float peakY = r.Y + r.Height * 0.15f;
            float eaveY = r.Y + r.Height * 0.38f;
            float baseTopY = r.Bottom - r.Height * 0.24f;
            float baseBottomY = r.Bottom - r.Height * 0.14f;

            // Triangular pediment roof
            PointF[] roof =
            {
                new PointF(left, eaveY),
                new PointF(midX, peakY),
                new PointF(right, eaveY)
            };
            g.DrawPolygon(pen, roof);

            // Three vertical columns
            float col1 = r.X + r.Width * 0.26f;
            float col2 = midX;
            float col3 = r.Right - r.Width * 0.26f;

            g.DrawLine(pen, col1, eaveY + 1f, col1, baseTopY);
            g.DrawLine(pen, col2, eaveY + 1f, col2, baseTopY);
            g.DrawLine(pen, col3, eaveY + 1f, col3, baseTopY);

            // Foundation steps
            g.DrawLine(pen, left, baseTopY, right, baseTopY);
            g.DrawLine(pen, left - r.Width * 0.03f, baseBottomY, right + r.Width * 0.03f, baseBottomY);
        }

        /// <summary>
        /// Minimalist steaming coffee cup icon for the Café.
        /// </summary>
        public static void DrawCafeIcon(Graphics g, RectangleF r, Color color)
        {
            if (r.Width < 8f || r.Height < 8f) return;

            float stroke = Math.Max(1.6f, r.Width * 0.09f);
            using Pen pen = new Pen(color, stroke)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            float cupLeft = r.X + r.Width * 0.18f;
            float cupWidth = r.Width * 0.50f;
            float cupTop = r.Y + r.Height * 0.36f;
            float cupHeight = r.Height * 0.38f;

            // Cup body
            using (GraphicsPath cupPath = CreateRoundedRectanglePath(
                new RectangleF(cupLeft, cupTop, cupWidth, cupHeight), r.Width * 0.08f))
            {
                g.DrawPath(pen, cupPath);
            }

            // Cup handle arc
            RectangleF handleRect = new RectangleF(
                cupLeft + cupWidth - r.Width * 0.06f,
                cupTop + cupHeight * 0.15f,
                r.Width * 0.20f,
                cupHeight * 0.62f);
            g.DrawArc(pen, handleRect, -80f, 160f);

            // Saucer line
            g.DrawLine(
                pen,
                r.X + r.Width * 0.13f,
                r.Bottom - r.Height * 0.14f,
                r.Right - r.Width * 0.18f,
                r.Bottom - r.Height * 0.14f);

            // Steam lines
            float s1 = cupLeft + cupWidth * 0.33f;
            float s2 = cupLeft + cupWidth * 0.67f;
            g.DrawLine(pen, s1, r.Y + r.Height * 0.16f, s1, r.Y + r.Height * 0.27f);
            g.DrawLine(pen, s2, r.Y + r.Height * 0.14f, s2, r.Y + r.Height * 0.27f);
        }

        /// <summary>
        /// Minimalist front-view shuttle bus icon for the Bus / Transport Hub.
        /// </summary>
        public static void DrawBusIcon(Graphics g, RectangleF r, Color color)
        {
            if (r.Width < 8f || r.Height < 8f) return;

            float stroke = Math.Max(1.6f, r.Width * 0.09f);
            using Pen pen = new Pen(color, stroke)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            using SolidBrush brush = new SolidBrush(color);

            RectangleF body = new RectangleF(
                r.X + r.Width * 0.18f,
                r.Y + r.Height * 0.15f,
                r.Width * 0.64f,
                r.Height * 0.60f);

            using (GraphicsPath bodyPath = CreateRoundedRectanglePath(body, r.Width * 0.10f))
            {
                g.DrawPath(pen, bodyPath);
            }

            // Windshield divider lines
            float winTop = body.Y + body.Height * 0.24f;
            float winBottom = body.Y + body.Height * 0.62f;
            g.DrawLine(pen, body.Left, winTop, body.Right, winTop);
            g.DrawLine(pen, body.Left, winBottom, body.Right, winBottom);

            // Headlights
            float dotSize = Math.Max(2.5f, r.Width * 0.09f);
            float lightY = body.Bottom - body.Height * 0.23f;
            g.FillEllipse(brush, body.Left + body.Width * 0.16f, lightY, dotSize, dotSize);
            g.FillEllipse(brush, body.Right - body.Width * 0.16f - dotSize, lightY, dotSize, dotSize);

            // Wheels
            float wheelTop = body.Bottom;
            float wheelBottom = r.Bottom - r.Height * 0.11f;
            g.DrawLine(pen, body.Left + body.Width * 0.20f, wheelTop, body.Left + body.Width * 0.20f, wheelBottom);
            g.DrawLine(pen, body.Right - body.Width * 0.20f, wheelTop, body.Right - body.Width * 0.20f, wheelBottom);
        }

        /// <summary>
        /// Minimalist location-pin icon used on the Main Dashboard's "Locations" card.
        /// </summary>
        public static void DrawMapPinIcon(Graphics g, RectangleF r, Color color)
        {
            if (r.Width < 8f || r.Height < 8f) return;

            float stroke = Math.Max(1.6f, r.Width * 0.09f);
            using Pen pen = new Pen(color, stroke)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };

            float cx = r.X + r.Width * 0.50f;
            float rad = r.Width * 0.26f;
            float cy = r.Y + r.Height * 0.40f;

            g.DrawArc(pen, cx - rad, cy - rad, rad * 2f, rad * 2f, 150f, 240f);
            g.DrawLine(pen, cx - rad * 0.86f, cy + rad * 0.50f, cx, r.Bottom - r.Height * 0.14f);
            g.DrawLine(pen, cx + rad * 0.86f, cy + rad * 0.50f, cx, r.Bottom - r.Height * 0.14f);

            float inner = rad * 0.38f;
            g.DrawEllipse(pen, cx - inner, cy - inner, inner * 2f, inner * 2f);
        }

        // =====================================================================
        // SHARED WINFORMS MODULE HEADER, BUTTONS & DATAGRIDVIEW STYLING
        // =====================================================================

        /// <summary>
        /// Creates the top navigation banner shared by Library, Bank, Café, and Transport Hub.
        /// Includes a "Back to Town" button so users can return without closing the app.
        /// </summary>
        public static Panel CreateModuleHeader(
            string moduleTitle,
            string subtitle,
            LocationType locationType,
            EventHandler onBackToTownClick)
        {
            Color accent = GetLocationAccent(locationType);

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                MinimumSize = new Size(0, 78),
                BackColor = DeepNavy,
                Padding = new Padding(20, 12, 20, 12)
            };

            TableLayoutPanel barLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            barLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            barLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            barLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // Custom-painted vector icon badge on the left
            Panel iconBox = new Panel
            {
                Size = new Size(48, 48),
                Margin = new Padding(0, 2, 14, 2),
                Anchor = AnchorStyles.Left,
                BackColor = Color.Transparent
            };
            EnableDoubleBuffering(iconBox);
            iconBox.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                RectangleF rect = new RectangleF(1f, 1f, iconBox.Width - 3f, iconBox.Height - 3f);
                using GraphicsPath path = CreateRoundedRectanglePath(rect, 12f);
                using SolidBrush fill = new SolidBrush(Color.FromArgb(40, accent));
                using Pen border = new Pen(Color.FromArgb(180, accent), 1.5f);
                e.Graphics.FillPath(fill, path);
                e.Graphics.DrawPath(border, path);

                RectangleF iconRect = new RectangleF(9f, 9f, iconBox.Width - 18f, iconBox.Height - 18f);
                DrawLocationIcon(e.Graphics, locationType, iconRect, accent);
            };

            TableLayoutPanel textStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Anchor = AnchorStyles.Left,
                BackColor = Color.Transparent
            };
            textStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            Label lblTitle = new Label
            {
                Text = moduleTitle,
                Font = GetDisplayFont(22f, FontStyle.Regular),
                ForeColor = GoldenYellow,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 1)
            };

            Label lblSubtitle = new Label
            {
                Text = subtitle,
                Font = GetBodyFont(9.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(196, 208, 226),
                AutoSize = true,
                Margin = new Padding(1, 0, 0, 0)
            };

            textStack.Controls.Add(lblTitle, 0, 0);
            textStack.Controls.Add(lblSubtitle, 0, 1);

            Button btnBack = new Button
            {
                Text = "←  Back to Town",
                AutoSize = true,
                MinimumSize = new Size(156, 40),
                Padding = new Padding(12, 4, 12, 4),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(12, 4, 0, 4)
            };
            StyleActionButton(btnBack, SurfaceNavy, Color.White, Color.FromArgb(95, GoldenYellow));
            btnBack.Click += onBackToTownClick;

            barLayout.Controls.Add(iconBox, 0, 0);
            barLayout.Controls.Add(textStack, 1, 0);
            barLayout.Controls.Add(btnBack, 2, 0);

            // Thin bottom accent line
            header.Paint += (_, e) =>
            {
                using Pen bottomLine = new Pen(Color.FromArgb(120, accent), 2f);
                e.Graphics.DrawLine(bottomLine, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            header.Controls.Add(barLayout);

            return header;
        }

        /// <summary>
        /// Styles a WinForms Button with flat modern edges, hand cursor, and subtle hover states.
        /// </summary>
        public static void StyleActionButton(
            Button button,
            Color baseColor,
            Color textColor,
            Color? borderColor = null)
        {
            Color hoverColor = ShiftBrightness(baseColor, 18);
            Color activeColor = ShiftBrightness(baseColor, -14);

            button.FlatStyle = FlatStyle.Flat;
            button.BackColor = baseColor;
            button.ForeColor = textColor;
            button.Font = GetBodyFont(9.5f, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.FlatAppearance.BorderSize = borderColor.HasValue ? 1 : 0;
            if (borderColor.HasValue)
            {
                button.FlatAppearance.BorderColor = borderColor.Value;
            }
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = activeColor;
        }

        /// <summary>
        /// Styles a DataGridView for clean, readable presentation without visual clutter.
        /// </summary>
        public static void StyleDataGridView(DataGridView grid)
        {
            EnableDoubleBuffering(grid);

            grid.BackgroundColor = CardWhite;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = Color.FromArgb(232, 237, 244);

            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersHeight = 40;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = DeepNavy,
                ForeColor = Color.White,
                SelectionBackColor = DeepNavy,
                SelectionForeColor = Color.White,
                Font = GetBodyFont(9.5f, FontStyle.Bold),
                Padding = new Padding(8, 0, 8, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = CardWhite,
                ForeColor = TextPrimary,
                SelectionBackColor = Color.FromArgb(224, 240, 255),
                SelectionForeColor = DeepNavy,
                Font = GetBodyFont(9.5f, FontStyle.Regular),
                Padding = new Padding(8, 0, 8, 0)
            };

            grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 250, 253),
                ForeColor = TextPrimary,
                SelectionBackColor = Color.FromArgb(224, 240, 255),
                SelectionForeColor = DeepNavy
            };

            grid.RowTemplate.Height = 36;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.MultiSelect = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        /// <summary>
        /// Creates a clean card container Panel with subtle rounded border painting.
        /// </summary>
        public static Panel CreateCardPanel(Padding padding)
        {
            Panel card = new Panel
            {
                BackColor = CardWhite,
                Padding = padding,
                Margin = new Padding(8)
            };
            EnableDoubleBuffering(card);
            card.Paint += (_, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle rect = new Rectangle(0, 0, card.ClientSize.Width - 1, card.ClientSize.Height - 1);
                using Pen pen = new Pen(SoftBorder, 1f);
                e.Graphics.DrawRectangle(pen, rect);
            };
            card.Resize += (_, _) => card.Invalidate();
            return card;
        }

        /// <summary>
        /// Updates a status feedback banner label with friendly success or error styling.
        /// </summary>
        public static void ShowStatusBanner(Label banner, string message, bool isSuccess)
        {
            banner.Text = message;
            banner.Visible = !string.IsNullOrWhiteSpace(message);

            if (isSuccess)
            {
                banner.BackColor = Color.FromArgb(228, 248, 238);
                banner.ForeColor = Color.FromArgb(21, 112, 74);
            }
            else
            {
                banner.BackColor = Color.FromArgb(254, 235, 237);
                banner.ForeColor = Color.FromArgb(168, 38, 46);
            }
        }

        /// <summary>
        /// Enables protected DoubleBuffered property on Panels and DataGridViews to prevent flicker.
        /// </summary>
        public static void EnableDoubleBuffering(Control control)
        {
            PropertyInfo? prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
            prop?.SetValue(control, true, null);
        }

        private static Color ShiftBrightness(Color color, int delta)
        {
            int r = Math.Clamp(color.R + delta, 0, 255);
            int g = Math.Clamp(color.G + delta, 0, 255);
            int b = Math.Clamp(color.B + delta, 0, 255);
            return Color.FromArgb(color.A, r, g, b);
        }
    }
}
