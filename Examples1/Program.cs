using FreeSql;
using System;
using System.Threading.Tasks;

namespace Examples1
{

    public class CustomAttribute : FreeSql.DynamicProxyAttribute
    {

        public override Task Before(DynamicProxyBeforeArguments args)
        {
            Console.WriteLine($"{args.MemberInfo.Name} Before");
            return base.Before(args);
        }

        public override Task After(DynamicProxyAfterArguments args)
        {
            Console.WriteLine($"{args.MemberInfo.Name} After");
            return base.After(args);
        }
    }
    public class CustomRepository
    {
        [Custom]
        public virtual string Get(string key)
        {
            Console.WriteLine($"CustomRepository Get");
            return $"CustomRepository.Get({key}) value";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            DependencyInjectionBuilder builder = new DependencyInjectionBuilder();
            // 依赖注入容器
            IServiceProvider services = builder
                .AddService<CustomRepository>().Build();

            // 获取服务
            CustomRepository cus = services.Get<CustomRepository>();
            
            // 执行方法
            cus.Get("source");

            Console.ReadKey();
        }
    }
}
