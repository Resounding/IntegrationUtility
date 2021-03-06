﻿using System;
using System.Collections.Generic;

namespace LinkGreen.Applications.Common.Model
{
    public class SupplierInventory
    {
        public string[] BuyerLinkedSkus { get; set; }
        public decimal? CatalogPrice { get; set; }
        public string Description { get; set; }
        public int? Inventory { get; set; }
        public int ItemId { get; set; }
        public string SizeDescription { get; set; }
        public ICollection<QuantityPricingBreak> Pricing { get; set; }
        public string SupplierSku { get; set; }
        // NOTE these aren't in the LinkGreen API payload
        public int? SupplierId { get; set; }
        public string OurSupplierNumber { get; set; }
        public string BuyerLinkedSku { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public SupplierInventory Clone()
        {
            return (SupplierInventory) MemberwiseClone();
        }
    }
}