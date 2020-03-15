#if ns20 || ns21

using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Generic;
using System.Linq;
using FreeSql;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionFreeSqlDynamicProxyExtensions
    {

        public static ServiceProvider BuildDynamicProxyProvider(this IServiceCollection services) => services.ToDynamicProxyService().BuildServiceProvider();
        public static ServiceProvider BuildDynamicProxyProvider(this IServiceCollection services, bool validateScopes) => services.ToDynamicProxyService().BuildServiceProvider(validateScopes);
        public static ServiceProvider BuildDynamicProxyProvider(this IServiceCollection services, ServiceProviderOptions options) => services.ToDynamicProxyService().BuildServiceProvider(options);

        public static IServiceCollection ToDynamicProxyService(this IServiceCollection services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var serviceProvider = services.BuildServiceProvider(false);
            var servicesDynamicProxy = new ServiceCollection();

            foreach (var service in services)
            {
                var implType = service.ImplementationType ?? service.ImplementationInstance?.GetType() ?? service.ImplementationFactory?.GetType().GetGenericArguments().FirstOrDefault();
                if (implType == null) continue;

                var meta = FreeSql.DynamicProxy.GetAvailableMeta(implType);
                if (meta != null)
                {
                    var serviceType = service.ServiceType.GetTypeInfo();
                    if (serviceType.IsClass)
                    {
                        servicesDynamicProxy.Add(ServiceDescriptor.Describe(service.ServiceType, meta.ProxyType, service.Lifetime));
                        continue;
                    }
                    if (serviceType.IsGenericTypeDefinition)
                    {
                        servicesDynamicProxy.Add(ServiceDescriptor.Describe(meta.SourceType, meta.ProxyType, service.Lifetime));
                        continue;
                    }
                    if (service.ImplementationInstance != null)
                    {
                        var implementationInstance = service.ImplementationInstance;
                        servicesDynamicProxy.Add(ServiceDescriptor.Describe(meta.SourceType, sp => implementationInstance.ToDynamicProxy(), service.Lifetime));
                        continue;
                    }
                    if (service.ImplementationFactory != null)
                    {
                        var implementationFactory = service.ImplementationFactory;
                        servicesDynamicProxy.Add(ServiceDescriptor.Describe(meta.SourceType, sp => implementationFactory(sp).ToDynamicProxy(), service.Lifetime));
                        continue;
                    }
                    if (service.ImplementationType != null)
                    {
                        var implementationType = service.ImplementationType;
                        servicesDynamicProxy.Add(ServiceDescriptor.Describe(meta.SourceType, sp => sp.GetService(implementationType).ToDynamicProxy(), service.Lifetime));
                        continue;
                    }
                }
                servicesDynamicProxy.Add(service);
            }

            serviceProvider.Dispose();
            return servicesDynamicProxy;
        }
    }
}

#endif