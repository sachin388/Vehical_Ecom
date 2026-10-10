using ItemsPrice.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ItemsPrice.Application.Queries
{
    public class SearchPricesQuery
    {
        public PriceSearchRequest Request { get; set; } = new();
    }

}
