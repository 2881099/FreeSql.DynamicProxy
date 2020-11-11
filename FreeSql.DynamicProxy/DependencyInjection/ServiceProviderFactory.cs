#if net50 || ns21 || ns20

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace FreeSql
{
    public class DynamicProxyServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            return containerBuilder
                //.BuildServiceProvider()
                .BuildDynamicProxyProvider()
                ;
        }
    }
}

#endif