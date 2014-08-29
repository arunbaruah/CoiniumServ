﻿#region License
// 
//     CoiniumServ - Crypto Currency Mining Pool Server Software
//     Copyright (C) 2013 - 2014, CoiniumServ Project - http://www.coinium.org
//     http://www.coiniumserv.com - https://github.com/CoiniumServ/CoiniumServ
// 
//     This software is dual-licensed: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//    
//     For the terms of this license, see licenses/gpl_v3.txt.
// 
//     Alternatively, you can license this software under a commercial
//     license or white-label it as set out in licenses/commercial.txt.
// 
#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using CoiniumServ.Configuration;
using CoiniumServ.Factories;
using Newtonsoft.Json;
using Serilog;

namespace CoiniumServ.Pools
{
    public class PoolManager : IPoolManager
    {
        private readonly IList<IPool> _storage; 

        private readonly ILogger _logger;

        public PoolManager(IObjectFactory objectFactory , IConfigManager configManager)
        {
            _logger = Log.ForContext<PoolManager>();

            _storage = new List<IPool>(); // initialize the pool storage.

            foreach (var config in configManager.PoolConfigs) // loop through all enabled pool configurations.
            {
                var pool = objectFactory.GetPool(config); // create pool for the given configuration.
                _storage.Add(pool); // add it to storage.
            }
        }

        public IQueryable<IPool> SearchFor(Expression<Func<IPool, bool>> predicate)
        {
            return _storage.AsQueryable().Where(predicate);
        }
        public IEnumerable<IPool> GetAll()
        {
            return _storage;
        }

        public IQueryable<IPool> GetAllAsQueryable()
        {
            return _storage.AsQueryable();
        }

        public IReadOnlyCollection<IPool> GetAllAsReadOnly()
        {
            return new ReadOnlyCollection<IPool>(_storage);
        }

        public string ServiceResponse { get; private set; }

        public void Recache()
        {
            foreach (var pool in _storage) // recache per-pool stats
            {
                pool.Recache();
            }

            var cache = _storage.ToDictionary(pool => pool.Config.Coin.Symbol);
            ServiceResponse = JsonConvert.SerializeObject(cache, Formatting.Indented, new JsonSerializerSettings // cache the json-service response.
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore // ignore circular dependencies
            });           
        }

        public IPool Get(string symbol)
        {
            return _storage.FirstOrDefault(p => p.Config.Coin.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
        }

        public void Run()
        {
            foreach (var pool in _storage)
            {
                pool.Start();
            }
        }
    }
}
