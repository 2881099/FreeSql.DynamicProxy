using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FreeSql
{
    partial class DynamicProxy
    {
        /// <summary>
        /// 此接口由 Attribute 特性实现功能
        /// </summary>
        public interface IDynamicProxy
        {

            /// <summary>
            /// 同步方法执行之前
            /// </summary>
            /// <param name="args"></param>
            void Before(Arguments args);

            /// <summary>
            /// 同步方法执行之后
            /// </summary>
            /// <param name="args"></param>
            void After(Arguments args);


#if net40
#else
            /// <summary>
            /// 异步方法执行之前，处理返回值为 Task/Task&lt;T&gt; 的异步方法
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            Task BeforeAsync(Arguments args);
            /// <summary>
            /// 异步方法执行之前，处理返回值为 Task/Task&lt;T&gt; 的异步方法
            /// </summary>
            /// <param name="args"></param>
            /// <returns></returns>
            Task AfterAsync(Arguments args);
#endif

        }
    }
}
