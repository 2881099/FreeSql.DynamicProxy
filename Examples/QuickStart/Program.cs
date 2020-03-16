using FreeSql;
using System;
using System.Threading.Tasks;

public class MyClass
{

    [Cache2(Key = "Get")]
    public virtual string Get(string key)
    {
        return $"MyClass.Get({key}) value";
    }

    [Cache(Key = "GetAsync")]
    [Cache2(Key = "GetAsync")]
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

    public string T2 {
        get
        {
            return "";
        }
        set
        {
            value = "rgerg";
            Text = value;
        }
    }
}

class Cache2Attribute : FreeSql.DynamicProxyAttribute
{
    [DynamicProxyFromServices]
    public IServiceProvider _service;

    public string Key { get; set; }

    public override Task Before(FreeSql.DynamicProxyBeforeArguments args)
    {
        if (args.Parameters.ContainsKey("key"))
            args.Parameters["key"] = "Newkey";
        return base.Before(args);
    }
    public override Task After(FreeSql.DynamicProxyAfterArguments args)
    {
        return base.After(args);
    }
}


class CacheAttribute : FreeSql.DynamicProxyAttribute
{
    public string Key { get; set; }

    public override Task Before(FreeSql.DynamicProxyBeforeArguments args)
    {
        this.Key = "213234234";
        args.ReturnValue = $"{args.MemberInfo.Name} Before Changed";
        return base.Before(args);
    }
    public override Task After(DynamicProxyAfterArguments args)
    {
        args.ExceptionHandled = true;
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
        Console.WriteLine(pxy.Get("key"));
        Console.WriteLine(pxy.GetAsync().Result);
        pxy.Text = "testSetProp1";
        Console.WriteLine(pxy.Text);

        Console.WriteLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms\r\n");

        dt = DateTime.Now;
        pxy = new MyClass().ToDynamicProxy();
        Console.WriteLine(pxy.Get("key1"));
        Console.WriteLine(pxy.GetAsync().Result);
        pxy.Text = "testSetProp2";
        Console.WriteLine(pxy.Text);

        Console.WriteLine(DateTime.Now.Subtract(dt).TotalMilliseconds + " ms");
    }
}