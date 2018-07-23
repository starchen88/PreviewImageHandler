using System;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Xml.Serialization;

namespace MicroAPI
{
    public class MicroAPIAsync : IHttpAsyncHandler
    {
        /// <summary>
        /// 存储字符串和Action的字典，使用SortedDictionary以提升性能
        /// </summary>
        protected SortedDictionary<string, Action<HttpContext>> actionMap = new SortedDictionary<string, Action<HttpContext>>();

        private static System.Web.Script.Serialization.JavaScriptSerializer serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
        /// <summary>
        /// 全局json序列化函数
        /// </summary>
        public static Func<object, string> JsonSerializeFunc = serializer.Serialize;

        /// <summary>
        /// 全局xml序列化函数
        /// </summary>
        public static Func<object, string> XmlSerializeFunc = obj =>
        {
            XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, obj);
                return textWriter.ToString();
            }
        };

        /// <summary>
        /// 异常处理程序，返回值false表示当前并为处理好异常，将继续使用throw抛出异常；如果返回true，则不再抛出异常
        /// 比如当您需要返回自己设计的错误提示时，可以自行输出并返回true
        /// </summary>
        public static Func<HttpContext, Exception, bool> ExceptionHandler = (context, ex) =>
        {
            System.Diagnostics.Trace.WriteLine(ex);
            return false;
        };

        /// <summary>
        /// 注册无参数操作，直接使用时需要自行完成Request操作，一般用于输出二进制流
        /// </summary>
        /// <param name="name">pathinfo标识</param>
        /// <param name="action"></param>
        protected void RegActionAsync(string name, Action<HttpContext> action)
        {
#if DEBUG
            if (actionMap.ContainsKey("/" + name))
            {
                throw new ArgumentException("pathinfo已存在，不能注册");
            }
#endif
            actionMap.Add("/" + name, action);
        }
        /// <summary>
        /// 注册返回纯文本的操作
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        protected void RegTextActionAsync(string name, Func<HttpContext, string> action)
        {
            Action<HttpContext> a = context =>
            {
                string msg = action(context);
                context.Response.ContentType = "text/plain";
                context.Response.Write(msg);
            };
            RegActionAsync(name, a);
        }
        /// <summary>
        /// 注册返回json的操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="action"></param>
        protected void RegJsonActionAsync<T>(string name, Func<HttpContext, T> action)
        {
            Action<HttpContext> a = context =>
            {
                var result = action(context);
                string msg = JsonSerializeFunc(result);
                context.Response.ContentType = "application/json";
                context.Response.Write(msg);
            };
            RegActionAsync(name, a);
        }
        /// <summary>
        /// 注册返回xml的操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="action"></param>
        protected void RegXmlActionAsync<T>(string name, Func<HttpContext, T> action)
        {
            Action<HttpContext> a = context =>
            {
                var result = action(context);
                string msg = XmlSerializeFunc(result);
                context.Response.ContentType = "text/xml";
                context.Response.Write(msg);
            };
            RegActionAsync(name, a);
        }
        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            var name = context.Request.PathInfo;
            if (actionMap.ContainsKey(name))
            {
                var currentAction = actionMap[name];

                try
                {
                    return currentAction.BeginInvoke(context, cb, Tuple.Create(name, context));
                }
                catch (Exception ex)
                {
                    if (!ExceptionHandler(context, ex))
                    {
                        throw ex;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException("pathinfo:" + name + "跟当前注册的操作均不匹配");
            }
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            var extraData = (Tuple<string, HttpContext>)result.AsyncState;
            var name = extraData.Item1;
            var context = extraData.Item2;
            try
            {

                var currentAction = actionMap[name];
                currentAction.EndInvoke(result);
            }
            catch (Exception ex)
            {
                if (!ExceptionHandler(context, ex))
                {
                    throw ex;
                }
            }
        }

        public virtual bool IsReusable
        {
            get { return false; }
        }
        public void ProcessRequest(HttpContext context)
        {
            throw new NotSupportedException();

        }
    }

    /// <summary>
    /// 泛型版的MicroAPIAsync主要是针对处理请求是需要某些前置参数，比如仅限授权用户使用，TState就是User实例，您需要实现GetState来告知如何获取User，并根据StateNullHandler操作决定当User为空时是否继续执行
    /// </summary>
    /// <typeparam name="TState">前置设置的状态类型</typeparam>
    public abstract class MicroAPIAsync<TState> : MicroAPIAsync
    {
        /// <summary>
        /// 全局获取TState的操作，您需要在接收请求之前设置内部逻辑
        /// 同一个TState只需要设置一次GetState，您可以在Global.asax或者app_start中进行
        /// </summary>
        public static Func<HttpContext, TState> GetState;
        /// <summary>
        /// 当State为空时的处理方式，当返回true时将继续执行action，返回false时不继续执行并抛出异常
        /// </summary>
        public static Func<HttpContext, bool> StateNullHandler = context =>
        {
            return false;
        };

        protected void RegActionAsync(string name, Action<HttpContext, TState> action)
        {
            Action<HttpContext> a = context =>
            {
                //为了提升性能，只在调试模式下检查GetState为空，请在正式发布时确保添加了GetState
#if DEBUG
                if (GetState == null)
                {
                    throw new Exception("在开始之前您需要先设置GetState逻辑");
                }
#endif
                var state = GetState(context);
                if (EqualityComparer<TState>.Default.Equals(state, default(TState)))
                {
                    throw new Exception("State为空");
                }
                else
                {
                    action(context, state);
                }
            };
            RegActionAsync(name, a);
        }

        /// <summary>
        /// 注册返回纯文本的操作
        /// </summary>
        /// <param name="name"></param>
        /// <param name="action"></param>
        protected void RegTextActionAsync(string name, Func<HttpContext, TState, string> action)
        {
            Action<HttpContext, TState> a = (context, state) =>
            {
                string msg = action(context, state);
                context.Response.ContentType = "text/plain";
                context.Response.Write(msg);
            };
            RegActionAsync(name, a);
        }

        /// <summary>
        /// 注册返回json的操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="action"></param>
        protected void RegJsonActionAsync<T>(string name, Func<HttpContext, TState, T> action)
        {
            Action<HttpContext, TState> a = (context, state) =>
            {
                var result = action(context, state);
                string msg = JsonSerializeFunc(result);
                context.Response.ContentType = "application/json";
                context.Response.Write(msg);
            };
            RegActionAsync(name, a);
        }

        /// <summary>
        /// 注册返回xml的操作
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="action"></param>
        protected void RegXmlActionAsync<T>(string name, Func<HttpContext, TState, T> action)
        {
            Action<HttpContext, TState> a = (context, state) =>
            {
                var result = action(context, state);
                string msg = XmlSerializeFunc(result);
                context.Response.ContentType = "text/xml";
                context.Response.Write(msg);
            };
            RegActionAsync(name, a);
        }
    }
}