using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AspNetCore
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddScoped<MyClass1>();
            services.AddScoped<MyClass2>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    public class MyClass1
    {
        [Cache(Key = "Get")]
        public virtual string Get()
        {
            return "MyClass1.Get value";
        }

        [Cache(Key = "GetAsync")]
        async public virtual Task<string> GetAsync(string id, MyClass2 cls2333, DateTime now)
        {
            await Task.Yield();
            return "MyClass1.GetAsync value";
        }

        public virtual string Text
        {
            [Cache(Key = "Text")]
            get;
            set;
        }

        public string T2 { get; set; }
    }
    public class MyClass2
    {
        [Cache(Key = "Get")]
        public virtual string Get()
        {
            return "MyClass2.Get value";
        }

        [Cache(Key = "GetAsync")]
        async public virtual Task<string> GetAsync()
        {
            await Task.Yield();
            return "MyClass2.GetAsync value";
        }

        public virtual string Text
        {
            [Cache(Key = "Text")]
            get;
            set;
        }

        public string T2 { get; set; }
    }

    class CacheAttribute : FreeSql.DynamicProxyAttribute
    {
        public string Key { get; set; }

        [FreeSql.DynamicProxyFromServices]
        public IServiceProvider sp;

        public override Task Before(FreeSql.DynamicProxyBeforeArguments args)
        {
            args.ReturnValue = $"{args.MemberInfo.Name} Before Changed";
            return base.Before(args);
        }
        public override Task After(FreeSql.DynamicProxyAfterArguments args)
        {
            return base.After(args);
        }
    }
}
