The dynamic proxy integration enables method calls on The .NetCore or .NetFramework4.0+.

- Support asynchronous method and property interception
- One method supports multiple AOP features and can take effect at the same time

## Install

> dotnet add package FreeSql.DynamicProxy

> Install-Package FreeSql.DynamicProxy

## AspNetCore

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

## Console

```csharp
using System;
using System.Threading.Tasks;

public class MyClass
{

    [Cache(Key = "Get")]
    public virtual string Get()
    {
        return "MyClass.Get value";
    }

    [Cache(Key = "GetAsync")]
    async public virtual Task<string> GetAsync()
    {
        await Task.Yield();
        return "MyClass.GetAsync value";
    }

    public virtual string Text
    {
        [Cache(Key = "Text")]
        get; 
        set;
    }
}

class CacheAttribute : FreeSql.DynamicProxyAttribute
{
    public string Key { get; set; }

    public override Task Before(FreeSql.DynamicProxyBeforeArguments args)
    {
        args.ReturnValue = $"{args.MemberInfo.Name} Before Changed";
        return base.Before(args);
    }
    public override Task After(DynamicProxyAfterArguments args)
    {
        return base.After(args);
    }
}

class Program
{
    static void Main(string[] args)
    {
        FreeSql.DynamicProxy.GetAvailableMeta(typeof(MyClass)); //The first dynamic compilation was slow

        var dt = DateTime.Now;
        var pxy = new MyClass { T2 = "123123" }.ToDynamicProxy();
        Console.WriteLine(pxy.Get());
        Console.WriteLine(pxy.GetAsync().Result);
        pxy.Text = "testSetProp1";
        Console.WriteLine(pxy.Text);

        Console.WriteLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

        dt = DateTime.Now;
        pxy = new MyClass().ToDynamicProxy();
        Console.WriteLine(pxy.Get());
        Console.WriteLine(pxy.GetAsync().Result);
        pxy.Text = "testSetProp2";
        Console.WriteLine(pxy.Text);

        Console.WriteLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms");
    }
}
```

Console output:

```shell
Before Get NewValue
BeforeAsync GetAsync NewValue
Before Text NewValue
30.8417 ms

Before Get NewValue
BeforeAsync GetAsync NewValue
Before Text NewValue
0.3338 ms
```