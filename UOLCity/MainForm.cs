using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace UOLCity
{
    /// <summary>
    /// Main Town Screen for UOL CITY — Designed to Evolve.
    /// Renders the real aerial campus photograph at 75% opacity while preserving proportions,
    /// positions the four interactive location controls according to the annotated reference image,
    /// displays the top-right UOL CITY branding, and updates the lower-left live statistics dashboard.
    /// </summary>
    public sealed class MainForm : Form
    {
        private const float AerialOpacity = 0.75f;

        private readonly TownDataStore _dataStore;
        private readonly List<LocationHotspot> _locations;
        private Bitmap? _aerialBitmap;
        private string? _missingImageError;

        // Live dashboard statistics (calculated from real saved JSON data, never hardcoded)
        private int _totalLocations;
        private int _libraryBookCount;
        private decimal _walletBalance;
        private int _busRouteCount;

        public MainForm()
        {
            _dataStore = new TownDataStore();
            _locations = CreateReferenceLocations();

            InitializeWindow();
            LoadAerialBackground();
            RefreshDashboardStatistics();
        }

        private void InitializeWindow()
        {
            Text = "UOL CITY — Designed to Evolve";

            // 1360 x 749 matches the exact 1600 x 881 aspect ratio of aerial.jpeg
            ClientSize = new Size(1360, 749);
            MinimumSize = new Size(980, 580);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiTheme.DarkCharcoal;
            Font = UiTheme.GetBodyFont(9.5f);
            AutoScaleMode = AutoScaleMode.Dpi;

            // Smooth, flicker-free custom GDI+ rendering on resize and hover
            DoubleBuffered = true;
            ResizeRedraw = true;
            KeyPreview = true;
        }

        /// <summary>
        /// Creates the exact four town locations mapped to the arrow positions in "aerial - Copy.jpeg":
        /// - Yellow Arrow -> Bank (upper-left building)
        /// - Blue Arrow   -> Library (lower-left academic building)
        /// - Purple Arrow -> Bus / Transport Hub (central parked buses courtyard)
        /// - Black Arrow  -> Café (lower-right garden courtyard & pavilion)
        /// </summary>
        private static List<LocationHotspot> CreateReferenceLocations()
        {
            return new List<LocationHotspot>
            {
                new LocationHotspot(
                    LocationType.Library,
                    "Library",
                    "Book Collection",
                    normalizedX: 0.192f,
                    normalizedY: 0.618f),

                new LocationHotspot(
                    LocationType.Bank,
                    "Bank",
                    "Personal Wallet",
                    normalizedX: 0.165f,
                    normalizedY: 0.342f),

                new LocationHotspot(
                    LocationType.TransportHub,
                    "Transport Hub",
                    "Bus Schedules",
                    normalizedX: 0.432f,
                    normalizedY: 0.452f),

                new LocationHotspot(
                    LocationType.Cafe,
                    "Café",
                    "Food & Drinks",
                    normalizedX: 0.775f,
                    normalizedY: 0.668f)
            };
        }

        /// <summary>
        /// Loads Resources/aerial.jpeg into an in-memory Bitmap without locking the file on disk.
        /// </summary>
        private void LoadAerialBackground()
        {
            try
            {
                string? imagePath = UiTheme.ResolveResourcePath("aerial.jpeg");
                if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                {
                    _missingImageError = "Resources/aerial.jpeg could not be found.";
                    return;
                }

                using FileStream fs = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using Image loaded = Image.FromStream(fs);
                _aerialBitmap = new Bitmap(loaded);
            }
            catch (Exception ex)
            {
                _missingImageError = $"Unable to load aerial.jpeg: {ex.Message}";
            }
        }

        /// <summary>
        /// Loads saved JSON files and recalculates all four dashboard statistics dynamically.
        /// </summary>
        public void RefreshDashboardStatistics()
        {
            List<BookItem> books = _dataStore.LoadBooks();
            List<BankTransaction> transactions = _dataStore.LoadTransactions();
            List<BusSchedule> busSchedules = _dataStore.LoadBusSchedules();

            _totalLocations = _dataStore.TotalLocations;
            _libraryBookCount = books.Count;
            _walletBalance = _dataStore.CalculateWalletBalance(transactions);
            _busRouteCount = busSchedules.Count;

            Invalidate();
        }

        // =====================================================================
        // PROPORTIONAL VIEWPORT & CUSTOM PAINTING
        // =====================================================================

        /// <summary>
        /// Computes an aspect-ratio-preserving viewport rectangle so aerial.jpeg is never stretched.
        /// </summary>
        private RectangleF GetAerialViewport()
        {
            float clientW = Math.Max(1f, ClientSize.Width);
            float clientH = Math.Max(1f, ClientSize.Height);

            float sourceW = _aerialBitmap != null ? _aerialBitmap.Width : 1600f;
            float sourceH = _aerialBitmap != null ? _aerialBitmap.Height : 881f;

            float scale = Math.Min(clientW / sourceW, clientH / sourceH);
            float drawW = sourceW * scale;
            float drawH = sourceH * scale;
            float offsetX = (clientW - drawW) * 0.5f;
            float offsetY = (clientH - drawH) * 0.5f;

            return new RectangleF(offsetX, offsetY, drawW, drawH);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Fill base canvas with dark charcoal-navy so 75% aerial opacity blends cleanly
            g.Clear(UiTheme.DarkCharcoal);

            RectangleF viewport = GetAerialViewport();

            DrawAerialPhotograph(g, viewport);
            DrawTopRightBranding(g, viewport);
            UpdateAndDrawLocations(g, viewport);
            DrawBottomLeftDashboard(g, viewport);
        }

        /// <summary>
        /// Renders aerial.jpeg at approximately 75% opacity using a ColorMatrix alpha blend.
        /// </summary>
        private void DrawAerialPhotograph(Graphics g, RectangleF viewport)
        {
            if (_aerialBitmap == null)
            {
                using Font errFont = UiTheme.GetBodyFont(12f, FontStyle.Bold);
                using SolidBrush brush = new SolidBrush(Color.White);
                g.DrawString(
                    _missingImageError ?? "Aerial image unavailable.",
                    errFont,
                    brush,
                    new PointF(32f, 32f));
                return;
            }

            ColorMatrix matrix = new ColorMatrix
            {
                Matrix33 = AerialOpacity // 75% opacity
            };

            using ImageAttributes attributes = new ImageAttributes();
            attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            Rectangle destRect = Rectangle.Round(viewport);
            g.DrawImage(
                _aerialBitmap,
                destRect,
                0,
                0,
                _aerialBitmap.Width,
                _aerialBitmap.Height,
                GraphicsUnit.Pixel,
                attributes);
        }

        /// <summary>
        /// Renders "UOL CITY" and "Designed to Evolve" in the top-right orchard region
        /// highlighted by the red border in "aerial - Copy.jpeg" (nx = 0.48..0.86, ny = 0.00..0.22).
        /// </summary>
        private static void DrawTopRightBranding(Graphics g, RectangleF viewport)
        {
            // Center inside the top-right red border orchard zone
            float regionLeft = viewport.X + viewport.Width * 0.50f;
            float regionWidth = viewport.Width * 0.35f;
            float regionTop = viewport.Y + viewport.Height * 0.022f;
            float regionHeight = viewport.Height * 0.178f;

            RectangleF bannerBackdrop = new RectangleF(regionLeft, regionTop, regionWidth, regionHeight);

            // Subtle semi-transparent dark backdrop so golden/white typography stays crisp
            using (GraphicsPath backdropPath = UiTheme.CreateRoundedRectanglePath(bannerBackdrop, 14f))
            using (SolidBrush backdropBrush = new SolidBrush(Color.FromArgb(118, 12, 18, 28)))
            using (Pen backdropBorder = new Pen(Color.FromArgb(90, 255, 212, 71), 1.2f))
            {
                g.FillPath(backdropBrush, backdropPath);
                g.DrawPath(backdropBorder, backdropPath);
            }

            float titlePx = Math.Clamp(viewport.Height * 0.095f, 38f, 84f);
            float taglinePx = Math.Clamp(viewport.Height * 0.024f, 12f, 21f);

            using Font titleFont = UiTheme.GetDisplayFont(titlePx, FontStyle.Bold, GraphicsUnit.Pixel);
            using Font taglineFont = UiTheme.GetBodyFont(taglinePx, FontStyle.Bold, GraphicsUnit.Pixel);

            const string titleText = "UOL CITY";
            const string taglineText = "Designed to Evolve";

            SizeF titleSize = g.MeasureString(titleText, titleFont);
            SizeF taglineSize = g.MeasureString(taglineText, taglineFont);

            float totalTextHeight = titleSize.Height * 0.86f + taglineSize.Height + 4f;
            float startY = bannerBackdrop.Y + (bannerBackdrop.Height - totalTextHeight) * 0.5f;

            float titleX = bannerBackdrop.X + (bannerBackdrop.Width - titleSize.Width) * 0.5f;
            float taglineX = bannerBackdrop.X + (bannerBackdrop.Width - taglineSize.Width) * 0.5f;
            float taglineY = startY + titleSize.Height * 0.86f + 2f;

            // Subtle dark outline / drop-shadow behind "UOL CITY"
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(225, 8, 12, 20)))
            {
                g.DrawString(titleText, titleFont, shadowBrush, titleX + 2.5f, startY + 2.5f);
                g.DrawString(titleText, titleFont, shadowBrush, titleX - 1f, startY + 1f);
                g.DrawString(taglineText, taglineFont, shadowBrush, taglineX + 1.5f, taglineY + 1.5f);
            }

            using (SolidBrush titleBrush = new SolidBrush(UiTheme.GoldenYellow))
            using (SolidBrush taglineBrush = new SolidBrush(Color.FromArgb(245, 248, 252)))
            {
                g.DrawString(titleText, titleFont, titleBrush, titleX, startY);
                g.DrawString(taglineText, taglineFont, taglineBrush, taglineX, taglineY);
            }
        }

        /// <summary>
        /// Positions and draws the four interactive location controls above their exact aerial buildings.
        /// </summary>
        private void UpdateAndDrawLocations(Graphics g, RectangleF viewport)
        {
            float cardW = Math.Clamp(viewport.Width * 0.132f, 148f, 198f);
            float cardH = Math.Clamp(viewport.Height * 0.074f, 48f, 62f);
            float titlePx = Math.Clamp(cardH * 0.31f, 13f, 18f);
            float subPx = Math.Clamp(cardH * 0.21f, 10f, 12.5f);

            using Font nameFont = UiTheme.GetBodyFont(titlePx, FontStyle.Bold, GraphicsUnit.Pixel);
            using Font subFont = UiTheme.GetBodyFont(subPx, FontStyle.Regular, GraphicsUnit.Pixel);

            foreach (LocationHotspot loc in _locations)
            {
                Color accent = UiTheme.GetLocationAccent(loc.Type);

                // Exact building anchor point on aerial.jpeg
                float pinX = viewport.X + viewport.Width * loc.NormalizedX;
                float pinY = viewport.Y + viewport.Height * loc.NormalizedY;

                float hoverLift = loc.IsPressed ? 0f : (loc.IsHovered ? -3f : 0f);
                float stemGap = 11f;

                RectangleF cardRect = new RectangleF(
                    pinX - cardW * 0.5f,
                    pinY - cardH - stemGap + hoverLift,
                    cardW,
                    cardH);

                // Store clickable hit region covering both the floating card and the building pin
                RectangleF pinHitBox = new RectangleF(pinX - 18f, pinY - 18f, 36f, 36f);
                loc.CardBounds = cardRect;
                loc.InteractiveBounds = RectangleF.Union(cardRect, pinHitBox);

                // 1. Subtle hover pulse ring around the building anchor pin
                if (loc.IsHovered || loc.IsPressed)
                {
                    using SolidBrush glowBrush = new SolidBrush(Color.FromArgb(75, accent));
                    g.FillEllipse(glowBrush, pinX - 14f, pinY - 14f, 28f, 28f);
                }

                // 2. Connector stem from card down to building anchor pin
                using (Pen stemPen = new Pen(Color.FromArgb(220, accent), 2f))
                {
                    g.DrawLine(stemPen, pinX, cardRect.Bottom, pinX, pinY);
                }

                // 3. Pin dot directly on the building / courtyard
                using (SolidBrush pinFill = new SolidBrush(loc.IsHovered ? UiTheme.GoldenYellow : accent))
                using (Pen pinRing = new Pen(Color.White, 2f))
                {
                    g.FillEllipse(pinFill, pinX - 6.5f, pinY - 6.5f, 13f, 13f);
                    g.DrawEllipse(pinRing, pinX - 6.5f, pinY - 6.5f, 13f, 13f);
                }

                // 4. Floating card drop shadow
                RectangleF shadowRect = new RectangleF(cardRect.X + 2f, cardRect.Y + 4f, cardRect.Width, cardRect.Height);
                using (GraphicsPath shadowPath = UiTheme.CreateRoundedRectanglePath(shadowRect, 12f))
                using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(110, 0, 0, 0)))
                {
                    g.FillPath(shadowBrush, shadowPath);
                }

                // 5. Card surface & border (with hover and active states)
                Color surfaceColor = loc.IsPressed
                    ? Color.FromArgb(242, 28, 44, 72)
                    : (loc.IsHovered ? Color.FromArgb(238, 22, 35, 58) : Color.FromArgb(216, 15, 23, 37));

                Color borderColor = loc.IsPressed
                    ? Color.White
                    : (loc.IsHovered ? UiTheme.GoldenYellow : Color.FromArgb(215, accent));

                float borderWidth = (loc.IsHovered || loc.IsPressed) ? 2.2f : 1.6f;

                using (GraphicsPath cardPath = UiTheme.CreateRoundedRectanglePath(cardRect, 12f))
                using (SolidBrush surfaceBrush = new SolidBrush(surfaceColor))
                using (Pen borderPen = new Pen(borderColor, borderWidth))
                {
                    g.FillPath(surfaceBrush, cardPath);
                    g.DrawPath(borderPen, cardPath);
                }

                // 6. Circular minimalist vector icon badge inside the card
                float badgeSize = cardRect.Height - 14f;
                RectangleF badgeRect = new RectangleF(cardRect.X + 8f, cardRect.Y + 7f, badgeSize, badgeSize);
                using (SolidBrush badgeFill = new SolidBrush(Color.FromArgb(loc.IsHovered ? 65 : 42, accent)))
                using (Pen badgeBorder = new Pen(Color.FromArgb(170, accent), 1.2f))
                {
                    g.FillEllipse(badgeFill, badgeRect);
                    g.DrawEllipse(badgeBorder, badgeRect);
                }

                RectangleF iconRect = new RectangleF(
                    badgeRect.X + badgeSize * 0.16f,
                    badgeRect.Y + badgeSize * 0.16f,
                    badgeSize * 0.68f,
                    badgeSize * 0.68f);
                UiTheme.DrawLocationIcon(g, loc.Type, iconRect, loc.IsHovered ? UiTheme.GoldenYellow : accent);

                // 7. Location Name & Subtitle
                float textX = badgeRect.Right + 8f;
                float textMaxW = cardRect.Right - textX - 6f;

                RectangleF nameLayout = new RectangleF(textX, cardRect.Y + cardRect.Height * 0.14f, textMaxW, cardRect.Height * 0.44f);
                RectangleF subLayout = new RectangleF(textX, cardRect.Y + cardRect.Height * 0.53f, textMaxW, cardRect.Height * 0.38f);

                string subtitle = loc.IsHovered ? "Click to open →" : loc.Subtitle;
                Color subColor = loc.IsHovered ? UiTheme.GoldenYellow : Color.FromArgb(190, 204, 224);

                using StringFormat singleLine = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                using SolidBrush nameBrush = new SolidBrush(Color.White);
                using SolidBrush subBrush = new SolidBrush(subColor);

                g.DrawString(loc.Name, nameFont, nameBrush, nameLayout, singleLine);
                g.DrawString(subtitle, subFont, subBrush, subLayout, singleLine);
            }
        }

        /// <summary>
        /// Draws the four compact semi-transparent dashboard cards inside the lower-left white box
        /// region of "aerial - Copy.jpeg" (nx = 0.049..0.419, ny = 0.812..0.991).
        /// Displays: 1. Locations, 2. Library Books, 3. Wallet Balance, 4. Bus Routes.
        /// </summary>
        private void DrawBottomLeftDashboard(Graphics g, RectangleF viewport)
        {
            // Keep strictly inside the lower-left reference area so aerial buildings remain clear
            float areaX = viewport.X + viewport.Width * 0.044f;
            float areaY = viewport.Y + viewport.Height * 0.822f;
            float areaW = viewport.Width * 0.415f;
            float areaH = viewport.Height * 0.144f;

            float gap = Math.Max(6f, areaW * 0.016f);
            float usableW = areaW - gap * 3f;

            // Give Wallet Balance slightly wider proportion so formatted currency fits comfortably
            float[] weights = { 0.21f, 0.23f, 0.33f, 0.23f };

            string[] labels =
            {
                "Locations",
                "Library Books",
                "Wallet Balance",
                "Bus Routes"
            };

            string[] values =
            {
                _totalLocations.ToString(),
                _libraryBookCount.ToString(),
                TownDataStore.FormatCurrency(_walletBalance),
                _busRouteCount.ToString()
            };

            Color[] accents =
            {
                UiTheme.CyanAccent,
                UiTheme.LibraryAccent,
                UiTheme.GoldenYellow,
                UiTheme.BusAccent
            };

            float labelPx = Math.Clamp(areaH * 0.14f, 10.5f, 13.5f);
            float valuePx = Math.Clamp(areaH * 0.24f, 14f, 22f);

            using Font labelFont = UiTheme.GetBodyFont(labelPx, FontStyle.Bold, GraphicsUnit.Pixel);
            using Font valueFont = UiTheme.GetBodyFont(valuePx, FontStyle.Bold, GraphicsUnit.Pixel);
            using StringFormat nowrap = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };

            float currentX = areaX;
            for (int i = 0; i < 4; i++)
            {
                float cardW = usableW * weights[i];
                RectangleF cardRect = new RectangleF(currentX, areaY, cardW, areaH);

                // Semi-transparent rounded dark-navy panel so satellite imagery subtly shows through
                using (GraphicsPath path = UiTheme.CreateRoundedRectanglePath(cardRect, 12f))
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(206, 15, 23, 38)))
                using (Pen border = new Pen(Color.FromArgb(95, accents[i]), 1.3f))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(border, path);
                }

                // Minimalist icon in top-left of stat card
                float iconSize = Math.Clamp(areaH * 0.25f, 16f, 24f);
                RectangleF iconRect = new RectangleF(cardRect.X + 10f, cardRect.Y + 10f, iconSize, iconSize);

                switch (i)
                {
                    case 0:
                        UiTheme.DrawMapPinIcon(g, iconRect, accents[i]);
                        break;
                    case 1:
                        UiTheme.DrawLibraryIcon(g, iconRect, accents[i]);
                        break;
                    case 2:
                        UiTheme.DrawBankIcon(g, iconRect, accents[i]);
                        break;
                    case 3:
                        UiTheme.DrawBusIcon(g, iconRect, accents[i]);
                        break;
                }

                // Stat label
                RectangleF labelRect = new RectangleF(
                    cardRect.X + 10f,
                    iconRect.Bottom + 5f,
                    cardRect.Width - 16f,
                    labelPx + 6f);
                using (SolidBrush lblBrush = new SolidBrush(Color.FromArgb(196, 210, 228)))
                {
                    g.DrawString(labels[i], labelFont, lblBrush, labelRect, nowrap);
                }

                // Stat calculated value
                // Slightly scale font if Wallet Balance text is long on narrow windows
                Font activeValueFont = valueFont;
                Font? scaledFont = null;
                SizeF measured = g.MeasureString(values[i], valueFont);
                if (measured.Width > cardRect.Width - 14f)
                {
                    float ratio = (cardRect.Width - 14f) / Math.Max(1f, measured.Width);
                    float adjustedPx = Math.Max(11f, valuePx * ratio);
                    scaledFont = UiTheme.GetBodyFont(adjustedPx, FontStyle.Bold, GraphicsUnit.Pixel);
                    activeValueFont = scaledFont;
                }

                RectangleF valueRect = new RectangleF(
                    cardRect.X + 10f,
                    cardRect.Bottom - activeValueFont.Height - 9f,
                    cardRect.Width - 14f,
                    activeValueFont.Height + 4f);

                Color valColor = (i == 2 && _walletBalance < 0m)
                    ? Color.FromArgb(255, 130, 130)
                    : (i == 2 ? UiTheme.GoldenYellow : Color.White);

                using (SolidBrush valBrush = new SolidBrush(valColor))
                {
                    g.DrawString(values[i], activeValueFont, valBrush, valueRect, nowrap);
                }

                scaledFont?.Dispose();
                currentX += cardW + gap;
            }
        }

        // =====================================================================
        // INTERACTIVE MOUSE & KEYBOARD NAVIGATION EVENTS
        // =====================================================================

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            bool needsRepaint = false;
            bool anyHovered = false;

            foreach (LocationHotspot loc in _locations)
            {
                bool hit = loc.InteractiveBounds.Contains(e.Location);
                if (loc.IsHovered != hit)
                {
                    loc.IsHovered = hit;
                    needsRepaint = true;
                }

                if (hit)
                {
                    anyHovered = true;
                }
            }

            Cursor = anyHovered ? Cursors.Hand : Cursors.Default;

            if (needsRepaint)
            {
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            bool changed = false;
            foreach (LocationHotspot loc in _locations)
            {
                if (loc.IsHovered || loc.IsPressed)
                {
                    loc.IsHovered = false;
                    loc.IsPressed = false;
                    changed = true;
                }
            }

            Cursor = Cursors.Default;
            if (changed)
            {
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            foreach (LocationHotspot loc in _locations)
            {
                if (loc.InteractiveBounds.Contains(e.Location))
                {
                    loc.IsPressed = true;
                    Invalidate();
                    break;
                }
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            LocationHotspot? activated = null;

            foreach (LocationHotspot loc in _locations)
            {
                bool wasPressed = loc.IsPressed;
                loc.IsPressed = false;

                if (wasPressed && loc.InteractiveBounds.Contains(e.Location))
                {
                    activated = loc;
                }
            }

            Invalidate();

            if (activated != null)
            {
                OpenLocationModule(activated.Type);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Convenient number-key shortcuts (1-4) for quick demonstration
            switch (e.KeyCode)
            {
                case Keys.D1:
                case Keys.NumPad1:
                    OpenLocationModule(LocationType.Library);
                    break;
                case Keys.D2:
                case Keys.NumPad2:
                    OpenLocationModule(LocationType.Bank);
                    break;
                case Keys.D3:
                case Keys.NumPad3:
                    OpenLocationModule(LocationType.Cafe);
                    break;
                case Keys.D4:
                case Keys.NumPad4:
                    OpenLocationModule(LocationType.TransportHub);
                    break;
            }
        }

        /// <summary>
        /// Opens the selected location Form and refreshes the main town dashboard upon returning.
        /// </summary>
        private void OpenLocationModule(LocationType locationType)
        {
            // Clear hover/press visual state before opening modal child window
            foreach (LocationHotspot loc in _locations)
            {
                loc.IsHovered = false;
                loc.IsPressed = false;
            }
            Cursor = Cursors.Default;
            Invalidate();

            using Form moduleForm = locationType switch
            {
                LocationType.Library => new LibraryForm(_dataStore),
                LocationType.Bank => new BankForm(_dataStore),
                LocationType.Cafe => new CafeForm(_dataStore),
                LocationType.TransportHub => new TransportForm(_dataStore),
                _ => new LibraryForm(_dataStore)
            };

            moduleForm.ShowDialog(this);

            // Immediately recalculate dashboard counts and wallet balance from JSON data
            RefreshDashboardStatistics();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _aerialBitmap?.Dispose();
                _aerialBitmap = null;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Represents a clickable building/location marker on the aerial town canvas.
        /// </summary>
        private sealed class LocationHotspot
        {
            public LocationType Type { get; }
            public string Name { get; }
            public string Subtitle { get; }
            public float NormalizedX { get; }
            public float NormalizedY { get; }

            public RectangleF CardBounds { get; set; }
            public RectangleF InteractiveBounds { get; set; }
            public bool IsHovered { get; set; }
            public bool IsPressed { get; set; }

            public LocationHotspot(
                LocationType type,
                string name,
                string subtitle,
                float normalizedX,
                float normalizedY)
            {
                Type = type;
                Name = name;
                Subtitle = subtitle;
                NormalizedX = normalizedX;
                NormalizedY = normalizedY;
            }
        }
    }
}
