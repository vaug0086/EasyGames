using System.Collections.Generic;
using EasyGames.Models;
/* 
 * It's a view model. Used by the catalogue contoller. Basically just a one for one from the adding models page, ngl.
 * What am I even meant to say? It provides a list of stocitem objects for the oage, it holds the state of search text,
 * the selected category, page, pagesize, etc. 
*/

/*  References
 *  https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-mvc-app/adding-model?view=aspnetcore-9.0&tabs=visual-studio
 */
namespace EasyGames.Models.ViewModels
{
    //  Pretty basic view model that creates the variables we need for listing, paging and filters
    public class CatalogueFilterViewModel
    {
        public string? Search { get; set; }
        public StockCategory? Category { get; set; }
        public string? Sort { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; }
        public List<StockItem> Items { get; set; } = new();
    }
}
