﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Nancy.Rest.Annotations.Enums;
using Nancy.Rest.Module.Filters;
using Nancy.Rest.Module.Interfaces;

namespace Nancy.Rest.Module.Helper
{
    internal static class Extensions
    {
        internal static List<T> GetCustomAttributesFromInterfaces<T>(this MethodInfo minfo) where T: Attribute
        {
            List<T> rests=new List<T>();
            List<Type> types = new List<Type> {minfo.DeclaringType};
            types.AddRange(minfo.DeclaringType?.GetInterfaces().ToList() ?? new List<Type>());
            foreach (Type t in types)
            {
                MethodInfo m = t.GetMethod(minfo.Name, minfo.GetParameters().Select(a => a.ParameterType).ToArray());
                if (m != null)
                    rests.AddRange(m.GetCustomAttributes(typeof(T)).Cast<T>().ToList());
            }
            return rests;
            
        }
        internal static List<T> GetCustomAttributesFromInterfaces<T>(this Type minfo) where T : Attribute
        {
            List<T> rests = new List<T>();
            List<Type> types = new List<Type> { minfo };
            types.AddRange(minfo.GetInterfaces());
            foreach (Type t in types)
            {
                rests.AddRange(t.GetCustomAttributes(typeof(T)).Cast<T>().ToList());
            }
            return rests;

        }


        internal static bool IsAsyncMethod(this MethodInfo minfo)
        {
            return (minfo.GetCustomAttribute(typeof(AsyncStateMachineAttribute)) != null);
        }

        internal static bool IsNullable<T>(this T obj)
        {
            if (obj == null) return true;
            Type type = typeof(T);
            if (!type.IsValueType) return true;
            if (Nullable.GetUnderlyingType(type) != null) return true;
            return false;
        }

        internal static bool IsNullable(this Type type)
        {
            if (!type.IsValueType) return true;
            if (Nullable.GetUnderlyingType(type) != null) return true;
            return false;

        }

        internal static bool IsRouteable(this Type type)
        {
            if (type.IsValueType)
                return true;
            if (type == typeof(string) || type == typeof(Guid) || type==typeof(Guid?))
                return true;
            if (Nullable.GetUnderlyingType(type)?.IsValueType ?? false)
                return true;
            return false;
        }
        public static bool SerializerSupportFilter(this NancyModule module)
        {
            string contentType = null;
            if (module.Request.Headers.Accept.Any())
                contentType=module.Request.Headers.Accept?.ElementAt(0)?.Item1;
            if (contentType == null)
                return false;
            foreach (ISerializer serializer in module.Response.Serializers)
            {
                if (serializer.CanSerialize(contentType))
                {
                    if (serializer is IFilterSupport)
                        return true;
                    return false;
                }
            }
            return false;
        }
        public static Type GetUnderlyingType(this MemberInfo member)
        {
            switch (member.MemberType)
            {
                case MemberTypes.Event:
                    return ((EventInfo)member).EventHandlerType;
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Method:
                    return ((MethodInfo)member).ReturnType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
                case MemberTypes.TypeInfo:
                    return ((TypeInfo) member).UnderlyingSystemType;
                default:
                    throw new ArgumentException
                    (
                     "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
                    );
            }
        }

        public static Response FromIStreamWithResponse(this IResponseFormatter response, IStreamWithResponse stream, string defaultresponsecontentype)
        {
            if (!(stream is Stream))
                throw new NotSupportedException("IStreamWithResponse should be also a stream");
            Response n;
            string contenttype = stream.ContentType;
            if (contenttype == null)
                contenttype = defaultresponsecontentype;
            if (contenttype == null)
                contenttype = "application/octet-stream";
            if (stream.HasContent)
                n = response.FromStream((Stream) stream, contenttype);
            else
            {
                n = new Response();
                if (!string.IsNullOrEmpty(stream.ResponseDescription))
                    n.ReasonPhrase = stream.ResponseDescription;
                n.ContentType = contenttype;
            }
            n.StatusCode = stream.ResponseStatus;
            foreach (string head in stream.Headers.Keys)
            {
                n.Headers.Add(head, stream.Headers[head]);
            }
            if (stream.ContentLength != 0)
                n.Headers.Add("Content-Length", stream.ContentLength.ToString(CultureInfo.InvariantCulture));
            return n;
        }




        public static NancyModule.RouteBuilder GetRouteBuilderForVerb(this NancyModule module, Verbs v)
        {
            NancyModule.RouteBuilder bld=null;
            switch (v)
            {
                case Verbs.Get:
                    bld = module.Get;
                    break;
                case Verbs.Post:
                    bld = module.Post;
                    break;
                case Verbs.Put:
                    bld = module.Put;
                    break;
                case Verbs.Delete:
                    bld = module.Delete;
                    break;
                case Verbs.Options:
                    bld = module.Options;
                    break;
                case Verbs.Patch:
                    bld = module.Patch;
                    break;
                case Verbs.Head:
                    bld = module.Head;
                    break;
            }
            return bld;
        } 
    }
}
