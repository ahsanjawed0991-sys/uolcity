# UOL CITY — Designed to Evolve

> **University Midterm Desktop Application Project**  
> Built with **C#**, **Windows Forms (.NET 8)**, custom **GDI+ rendering**, and **`System.Text.Json`** local persistence.

---

## 1. Project Overview

**UOL CITY — Designed to Evolve** is a clean, beginner-friendly C# Windows Forms desktop application representing a miniature interactive university town built over a real campus aerial photograph (`aerial.jpeg`).

From the main aerial town screen, users can explore **four functional campus locations**, track real-time statistics on the town dashboard, and manage personal records saved locally as readable JSON files.

---

## 2. University Midterm Objectives

This project is designed specifically to demonstrate core C# and Windows Forms object-oriented programming concepts clearly and reliably:

- **Classes & Objects**: Strongly typed models (`BookItem`, `BankTransaction`, `CafeMenuItem`, `CafeOrderItem`, `BusSchedule`, `LocationHotspot`).
- **Properties & Calculated Properties**: Auto-properties and computed properties such as `CafeOrderItem.LineTotal`.
- **Collections & Lists**: `List<T>` collections used across all four modules for dynamic in-memory management.
- **Loops & Conditional Logic**: `foreach` and `for` loops with `if / else` and `switch` statements for searching, filtering, summing balances, and validating forms.
- **Events & Navigation**: Mouse hover/click hit-testing (`OnMouseMove`, `OnMouseDown`, `OnMouseUp`), custom GDI+ `OnPaint` rendering, live search (`TextChanged`), and modal form navigation (`ShowDialog` + `Back to Town`).
- **Local File Persistence**: Safe `System.Text.Json` serialization and deserialization with graceful handling for missing files, empty files, and corrupted JSON recovery.

---

## 3. Technologies Used

| Component | Technology |
| :--- | :--- |
| **Language** | C# 12 |
| **Desktop UI Framework** | Windows Forms (`System.Windows.Forms`) |
| **Target Framework** | `.NET 8.0` (`net8.0-windows`) |
| **Graphics & Icons** | Custom 2D GDI+ (`System.Drawing` / `GraphicsPath` / `ColorMatrix` 75% opacity) |
| **Typography** | Bundled `Bebas Neue` condensed display font (`font.png` reference) + `Segoe UI` + `Consolas` |
| **Local Data Storage** | `System.Text.Json` (Zero external NuGet dependencies) |

---

## 4. Four Implemented Locations & Reference Mapping

The main town canvas renders `Resources/aerial.jpeg` at **75% opacity** while preserving its exact `1600 × 881` proportions on window resize. Building markers, branding, and the dashboard follow the exact annotations in `Resources/aerial - Copy.jpeg`:

1. **Library** *(Blue Arrow — Lower-Left Academic Building)*
   - Add books with validated **Book Title** and **Author Name** inputs (prevents empty or duplicate entries).
   - View all saved books in a clean formatted table.
   - Live case-insensitive **Search** by title or author, plus one-click search clearing.
   - Remove selected books with confirmation and automatic JSON saving.
   - Friendly empty states for both an empty library and zero-match searches.

2. **Bank** *(Yellow Arrow — Upper-Left Campus Building)*
   - Record **Income** (`+ Add Income`) and **Expense** (`− Add Expense`) transactions.
   - Calculates **Current Wallet Balance**, **Total Income**, and **Total Expenses** dynamically from actual transaction records (never hardcoded).
   - Validates monetary inputs (`decimal.TryParse`, positive amounts, non-empty descriptions).
   - Color-coded transaction history table with support for removing entries and recalculating balances.
   - Persists all transactions locally to JSON.

3. **Café** *(Black Arrow — Lower-Right Garden Courtyard & Pavilion)*
   - Displays a curated 8-item campus menu of **Drinks** and **Food** with unit prices in `Rs`.
   - Supports quantity selection (`−` / `+` buttons or direct numeric entry) with quantity validation.
   - Calculates line subtotals and the live **Total Bill** automatically.
   - Generates a clean, aligned monospaced **Order Receipt** (`Order #`, date/time, itemized breakdown, and total bill).
   - Supports **Generate Receipt**, **Complete Order**, **Remove Selected**, and **Clear Order** actions (session-based).

4. **Bus / Transport Hub** *(Purple Arrow — Central Bus Courtyard)*
   - Displays active campus shuttle schedules including **Bus Number**, **Route Name**, **Destination**, and **Departure Time**.
   - Allows adding new schedule entries with time parsing/validation (accepts formats like `08:30 AM` or `14:15` and normalizes to `hh:mm tt`).
   - Allows removing schedule entries and displays a clear empty state when no routes are registered.
   - Persists all bus schedules locally to JSON.

### Main Header & Live Town Dashboard

- **Top-Right Branding** *(Red Border Orchard Region)*: Displays **UOL CITY** and **Designed to Evolve** in high-contrast condensed typography with a subtle dark shadow for readability.
- **Lower-Left Dashboard** *(White Box Region)*: Semi-transparent rounded cards displaying four live values calculated from real data:
  1. **Locations** (`4`)
  2. **Library Books** (actual saved book count)
  3. **Wallet Balance** (actual calculated Bank balance)
  4. **Bus Routes** (actual saved bus schedule count)

---

## 5. Project Structure

```text
uolcity/
├── UOLCity.sln                      # Visual Studio 2022 Solution file
├── README.md                        # Project documentation & teacher demo guide
├── .gitignore                       # Standard .NET & Visual Studio ignore rules
└── UOLCity/
    ├── UOLCity.csproj               # .NET 8 Windows Forms project configuration
    ├── Program.cs                   # Application entry point ([STAThread] Main)
    ├── Models.cs                    # Data classes (BookItem, BankTransaction, CafeMenuItem, etc.)
    ├── TownDataStore.cs             # System.Text.Json storage, validation, search & calculations
    ├── UiTheme.cs                   # Shared palette, font loader, GDI+ vector icons & UI styling
    ├── MainForm.cs                  # Aerial town map, 75% opacity renderer, pins & live dashboard
    ├── LibraryForm.cs               # Library module (Book Collection Management)
    ├── BankForm.cs                  # Bank module (Personal Wallet & Ledger)
    ├── CafeForm.cs                  # Café module (Food/Drinks Ordering & Receipt Generator)
    ├── TransportForm.cs             # Bus / Transport Hub module (Shuttle Schedules)
    └── Resources/
        ├── aerial.jpeg              # Main campus aerial photograph background
        ├── aerial - Copy.jpeg       # Annotated building & layout reference image
        ├── font.png                 # Condensed display typography reference
        └── BebasNeue-Regular.ttf    # Bundled condensed display typeface for UOL CITY branding
```

---

## 6. Local JSON Storage & Error Handling

Persistent data is stored automatically in the user's local application data folder:

```text
%LocalAppData%\UOLCity\
├── library_books.json
├── bank_transactions.json
└── bus_schedules.json
```

`TownDataStore.cs` safely handles every storage scenario:

- **First-Time Initialization (Missing Files)**: Automatically creates `%LocalAppData%\UOLCity\` and initializes starter JSON records so the town dashboard and modules can be demonstrated immediately.
- **Existing Saved Data**: Loads and preserves all user-added records across application restarts without overwriting them.
- **Empty Files / All Items Removed**: Returns an empty `List<T>()` cleanly and displays the module's empty state without re-seeding starter records.
- **Invalid or Corrupted JSON**: Catches `JsonException`, saves a timestamped `.corrupt.bak` backup of the damaged file so user text is never lost, and resets cleanly without crashing the application.
- **Café Orders**: Kept intentionally as temporary in-memory session orders (`List<CafeOrderItem>`), matching project specifications.

---

## 7. Installation & Setup Requirements

- **Operating System**: Windows 10 or Windows 11 (64-bit recommended)
- **IDE**: **Visual Studio 2022** (Community, Professional, or Enterprise) with the **.NET desktop development** workload installed
- **SDK**: **.NET 8.0 SDK** (`net8.0-windows`)

---

## 8. How to Open & Run in Visual Studio 2022

1. Clone or download this repository:
   ```bash
   git clone https://github.com/ahsanjawed0991-sys/uolcity.git
   cd uolcity
   ```
2. Double-click **`UOLCity.sln`** to open the solution in **Visual Studio 2022**.
3. Ensure **`UOLCity`** is set as the Startup Project and configuration is **`Debug | Any CPU`**.
4. Press **`F5`** (Start Debugging) or **`Ctrl + F5`** (Start Without Debugging) to build and launch **UOL CITY**.

### Running via .NET CLI (Windows Command Prompt / PowerShell)

```powershell
dotnet restore UOLCity.sln
dotnet build UOLCity.sln -c Release
dotnet run --project UOLCity/UOLCity.csproj
```

---

## 9. Step-by-Step Teacher Demonstration Guide

Use this 3-minute walkthrough during your university midterm presentation:

1. **Main Aerial Town Screen (`MainForm.cs`)**
   - Point out `aerial.jpeg` rendered at **75% opacity**, the **UOL CITY — Designed to Evolve** branding in the top-right orchard area, the **four interactive location pins** positioned over their exact campus buildings, and the **four live dashboard cards** in the lower-left corner.
   - Briefly resize the window to show that the aerial photograph maintains its aspect ratio and all four location pins stay anchored to their buildings.

2. **Library Module (`LibraryForm.cs`)**
   - Click **Library** on the town map.
   - Try clicking **Add Book** with an empty title to demonstrate input validation.
   - Add a new book (e.g., *"C# Player's Guide"* by *"RB Whitaker"*), search for *"Whitaker"* in the search box, then click **← Back to Town** and point out that **Library Books** on the main dashboard increased automatically.

3. **Bank Module (`BankForm.cs`)**
   - Click **Bank** on the town map.
   - Try entering `"abc"` or `"-100"` in Amount to show monetary validation.
   - Record an **Income** of `2000` (*"Scholarship Stipend"*) and an **Expense** of `450` (*"Campus Lunch"*). Explain how `CalculateWalletBalance()` loops through `_transactions` to compute the balance dynamically.
   - Click **← Back to Town** and show the updated **Wallet Balance** on the dashboard.

4. **Café Module (`CafeForm.cs`)**
   - Click **Café** on the town map.
   - Select *Hot Cappuccino* (Qty `2`) and *Grilled Club Sandwich* (Qty `1`), clicking **Add Selected Item to Order**.
   - Point out the calculated **Total Bill** (`Rs 1,090.00`) and click **Complete Order** to display the formatted monospaced **Order Receipt**.

5. **Bus / Transport Hub Module (`TransportForm.cs`)**
   - Click **Transport Hub** near the parked campus buses.
   - Add Bus `UOL-05`, Route `Route E - Evening Loop`, Destination `City Terminal`, Departure `05:15 PM`.
   - Click **← Back to Town**, show the updated **Bus Routes** count on the dashboard, then close and reopen the app to prove all Library, Bank, and Bus records persisted via `System.Text.Json`!
