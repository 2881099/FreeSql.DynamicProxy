using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Examples1
{
    /// <summary>
    /// 依赖注入服务
    /// </summary>
    public class DependencyInjectionBuilder
    {
        private IServiceCollection _services;
        public DependencyInjectionBuilder()
        {
            _services = new ServiceCollection();
        }

        /// <summary>
        /// 注入服务
        /// </summary>
        /// <typeparam name="TIService">接口</typeparam>
        /// <typeparam name="TService">服务</typeparam>
        public DependencyInjectionBuilder AddService<TIService, TService>()
            where TIService : class
            where TService : class, TIService
        {
            _services.AddTransient<TIService, TService>();
            return this;
        }

        /// <summary>
        /// 注入服务
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        public DependencyInjectionBuilder AddService<TService>()
            where TService : class
        {
            _services.AddTransient<TService>();
            return this;
        }

        public IServiceProvider Build()
        {
            // var serviceProvider = _services.BuildServiceProvider()
            // BuildDynamicProxyProvider 是 Freesql.DynamicProxy 的拓展方法
            var serviceProvider = _services.BuildDynamicProxyProvider();
            return serviceProvider;
        }
    }

    public static class DependencyInjectionExtensions
    {
        public static TService Get<TService>(this IServiceProvider provider)
        {
            return (TService)provider.GetService(typeof(TService));
        }
    }
}
