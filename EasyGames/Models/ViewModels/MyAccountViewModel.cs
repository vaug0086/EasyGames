using System;
using System.Collections.Generic;
using EasyGames.Models; // for Tier

namespace EasyGames.ViewModels
{
    public class MyAccountViewModel
    {
        public Tier CurrentTier { get; set; }
        public decimal LifetimeProfit { get; set; }
        /// <summary>Fraction 0..1. Your view multiplies by 100.</summary>
        public decimal PercentToNext { get; set; }
        /// <summary>Dollar target to reach the next tier.</summary>
        public decimal NextTierTarget { get; set; }

        public List<SaleRow> Sales { get; set; } = new();

        public class SaleRow
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string Channel { get; set; } = "";

            /// <summary>Total charged to customer (map from Order.GrandTotal).</summary>
            public decimal TotalSell { get; set; }

            /// <summary>Your internal cost (map from Order.TotalCost).</summary>
            public decimal TotalCost { get; set; }

            /// <summary>Profit = TotalSell - TotalCost (map from Order.TotalProfit).</summary>
            public decimal TotalProfit { get; set; }
        }
    }
}
