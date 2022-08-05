using System.Text;
using Caching.WebApi.Data;
using Caching.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace Caching.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedisAndInMemoryController : ControllerBase
    {
        private readonly IMemoryCache memoryCache;
        private readonly ApplicationDbContext context;
        private readonly IDistributedCache distributedCache;

        public RedisAndInMemoryController(IMemoryCache memoryCache, ApplicationDbContext context, IDistributedCache distributedCache)
        {
            this.memoryCache = memoryCache;
            this.context = context;
            this.distributedCache = distributedCache;
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

            if (!memoryCache.TryGetValue(cacheKey, out List<Customer> customerList))
            {
                customerList = await context.Customers.ToListAsync();

                var cacheExpiryOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpiration = DateTime.Now.AddMinutes(5),
                    Priority = CacheItemPriority.High,
                    SlidingExpiration = TimeSpan.FromMinutes(2)
                };

                memoryCache.Set(cacheKey, customerList, cacheExpiryOptions);
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

            var redisCustomerList = await distributedCache.GetAsync(cacheKey);

            if (redisCustomerList != null)
            {
                serializedCustomerList = Encoding.UTF8.GetString(redisCustomerList);

                customerList = JsonConvert.DeserializeObject<List<Customer>>(serializedCustomerList);
            }
            else
            {
                customerList = await context.Customers.ToListAsync();

                serializedCustomerList = JsonConvert.SerializeObject(customerList);

                redisCustomerList = Encoding.UTF8.GetBytes(serializedCustomerList);

                var options = new DistributedCacheEntryOptions()
                    .SetAbsoluteExpiration(DateTime.Now.AddMinutes(10))
                    .SetSlidingExpiration(TimeSpan.FromMinutes(2));

                await distributedCache.SetAsync(cacheKey, redisCustomerList, options);
            }

            return Ok(customerList);
        }
    }
}