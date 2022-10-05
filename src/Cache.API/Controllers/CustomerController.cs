using System.Text;
using Cache.Core.Interfaces;
using Cache.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Caching.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedisAndInMemoryCacheController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ICustomerRepository _repository;
        private readonly IDistributedCache _distributedCache;

        public RedisAndInMemoryCacheController(
            IMemoryCache memoryCache, 
            ICustomerRepository repository, 
            IDistributedCache distributedCache
        )
        {
            _memoryCache = memoryCache;
            _repository = repository;
            _distributedCache = distributedCache;
        }


        /// <summary>
        /// Get all customers using InMemoryCache.
        /// </summary>
        /// <remarks>
        /// Requisition example:
        /// 
        ///     [GET] api/redisAndInMemory/memory
        /// </remarks>
        [HttpGet("memory")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Customer>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<Customer>>> GetAllCustomersUsingInMemoryCaching()
        {
            var cacheKey = "customerList";

            if (!_memoryCache.TryGetValue(cacheKey, out List<Customer> customerList))
            {
                customerList = await _repository.GetAllCustomersAsync();

                var cacheExpiryOptions = new MemoryCacheEntryOptions
                {
                    // With Absolute expiration, we can set the actual expiration of the cache entry. 
                    // Here it is set as 5 minutes. So, every 5 minutes, without taking into consideration the sliding expiration, the cache will be expired
                    AbsoluteExpiration = DateTime.Now.AddMinutes(5),
                    // Sets the priority of keeping the cache entry in the cache. The default setting is Normal. Other options are High, Low and Never Remove
                    Priority = CacheItemPriority.High,
                    // A defined Timespan within which a cache entry will expire if it is not used by anyone for this particular time period
                    SlidingExpiration = TimeSpan.FromMinutes(2),
                    // Allows you to set the size of this particular cache entry, so that it doesn’t start consuming the server resources
                    Size = 1024, // Size of cache
                };

                _memoryCache.Set(cacheKey, customerList, cacheExpiryOptions);
            }

            return Ok(customerList);
        }


        /// <summary>
        /// Get all customers using Redis Cache.
        /// </summary>
        /// <remarks>
        /// Requisition example:
        /// 
        ///     [GET] api/redisAndInMemory/redis
        /// </remarks>
        [HttpGet("redis")]
        public async Task<IActionResult> GetAllCustomersUsingRedisCache()
        {
            var cacheKey = "customerList";

            string serializedCustomerList;

            var customerList = new List<Customer>();

            var redisCustomerList = await _distributedCache.GetAsync(cacheKey);

            if (redisCustomerList != null)
            {
                serializedCustomerList = Encoding.UTF8.GetString(redisCustomerList);

                customerList = JsonConvert.DeserializeObject<List<Customer>>(serializedCustomerList);
            }
            else
            {
                customerList = await _repository.GetAllCustomersAsync();

                serializedCustomerList = JsonConvert.SerializeObject(customerList);

                redisCustomerList = Encoding.UTF8.GetBytes(serializedCustomerList);

                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(10))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

                await _distributedCache.SetAsync(cacheKey, redisCustomerList, options);
            }

            return Ok(customerList);
        }
    }
}