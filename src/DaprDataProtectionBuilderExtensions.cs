// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Dapr.Client;
using Microsoft.AspNetCore.DataProtection.Dpar;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.DataProtection
{
    /// <summary>
    /// Contains Redis-specific extension methods for modifying a <see cref="IDataProtectionBuilder"/>.
    /// </summary>
    public static class CSRedisDataProtectionBuilderExtensions
    {
        private const string DataProtectionKeysName = "DataProtection-Keys";

        /// <summary>
        /// Configures the data protection system to persist keys to specified key in Redis database
        /// </summary>  
        /// <param name="builder">The builder instance to modify.</param>
        /// <param name="databaseFactory">The delegate used to create instances.</param>
        /// <param name="storeName">Dapr state store name</param>
        /// <param name="key">The key used to store key list.</param>
        /// <returns>A reference to the <see cref="IDataProtectionBuilder" /> after this operation has completed.</returns>
        public static IDataProtectionBuilder PersistKeysToStackDapr(this IDataProtectionBuilder builder,
            Func<DaprClient> databaseFactory,
            string storeName,
            string key)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (databaseFactory == null)
            {
                throw new ArgumentNullException(nameof(databaseFactory));
            }
            return PersistKeysToStackExchangeRedisInternal(builder, databaseFactory, storeName, key);
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the default key ('DataProtection-Keys') in Redis database
        /// </summary>        
        public static IDataProtectionBuilder PersistKeysToStackCSRedis(this IDataProtectionBuilder builder, DaprClient connectionMultiplexer, string storeName)
        {
            return PersistKeysToStackCSRedis(builder, connectionMultiplexer, storeName, DataProtectionKeysName);
        }

        /// <summary>
        /// Configures the data protection system to persist keys to the specified key in Redis database
        /// </summary>        
        public static IDataProtectionBuilder PersistKeysToStackCSRedis(this IDataProtectionBuilder builder, DaprClient connectionMultiplexer, string storeName, string key)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (connectionMultiplexer == null)
            {
                throw new ArgumentNullException(nameof(connectionMultiplexer));
            }
            return PersistKeysToStackExchangeRedisInternal(builder, () => connectionMultiplexer, storeName, key);
        }

        private static IDataProtectionBuilder PersistKeysToStackExchangeRedisInternal(IDataProtectionBuilder builder,
            Func<DaprClient> databaseFactory,
            string storeName,
            string key)
        {
            builder.Services.Configure<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new DaprXmlRepository(databaseFactory, storeName, key);
            });
            return builder;
        }
    }
}