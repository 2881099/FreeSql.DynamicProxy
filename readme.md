轻量级 AOP 动态代理，支持 .NetCore 或 .NetFramework4.0+ 平台。

- 支持 同步/异步方法拦截；
- 支持 方法的参数值拦截，并支持修改参数值；
- 支持 属性拦截；
- 支持 多个拦截器同时生效；
- 支持 依赖注入的使用方式；
- 支持 动态接口实现；

> dotnet add package FreeSql.DynamicProxy

## 1、定义拦截器

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

- 拦截器和特性一起定义，合二为一；
- 私有字段可从Ioc反转获得对象，如上面的 _service；

## 2、开始拦截

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

## 3、Before/After 参数说明

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

## 4、AspNetCore 环境

第一步. 修改 Program.cs

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

第二步. 注入 CustomRepository

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<CustomRepository>();
    }
```

第三步. 创建 Controller

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

第四步. 控制台输出

```shell
Get Before
CustomRepository Get
Get After
```

## 5、动态接口实现

```csharp
var api = DynamicProxy.Resolve<IUserApi>();
api.Add(new UserInfo { Id = "001", Remark = "add" });
Console.WriteLine(JsonConvert.SerializeObject(api.Get("001")));

public interface IUserApi
{
    [HttpGet("api/user")]
    string Get(string id);
}

class HttpGetAttribute : FreeSql.DynamicProxyAttribute
{
    string _url;
    public HttpGetAttribute(string url)
    {
        _url = url;
    }
    public override Task Before(FreeSql.DynamicProxyBeforeArguments args)
    {
        args.ReturnValue = $"{args.MemberInfo.Name} HttpGet {_url}";
        return base.Before(args);
    }
}
```