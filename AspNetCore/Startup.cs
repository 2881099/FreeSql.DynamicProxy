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
            services.AddIdentityServer();
            services.AddScoped<CustomRepository>();
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

    public class CustomRepository
    {

        [Custom]
        public virtual string Get(string key)
        {
            return $"CustomRepository.Get({key}) value";
        }

        [Custom]
        async public virtual Task<string> GetAsync(string key)
        {
            await Task.Yield();
            return $"CustomRepository.GetAsync({key}) value";
        }

        public virtual string Text
        {
            [Custom]
            get;
            set;
        }
    }

    class CustomAttribute : FreeSql.DynamicProxyAttribute
    {
        //Inversion of control
        [FreeSql.DynamicProxyFromServices]
        IServiceProvider _service;

        public override Task Before(FreeSql.DynamicProxyBeforeArguments args)
        {
            Console.WriteLine($"{args.MemberInfo.Name} Before");
            //args.Parameters["key"] = "NewKey";
            //args.ReturnValue = $"{args.MemberInfo.Name} Before Changed";
            return base.Before(args);
        }
        public override Task After(FreeSql.DynamicProxyAfterArguments args)
        {
            Console.WriteLine($"{args.MemberInfo.Name} After");
            return base.After(args);
        }
    }
}
