.Net ��������̬����֧�� .NetCore �� .NetFramework4.0+ ƽ̨��

- ֧�� ͬ��/�첽�������أ�
- ֧�� �����Ĳ���ֵ���أ���֧���޸Ĳ���ֵ��
- ֧�� �������أ�
- ֧�� ���������ͬʱ��Ч��
- ֧�� ����ע���ʹ�÷�ʽ��

> dotnet add package FreeSql.DynamicProxy

## ����������

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

- ������Ҳ�����Զ��壬�϶�Ϊһ��
- �������п��Զ���˽���ֶΣ���Ioc�з�ת������������������ _service��

## ��ʼ����

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

## Before/After ����˵��

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

## AspNetCore ʹ��

1. �޸� Program.cs

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

2. ע�� CustomRepository

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<CustomRepository>();
    }
```

3. ���� Controller

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

4. ����̨���

```shell
Get Before
CustomRepository Get
Get After
```
