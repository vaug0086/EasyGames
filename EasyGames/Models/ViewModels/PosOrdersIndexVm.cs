using System;
using System.Collections.Generic;
using EasyGames.Models;

namespace EasyGames.ViewModels
{
    public class PosOrdersIndexVm
    {
        // Context
        public List<Shop> UserShops { get; set; } = new();
        public Shop SelectedShop { get; set; } = default!;

        // Results
        public List<Order> Items { get; set; } = new();
        public int TotalCount { get; set; }

        // Filters
        public string? Q { get; set; }
        public string? Status { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        // Paging/sorting
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Sort { get; set; } = "newest";
    }
}
