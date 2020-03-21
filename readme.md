������ AOP ��̬����֧�� .NetCore �� .NetFramework4.0+ ƽ̨��

- ֧�� ͬ��/�첽�������أ�
- ֧�� �����Ĳ���ֵ���أ���֧���޸Ĳ���ֵ��
- ֧�� �������أ�
- ֧�� ���������ͬʱ��Ч��
- ֧�� ����ע���ʹ�÷�ʽ��
- ֧�� ��̬�ӿ�ʵ�֣�

> dotnet add package FreeSql.DynamicProxy

## 1������������

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

- ������������һ���壬�϶�Ϊһ��
- ˽���ֶοɴ�Ioc��ת��ö���������� _service��

## 2����ʼ����

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

- ���صķ�����ʹ�����η� virtual��

## 3��Before/After ����˵��

1. Before args

| Property | Type | Notes |
| -- | -- | -- |
| Sender | Object | ������� |
| InjectorType | Enum | Method, PropertyGet, PropertySet |
| MemberInfo | MemberInfo | ������Ϣ |
| Parameters | Dictionary\<string, object\> | �����Ĳ����б� |
| ReturnValue | Object | ���÷����ķ���ֵ |

> ���ز��޸ķ����Ĳ���ֵ�� args.Parameters["key"] = "NewKey";

> ���ط����ķ���ֵ��args.ReturnValue = "NewValue";

2. After args

| Property | Type | Notes |
| -- | -- | -- |
| Sender | Object | ������� |
| InjectorType | Enum | Method, PropertyGet, PropertySet |
| MemberInfo | MemberInfo | ������Ϣ |
| Parameters | Dictionary\<string, object\> | �����Ĳ����б� |
| ReturnValue | Object | ��ȡ�����ķ���ֵ |
| Exception | Exception | ԭ����ִ�е��쳣���� |
| ExceptionHandled | bool | ����ԭ����ִ�з����쳣�����Ϊ |

> ExceptionHandled��False: �׳��쳣 (Ĭ��), True: �����쳣 (����ִ��)

## 4��AspNetCore ����

��һ��. �޸� Program.cs

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

�ڶ���. ע�� CustomRepository

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<CustomRepository>();
    }
```

������. ���� Controller

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

���Ĳ�. ����̨���

```shell
Get Before
CustomRepository Get
Get After
```

## 5����̬�ӿ�ʵ��

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