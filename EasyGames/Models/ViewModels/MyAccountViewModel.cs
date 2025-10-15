using System;
using System.Collections.Generic;
using EasyGames.Models; // for Tier

namespace EasyGames.ViewModels
{
    public class PurchaseRowVM
    {
        public DateTime CreatedUtc { get; set; }
        public string Channel { get; set; } = "";
        public decimal Total { get; set; } 
    }

    public class MyAccountViewModel
    {
        public string CurrentTier { get; set; } = "Bronze";
        public decimal LifetimeContribution { get; set; } // your “profit contribution”
        public decimal NextTierTarget { get; set; }       // e.g., 200.00
        public int ProgressPercent { get; set; }          // 0..100
        public bool IsStaff { get; set; }                 // toggle which table columns to show
        public List<PurchaseRowVM> Rows { get; set; } = new();
    }
}
