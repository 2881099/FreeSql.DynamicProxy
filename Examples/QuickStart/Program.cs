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

    [Cache2(Key = "GetAsync")]
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

class Cache2Attribute : FreeSql.DynamicProxyAttribute
{
    public string Key { get; set; }

    public override Task Before(FreeSql.DynamicProxyArguments args)
    {
        return base.Before(args);
    }
    public override Task After(FreeSql.DynamicProxyArguments args)
    {
        return base.After(args);
    }
}


class CacheAttribute : FreeSql.DynamicProxyAttribute
{
    public string Key { get; set; }

    public override Task Before(FreeSql.DynamicProxyArguments args)
    {
        args.ReturnValue = $"{args.MemberInfo.Name} Before Changed";
        return base.Before(args);
    }
    public override Task After(DynamicProxyArguments args)
    {
        return base.After(args);
    }
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