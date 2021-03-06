﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using DataTransfer.AccessDatabase;
using LinkGreen.Applications.Common;
using LinkGreen.Applications.Common.Model;

namespace LinkGreenODBCUtility
{
    class Products : IOdbcTransfer
    {
        // public string ConnectionString = $"DSN={Settings.DsnName}";
        public string ClientConnectionString;

        public Products()
        {

        }

        public Products(string clientDsnName)
        {
            ClientConnectionString = "DSN=" + clientDsnName;
        }

        public bool Empty()
        {
            var productInventoryRepository = new ProductInventoryRepository(Settings.ConnectionString);
            productInventoryRepository.ClearAll();
            Logger.Instance.Info("Products LinkGreen transfer table emptied.");
            Logger.Instance.Debug($"{Settings.ConnectionString}.Products emptied.");
            return true;
        }

        public void SaveTableMapping(string dsnName, string tableName)
        {
            var productRepository = new ProductInventoryRepository(Settings.ConnectionString);
            productRepository.SaveTableMapping(dsnName, tableName, "Products");
            Logger.Instance.Debug($"Products table mapping saved: (DSN: {dsnName}, Table: {tableName})");
        }

        public void SaveFieldMapping(string fieldName, string mappingName)
        {
            var productRepository = new ProductInventoryRepository(Settings.ConnectionString);
            productRepository.SaveFieldMapping(fieldName, mappingName);
            Logger.Instance.Debug($"Products field mapping saved: (Field: {fieldName}, MappingField: {mappingName})");
        }

        public void UpdateTemporaryTables()
        {
            BatchTaskManager batchTaskManager = new BatchTaskManager("Products");
            List<string> commands = batchTaskManager.GetCommandsByTrigger();
            foreach (string cmd in commands)
            {
                Batch.Exec(cmd);
            }
        }

        public bool Publish(out List<string> publishDetails, BackgroundWorker bw = null)
        {
            publishDetails = new List<string>();
            string apiKey = Settings.GetApiKey();

            if (!string.IsNullOrEmpty(apiKey))
            {
                var products = new ProductInventoryRepository(Settings.ConnectionString).GetAll().ToList();
                var existingCategories = WebServiceHelper.GetAllCategories();

                var existingInventory = WebServiceHelper.GetAllInventory();
                var items = 0;

                var bulkPushRequest = new List<InventoryItemRequest>();


                var productsToAdd = products.Where(p => existingInventory.All(ei => ei.PrivateSKU != p.Id));
                // Add new items
                foreach (var product in productsToAdd)
                {
                    var request = AddOrUpdateSupplierItem(product, existingInventory, ref existingCategories);
                    WebServiceHelper.PushInventoryItem(request);

                    bw?.ReportProgress(0, $"Processing product sync (Pushing {++items}/{products.Count})\n\rPlease wait");
                    Logger.Instance.Debug($"Finished importing product {items} of {products.Count}. Id: {product.Id}");
                }

                // Update existing items
                foreach (var product in products.Where(p => existingInventory.Any(ei => ei.PrivateSKU == p.Id)))
                {
                    var request = AddOrUpdateSupplierItem(product, existingInventory, ref existingCategories);
                    if (request != null)
                        bulkPushRequest.Add(request);
                    if (bulkPushRequest.Count > 10)
                    {
                        WebServiceHelper.PushBulkUpdateInventoryItem(bulkPushRequest.ToArray());
                        bulkPushRequest.Clear();
                    }

                    bw?.ReportProgress(0, $"Processing product sync (Pushing {++items}/{products.Count})\n\rPlease wait");
                    Logger.Instance.Debug($"Finished importing product {items} of {products.Count}. Id: {product.Id}");
                }

                if (bulkPushRequest.Count > 0)
                {
                    WebServiceHelper.PushBulkUpdateInventoryItem(bulkPushRequest.ToArray());
                }


                WebServiceHelper.PostInventoryImport();
                publishDetails.Insert(0, $"{items} products published to LinkGreen");
                return true;
            }

            publishDetails.Insert(0, "No Api Key set while executing products publish.");
            Logger.Instance.Warning("No Api Key set while executing products publish.");

            return false;
        }

        private static InventoryItemRequest AddOrUpdateSupplierItem(ProductInventory product, List<InventoryItemResponse> existingInventory, ref List<PrivateCategory> existingCategories)
        {
            Trace.TraceInformation(product.Description);

            var request = new InventoryItemRequest
            {
                Description = product.Description ?? "",
                PrivateSKU = product.Id,
                Inactive = product.Inactive,
                QuantityAvailable = product.QuantityAvailable >= 1 ? product.QuantityAvailable : 1
            };

            bool updateCategories = Settings.GetUpdateCategories();
            //lets check if this item already exists, if so just update qty, else
            var existing = existingInventory.FirstOrDefault(s => s.PrivateSKU == product.Id);
            var existingCategory = existingCategories.FirstOrDefault(s => s?.Name == product.Category);

            var updateExistingProducts = Settings.GetUpdateExistingProducts();
            if (existing != null && !updateExistingProducts)
                return null;

            // Add
            if (existing == null)
            {
                if (existingCategory == null)
                {
                    existingCategory = WebServiceHelper.PushCategory(new PrivateCategory { Name = product.Category });
                    existingCategories.Add(existingCategory);
                }
            }
            // Update
            else
            {
                if (existingCategory == null && updateCategories)
                {
                    existingCategory = WebServiceHelper.PushCategory(new PrivateCategory { Name = product.Category });
                    existingCategories.Add(existingCategory);
                }
            }


            if (existingCategory != null && (updateCategories || existing == null))
            {
                request.CategoryId = existingCategory.Id;
            }
            else if (existing != null)
            {
                request.CategoryId = existing.CategoryId;
            }

            request.NetPrice = product.NetPrice;
            request.OpenSizeDescription = product.OpenSizeDescription ?? "";
            request.MasterQuantityDescription = product.MasterQuantityDescription ?? "";
            request.Comments = product.Comments ?? "";
            request.DirectDeliveryCode = product.DirectDeliveryCode ?? "";
            request.DirectDeliveryMinQuantity = product.DirectDeliveryMinQuantity;
            request.FreightFactor = product.FreightFactor;
            request.IsDirectDelivery = product.IsDirectDelivery;
            request.MasterQuantityDescription = product.MasterQuantityDescription ?? "";
            request.MinOrderSpring = product.MinOrderSpring;
            request.MinOrderSummer = product.MinOrderSummer;
            request.SlaveQuantityDescription = product.SlaveQuantityDescription ?? "";
            request.SlaveQuantityPerMaster = product.SlaveQuantityPerMaster;
            request.SuggestedRetailPrice = product.SuggestedRetailPrice;
            request.UPC = product.UPC;

            if (existing != null)
            {
                request.Id = existing.Id;
            }

            return request;
        }
    }
}
