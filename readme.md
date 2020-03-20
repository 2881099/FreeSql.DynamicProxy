.Net 轻量级动态代理，支持 .NetCore 或 .NetFramework4.0+ 平台。

- 支持 同步/异步方法拦截；
- 支持 方法的参数值拦截，并支持修改参数值；
- 支持 属性拦截；
- 支持 多个拦截器同时生效；
- 支持 依赖注入的使用方式；

> dotnet add package FreeSql.DynamicProxy

## 定义拦截器

```csharp
class CustomAttribute : FreeSql.DynamicProxyAttribute
{
    [FreeSql.DynamicProxyFromServices]
    IServiceProvider _service;

    public override Task Before(FreeSql.DynamicProxyBeforeArguments args)
    {
        Console.WriteLine($"{args.MemberInfo.Name} Before");
        return base.Before(args);
    }
    public override Task After(FreeSql.DynamicProxyAfterArguments args)
    {
        Console.WriteLine($"{args.MemberInfo.Name} After");
        return base.After(args);
    }
}
```

- 拦截器也是特性定义，合二为一；
- 拦截器中可以定义私有字段，从Ioc中反转获得容器对象，如上面的 _service；

## 开始拦截

```csharp
public class CustomRepository
{
    [Custom]
    public virtual string Get(string key)
    {
        Console.WriteLine($"CustomRepository Get");
        return $"CustomRepository.Get({key}) value";
    }
}
```

- 拦截的方法须使用修饰符 virtual；

## Before/After 参数说明

1. Before args

| Property | Type | Notes |
| -- | -- | -- |
| Sender | Object | 代理对象 |
| InjectorType | Enum | Method, PropertyGet, PropertySet |
| MemberInfo | MemberInfo | 反射信息 |
| Parameters | Dictionary\<string, object\> | 方法的参数列表 |
| ReturnValue | Object | 设置方法的返回值 |

> 拦截并修改方法的参数值： args.Parameters["key"] = "NewKey";

> 拦截方法的返回值：args.ReturnValue = "NewValue";

2. After args

| Property | Type | Notes |
| -- | -- | -- |
| Sender | Object | 代理对象 |
| InjectorType | Enum | Method, PropertyGet, PropertySet |
| MemberInfo | MemberInfo | 反射信息 |
| Parameters | Dictionary\<string, object\> | 方法的参数列表 |
| ReturnValue | Object | 获取方法的返回值 |
| Exception | Exception | 原方法执行的异常对象 |
| ExceptionHandled | bool | 控制原方法执行发生异常后的行为 |

> ExceptionHandled：False: 抛出异常 (默认), True: 忽略异常 (继续执行)

## AspNetCore 使用

1. 修改 Program.cs

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            })
            .UseServiceProviderFactory(new FreeSql.DynamicProxyServiceProviderFactory());
}
```

2. 注入 CustomRepository

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<CustomRepository>();
    }
```

3. 创建 Controller

```csharp
public class ValuesController : ControllerBase
{
    [HttpGet("1")]
    public string Get([FromServices]CustomRepository repo, [FromQuery]string key)
    {
        return repo.Get(key);
    }
}
```

4. 控制台输出

```shell
Get Before
CustomRepository Get
Get After
```
