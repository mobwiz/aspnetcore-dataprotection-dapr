// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Dapr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Dapr.Client;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.DataProtection.Dpar
{
    /// <summary>
    /// An XML repository backed by a Redis list entry.
    /// </summary>
    public class DaprXmlRepository : IXmlRepository
    {
        private readonly Func<DaprClient> _databaseFactory;
        private readonly string _key;
        private readonly string _storeName;

        private const string IndexKeyName = "_DaprXmlRepository_Indeics";

        /// <summary>
        /// Creates a <see cref="DaprClient"/> with keys stored at the given directory.
        /// </summary>
        /// <param name="databaseFactory">The delegate used to create <see cref="DaprClient"/> instances.</param>
        /// <param name="stateStoreName"></param>
        /// <param name="key">The <see cref="string"/> used to store key list.</param>
        public DaprXmlRepository(Func<DaprClient> databaseFactory, string stateStoreName, string key)
        {
            _databaseFactory = databaseFactory;
            _storeName = stateStoreName;
            _key = key;
        }

        /// <inheritdoc />
        public IReadOnlyCollection<XElement> GetAllElements()
        {
            return GetAllElementsCore().ToList().AsReadOnly();
        }

        private IEnumerable<XElement> GetAllElementsCore()
        {
            // Note: Inability to read any value is considered a fatal error (since the file may contain
            // revocation information), and we'll fail the entire operation rather than return a partial
            // set of elements. If a value contains well-formed XML but its contents are meaningless, we
            // won't fail that operation here. The caller is responsible for failing as appropriate given
            // that scenario.            
            var client = _databaseFactory();
            IndexData indexData = GetIndexData(client);

            foreach (var value in indexData.Keys)
            {
                var str = client.GetStateAsync<string>(_storeName, value).GetAwaiter().GetResult();
                yield return XElement.Parse(str);
            }
        }

        /// <inheritdoc />
        public void StoreElement(XElement element, string friendlyName)
        {
            //RedisHelper.LPush(_key, element.ToString(SaveOptions.DisableFormatting));

            var client = _databaseFactory();
            IndexData indexData = GetIndexData(client);

            var key = $"{_key}_{Guid.NewGuid().ToString("N")}";
            _databaseFactory().SaveStateAsync(_storeName, key, element.ToString(SaveOptions.DisableFormatting)).GetAwaiter().GetResult();
            indexData.Keys.Add(key);

            client.SaveStateAsync(_storeName, IndexKeyName, JsonConvert.SerializeObject(indexData)).GetAwaiter().GetResult();
        }

        internal class IndexData
        {
            public IList<string> Keys { get; set; }
        }

        private IndexData GetIndexData(DaprClient client)
        {
            IndexData indexData = new IndexData();
            try
            {
                var data = client.GetStateAsync<string>(_storeName, IndexKeyName).GetAwaiter().GetResult();
                if (!string.IsNullOrEmpty(data))
                {
                    indexData = JsonConvert.DeserializeObject<IndexData>(data);
                }
            }
            catch (Exception ex)
            {
            }

            return indexData;
        }
    }
}
