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
    public class WeatherForecastController : ControllerBase
    {

        private readonly ILogger<WeatherForecastController> _logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger,
            MyClass1 cls1, MyClass2 cls2, MyClass1 cls11)
        {
            
        }

        [HttpGet("1")]
        public object Get([FromServices]MyClass1 cls1, [FromServices]MyClass2 cls2, [FromServices]MyClass1 cls11)
        {
            var sb = new StringBuilder();
            var dt = DateTime.Now;
            sb.AppendLine(cls1.Get());
            cls1.Text = "testSetProp1";
            sb.AppendLine(cls1.Text);
            sb.AppendLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

            dt = DateTime.Now;
            sb.AppendLine(cls2.Get());
            cls2.Text = "testSetProp2";
            sb.AppendLine(cls2.Text);
            sb.AppendLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

            dt = DateTime.Now;
            sb.AppendLine(cls11.Get());
            cls11.Text = "testSetProp3";
            sb.AppendLine(cls11.Text);
            sb.AppendLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

            return sb.ToString();
        }

        [HttpGet("2")]
        async public Task<string> GetAsync([FromServices]MyClass1 cls1, [FromServices]MyClass2 cls2, [FromServices]MyClass1 cls11)
        {
            var sb = new StringBuilder();
            var dt = DateTime.Now;
            sb.AppendLine(await cls1.GetAsync("id1", cls2, DateTime.Now));
            cls1.Text = "testSetProp1";
            sb.AppendLine(cls1.Text);
            sb.AppendLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

            dt = DateTime.Now;
            sb.AppendLine(await cls2.GetAsync());
            cls2.Text = "testSetProp2";
            sb.AppendLine(cls2.Text);
            sb.AppendLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

            dt = DateTime.Now;
            sb.AppendLine(await cls11.GetAsync("id22", cls2, DateTime.Now));
            cls11.Text = "testSetProp3";
            sb.AppendLine(cls11.Text);
            sb.AppendLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

            return sb.ToString();
        }
    }
}
