The dynamic proxy integration enables method calls on The .NetCore or .NetFramework4.0+, Support asynchronous method and property interception.

## Quick start

> dotnet add package FreeSql.DynamicProxy

> Install-Package FreeSql.DynamicProxy

```csharp
using FreeSql;
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

    public string T2 { get; set; }
}

class CacheAttribute : FreeSql.DynamicProxyAttribute
{
    public string Key { get; set; }

    public override void Before(FreeSql.DynamicProxyArguments args)
    {
        args.ReturnValue = $"{args.MemberInfo.Name} Before Changed";
    }
    public override void After(FreeSql.DynamicProxyArguments args)
    {
    }

    //Intercept asynchronous methods, Comment code will execute synchronization method
    //public override Task BeforeAsync(FreeSql.DynamicProxyArguments args)
    //{
    //    args.ReturnValue = string.Concat(args.ReturnValue, " BeforeAsync Changed");
    //    return Task.CompletedTask;
    //}
}

class Program
{
    static void Main(string[] args)
    {
        FreeSql.DynamicProxy.GetAvailableMeta(typeof(MyClass)); //The first dynamic compilation was slow

        DateTime dt = DateTime.Now;
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