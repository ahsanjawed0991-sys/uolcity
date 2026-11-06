using System;

namespace UOLCity
{
    /// <summary>
    /// Identifies the exact four interactive locations in UOL CITY.
    /// </summary>
    public enum LocationType
    {
        Library,
        Bank,
        Cafe,
        TransportHub
    }

    /// <summary>
    /// Represents a single book saved in the UOL CITY Library module.
    /// Demonstrates a simple C# class with auto-implemented properties.
    /// </summary>
    public class BookItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string AddedDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Represents a single wallet transaction (Income or Expense) in the Bank module.
    /// </summary>
    public class BankTransaction
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

        /// <summary>
        /// Either "Income" or "Expense".
        /// </summary>
        public string Type { get; set; } = "Income";

        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd hh:mm tt");
    }

    /// <summary>
    /// Represents a food or drink item available on the Café menu.
    /// </summary>
    public class CafeMenuItem
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    /// <summary>
    /// Represents a selected menu item and quantity inside the current Café order.
    /// Demonstrates a calculated read-only property (LineTotal).
    /// </summary>
    public class CafeOrderItem
    {
        public string ItemName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        /// <summary>
        /// Calculates the subtotal for this order row: UnitPrice multiplied by Quantity.
        /// </summary>
        public decimal LineTotal => UnitPrice * Quantity;
    }

    /// <summary>
    /// Represents a single bus route and departure schedule entry in the Transport Hub module.
    /// </summary>
    public class BusSchedule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
        public string BusNumber { get; set; } = string.Empty;
        public string RouteName { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string DepartureTime { get; set; } = string.Empty;
    }
}
