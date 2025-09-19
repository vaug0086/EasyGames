using System;
using System.Collections.Generic;
using EasyGames.Models;

namespace EasyGames.ViewModels
{
    //  holds the list of order entities for the current page of results
    //  carries filter criteria like status free text q date range from and to
    //  carries paging data and sort choice so the view can render controls
    public class OrdersAdminIndexVm
    {
        public List<Order> Items { get; set; } = new();

        public string? Status { get; set; }
        public string? Q { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string? Sort { get; set; }
    }
}
