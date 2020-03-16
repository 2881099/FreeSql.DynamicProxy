using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

public class AopProxyClass___5a956965b6fc4df88843afee3825c0a0 : MyClass
{
    private FreeSql.DynamicProxyMeta __DP_Meta = FreeSql.DynamicProxy.GetAvailableMeta(typeof(MyClass));

    //这里要注释掉，如果重写的基类没有无参构造函数，会报错
    //public AopProxyClass___5a956965b6fc4df88843afee3825c0a0(FreeSql.DynamicProxyMeta meta)
    //{
    //    __DP_Meta = meta;
    //}

    private System.IServiceProvider __DP_ARG___FromServices_0;

    public AopProxyClass___5a956965b6fc4df88843afee3825c0a0(System.IServiceProvider parameter__DP_ARG___FromServices_0)
        : base()
    {
        __DP_ARG___FromServices_0 = parameter__DP_ARG___FromServices_0;
    }

    private void __DP_ARG___attribute0_FromServicesCopyTo(FreeSql.DynamicProxyAttribute attr)
    {
        __DP_Meta.SetDynamicProxyAttributePropertyValue(0, attr, "_service", __DP_ARG___FromServices_0);
    }

    private void __DP_ARG___attribute1_FromServicesCopyTo(FreeSql.DynamicProxyAttribute attr)
    {
    }

    private void __DP_ARG___attribute2_FromServicesCopyTo(FreeSql.DynamicProxyAttribute attr)
    {
        __DP_Meta.SetDynamicProxyAttributePropertyValue(2, attr, "_service", __DP_ARG___FromServices_0);
    }

    private void __DP_ARG___attribute3_FromServicesCopyTo(FreeSql.DynamicProxyAttribute attr)
    {
    }


    public override System.String Get(System.String key)
    {
        Exception __DP_ARG___exception = null;
        var __DP_ARG___is_return = false;
        object __DP_ARG___return_value = null;
        var __DP_ARG___parameters = new Dictionary<string, object>(); __DP_ARG___parameters.Add("key", key);

        var __DP_ARG___attribute0 = __DP_Meta.CreateDynamicProxyAttribute(0);
        __DP_ARG___attribute0_FromServicesCopyTo(__DP_ARG___attribute0);
        var __DP_ARG___Before0 = new FreeSql.DynamicProxyBeforeArguments(this, FreeSql.DynamicProxyInjectorType.Method, __DP_Meta.MatchedMemberInfos[0], __DP_ARG___parameters, null);
        __DP_ARG___attribute0.Before(__DP_ARG___Before0);
        if (__DP_ARG___is_return == false)
        {
            __DP_ARG___is_return = __DP_ARG___Before0.Returned;
            if (__DP_ARG___is_return) __DP_ARG___return_value = __DP_ARG___Before0.ReturnValue;
        }

        try
        {
            if (__DP_ARG___is_return == false)
            {
                if (!object.ReferenceEquals(key, __DP_ARG___parameters["key"])) key = __DP_ARG___parameters["key"] as System.String;
                __DP_ARG___return_value = base.Get(key);
            }
        }
        catch (Exception __DP_ARG___ex)
        {
            __DP_ARG___exception = __DP_ARG___ex;
        }

        var __DP_ARG___After0 = new FreeSql.DynamicProxyAfterArguments(this, FreeSql.DynamicProxyInjectorType.Method, __DP_Meta.MatchedMemberInfos[0], __DP_ARG___parameters, __DP_ARG___return_value, __DP_ARG___exception);
        __DP_ARG___attribute0.After(__DP_ARG___After0);
        if (__DP_ARG___After0.Exception != null && __DP_ARG___After0.ExceptionHandled == false) throw __DP_ARG___After0.Exception;

        return __DP_ARG___return_value as System.String;
    }

    async public override System.Threading.Tasks.Task<System.String> GetAsync()
    {
        Exception __DP_ARG___exception = null;
        var __DP_ARG___is_return = false;
        object __DP_ARG___return_value = null;
        var __DP_ARG___parameters = new Dictionary<string, object>();

        var __DP_ARG___attribute1 = __DP_Meta.CreateDynamicProxyAttribute(1);
        __DP_ARG___attribute1_FromServicesCopyTo(__DP_ARG___attribute1);
        var __DP_ARG___Before1 = new FreeSql.DynamicProxyBeforeArguments(this, FreeSql.DynamicProxyInjectorType.Method, __DP_Meta.MatchedMemberInfos[1], __DP_ARG___parameters, null);
        await __DP_ARG___attribute1.Before(__DP_ARG___Before1);
        if (__DP_ARG___is_return == false)
        {
            __DP_ARG___is_return = __DP_ARG___Before1.Returned;
            if (__DP_ARG___is_return) __DP_ARG___return_value = __DP_ARG___Before1.ReturnValue;
        }
        var __DP_ARG___attribute2 = __DP_Meta.CreateDynamicProxyAttribute(2);
        __DP_ARG___attribute2_FromServicesCopyTo(__DP_ARG___attribute2);
        var __DP_ARG___Before2 = new FreeSql.DynamicProxyBeforeArguments(this, FreeSql.DynamicProxyInjectorType.Method, __DP_Meta.MatchedMemberInfos[2], __DP_ARG___parameters, null);
        await __DP_ARG___attribute2.Before(__DP_ARG___Before2);
        if (__DP_ARG___is_return == false)
        {
            __DP_ARG___is_return = __DP_ARG___Before2.Returned;
            if (__DP_ARG___is_return) __DP_ARG___return_value = __DP_ARG___Before2.ReturnValue;
        }

        try
        {
            if (__DP_ARG___is_return == false)
            {
                __DP_ARG___return_value = await base.GetAsync();
            }
        }
        catch (Exception __DP_ARG___ex)
        {
            __DP_ARG___exception = __DP_ARG___ex;
        }

        var __DP_ARG___After1 = new FreeSql.DynamicProxyAfterArguments(this, FreeSql.DynamicProxyInjectorType.Method, __DP_Meta.MatchedMemberInfos[1], __DP_ARG___parameters, __DP_ARG___return_value, __DP_ARG___exception);
        await __DP_ARG___attribute1.After(__DP_ARG___After1);
        if (__DP_ARG___After1.Exception != null && __DP_ARG___After1.ExceptionHandled == false) throw __DP_ARG___After1.Exception;
        var __DP_ARG___After2 = new FreeSql.DynamicProxyAfterArguments(this, FreeSql.DynamicProxyInjectorType.Method, __DP_Meta.MatchedMemberInfos[2], __DP_ARG___parameters, __DP_ARG___return_value, __DP_ARG___exception);
        await __DP_ARG___attribute2.After(__DP_ARG___After2);
        if (__DP_ARG___After2.Exception != null && __DP_ARG___After2.ExceptionHandled == false) throw __DP_ARG___After2.Exception;

        return __DP_ARG___return_value as System.String;
    }



    public override System.String Text
    {
        get
        {
            Exception __DP_ARG___exception = null;
            var __DP_ARG___is_return = false;
            object __DP_ARG___return_value = null;
            var __DP_ARG___parameters = new Dictionary<string, object>();

            var __DP_ARG___attribute3 = __DP_Meta.CreateDynamicProxyAttribute(3);
            __DP_ARG___attribute3_FromServicesCopyTo(__DP_ARG___attribute3);
            var __DP_ARG___Before3 = new FreeSql.DynamicProxyBeforeArguments(this, FreeSql.DynamicProxyInjectorType.PropertyGet, __DP_Meta.MatchedMemberInfos[3], __DP_ARG___parameters, null);
            __DP_ARG___attribute3.Before(__DP_ARG___Before3);
            if (__DP_ARG___is_return == false)
            {
                __DP_ARG___is_return = __DP_ARG___Before3.Returned;
                if (__DP_ARG___is_return) __DP_ARG___return_value = __DP_ARG___Before3.ReturnValue;
            }

            try
            {
                if (__DP_ARG___is_return == false) __DP_ARG___return_value = base.Text;
            }
            catch (Exception __DP_ARG___ex)
            {
                __DP_ARG___exception = __DP_ARG___ex;
            }

            var __DP_ARG___After3 = new FreeSql.DynamicProxyAfterArguments(this, FreeSql.DynamicProxyInjectorType.PropertyGet, __DP_Meta.MatchedMemberInfos[3], __DP_ARG___parameters, __DP_ARG___return_value, __DP_ARG___exception);
            __DP_ARG___attribute3.After(__DP_ARG___After3);
            if (__DP_ARG___After3.Exception != null && __DP_ARG___After3.ExceptionHandled == false) throw __DP_ARG___After3.Exception;

            return __DP_ARG___return_value as System.String;
        }
        set
        {
            base.Text = value;
        }
    }
}