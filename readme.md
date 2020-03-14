The dynamic proxy integration enables method calls on The .NetCore or .NetFramework4.0+, Support asynchronous method and property interception.

## Quick start

> dotnet add package FreeSql.DynamicProxy

Or

> Install-Package DotNetCore.CAP

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

class CacheAttribute : Attribute, FreeSql.DynamicProxy.IDynamicProxy
{
    public string Key { get; set; }

    public void Before(FreeSql.DynamicProxy.Arguments args)
    {
        if (args.MemberInfo.Name == "Get")
        {
            args.ReturnValue = "Before Get NewValue";
        }
        if (args.MemberInfo.Name == "Text")
        {
            args.ReturnValue = "Before Text NewValue";
        }
    }
    public void After(FreeSql.DynamicProxy.Arguments args)
    {
    }

    public Task BeforeAsync(FreeSql.DynamicProxy.Arguments args)
    {
        if (args.MemberInfo.Name == "GetAsync")
        {
            args.ReturnValue = "BeforeAsync GetAsync NewValue";
        }
        return Task.CompletedTask;
    }
    public Task AfterAsync(FreeSql.DynamicProxy.Arguments args)
    {
        return Task.CompletedTask;
    }
}

class Program
{
    static void Main(string[] args)
    {
        FreeSql.DynamicProxy.Test(typeof(MyClass)); //The first dynamic compilation was slow

        DateTime dt = DateTime.Now;
        var pxy = FreeSql.DynamicProxy.CreateInstanse<MyClass>();
        Console.WriteLine(pxy.Get());
        Console.WriteLine(pxy.GetAsync().Result);
        pxy.Text = "testSetProp1";
        Console.WriteLine(pxy.Text);

        Console.WriteLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms");

        dt = DateTime.Now;
        pxy = FreeSql.DynamicProxy.CreateInstanse<MyClass>();
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
86.2115 ms

Before Get NewValue
BeforeAsync GetAsync NewValue
Before Text NewValue
0.4034 ms
```