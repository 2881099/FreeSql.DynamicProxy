The dynamic proxy integration enables method calls on The .NetCore or .NetFramework4.0+.

- Support asynchronous method interception
- Support method parameter interception
- Support property interception
- Support multiple intercepts
- Support for dependency injection and inversion of control

## Step 1: Install

> dotnet add package FreeSql.DynamicProxy

> Install-Package FreeSql.DynamicProxy

## Step 2: Defining Attributes

```csharp
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
```

1. Before Arguments

| Property | Type | Notes |
| -- | -- | -- |
| Sender | Object | Proxy Object |
| InjectorType | Enum | Method, PropertyGet, PropertySet |
| MemberInfo | MemberInfo | Reflection information |
| Parameters | Dictionary\<string, object\> | Method execution parameters, Parameters: Values can be modified (Intercept) |
| ReturnValue | Object | Intercept the original method and set the return value |

2. After Arguments

| Property | Type | Notes |
| -- | -- | -- |
| Sender | Object | Proxy Object |
| InjectorType | Enum | Method, PropertyGet, PropertySet |
| MemberInfo | MemberInfo | Reflection information |
| Parameters | Dictionary\<string, object\> | Method execution parameters |
| ReturnValue | Object | Return value of method |
| Exception | Exception | Exception information of original method execution |
| ExceptionHandled | bool | Handle exceptions when exceptions occur, False: throw exception (default), True: ignore exception (continue) |

## Step 3: Interceptor method

```csharp
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
```

## Step 4: Testing in AspNetCore


1. Use ServiceProviderFactory

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
            UseServiceProviderFactory.(new FreeSql.DynamicProxyServiceProviderFactory());
}
```

2. Add Dependency injection

```csharp
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddScoped<CustomRepository>();
    }
```

3. Create Controller

```csharp
public class ValuesController : ControllerBase
{
    [HttpGet("1")]
    public string Get([FromServices]CustomRepository repo, [FromQuery]string key)
    {
        Console.WriteLine(repo.Get(key));
        repo.Text = "Invalid value";
        Console.WriteLine(repo.Text);

        return "Get OK";
    }
    [HttpGet("2")]
    async public Task<string> GetAsync([FromServices]CustomRepository repo, [FromQuery]string key)
    {
        Console.WriteLine(await repo.GetAsync(key));
        repo.Text = "Invalid value";
        Console.WriteLine(repo.Text);

        return "GetAsync OK";
    }
}
```

4. Console Output

```shell
Get Before
Get After
CustomRepository.Get(test1) value
Text Before
Text After
Invalid value

GetAsync Before
GetAsync After
CustomRepository.GetAsync(test2) value
Text Before
Text After
Invalid value
```