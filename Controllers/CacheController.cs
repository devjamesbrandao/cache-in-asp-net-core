using Caching.WebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InMemoryCacheController : ControllerBase
    {
        private readonly IMemoryCache memoryCache;

        public InMemoryCacheController(IMemoryCache memoryCache) => this.memoryCache = memoryCache; 


        /// <summary>
        /// Try get value of cache.
        /// </summary>
        /// <remarks>
        /// Requisition example:
        /// 
        ///     [GET] api/inMemoryCache/keyvalue
        /// </remarks>
        /// <param name="key">Search Key</param> 
        [Produces("application/json")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [HttpGet("{key}")]
        public ActionResult<string> GetCache(string key)
        {
            string value = string.Empty;

            memoryCache.TryGetValue(key, out value);

            return Ok(value);
        }

        /// <summary>
        /// Set value on cache.
        /// </summary>
        /// <remarks>
        /// Requisition example:
        /// 
        ///     [POST] api/inMemoryCache
        ///     {
        ///         "key": "test",
        ///         "value" : "this is an example"
        ///     }
        /// </remarks>
        /// <param name="data"></param> 
        [HttpPost]
        public ActionResult SetCache(CacheRequest data)
        {
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

            memoryCache.Set(data.key, data.value, cacheExpiryOptions);

            return NoContent();
        }
    }
}