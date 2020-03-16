using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ValuesController : ControllerBase
    {
        [HttpGet("1")]
        public string Get([FromServices]CustomRepository repo, [FromQuery]string key)
        {
            Console.WriteLine(repo.Get(key));
            repo.Text = "Invalid value";
            Console.WriteLine(repo.Text);

            return "Get OK";
        }
        [HttpGet("2")]
        async public Task<string> GetAsync([FromServices]CustomRepository repo, [FromQuery]string key)
        {
            Console.WriteLine(await repo.GetAsync(key));
            repo.Text = "Invalid value";
            Console.WriteLine(repo.Text);

            return "GetAsync OK";
        }
    }
}
