using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace SandyBox.CSharp.HostingServer
{
    internal static class ModuleFunctionBinder
    {

        public static MethodInfo BindMethod(IEnumerable<MethodInfo> candidates, Type ownerType, string methodName, IList<JToken> positionalParameters,
            IDictionary<string, JToken> namedParameters)
        {
            Debug.Assert(ownerType != null);
            Debug.Assert(methodName != null);

            bool MatchMethod(MethodInfo m)
            {
                var expectedParams = m.GetParameters();
                int namedArgsBeginsAt = 0;
                if (positionalParameters != null && positionalParameters.Count > 0)
                {
                    for (int i = 0; i < positionalParameters.Count; i++)
                    {
                        if (expectedParams.Length <= i) return false;
                        var ep = expectedParams[i];
                        var p = positionalParameters[i];
                        if (!MatchJTokenType(ep.ParameterType, p.Type)) return false;
                        // TODO varargs
                    }
                    namedArgsBeginsAt = positionalParameters.Count;
                }
                if (namedParameters != null && namedParameters.Count > 0)
                {
                    int namedParamsCount = namedParameters.Count;
                    for (int i = namedArgsBeginsAt; i < expectedParams.Length; i++)
                    {
                        var ep = expectedParams[i];
                        if (namedParameters.TryGetValue(ep.Name, out var p))
                        {
                            if (!MatchJTokenType(ep.ParameterType, p.Type)) return false;
                            namedParamsCount--;
                        }
                        if (namedParamsCount == 0) break;
                    }
                    if (namedParamsCount > 0) return false;
                }
                for (int i = namedArgsBeginsAt; i < expectedParams.Length; i++)
                {
                    if (!expectedParams[i].IsOptional)
                    {
                        if (namedParameters == null || !namedParameters.ContainsKey(expectedParams[i].Name))
                            return false;
                    }
                }
                return true;
            }

            var matches = candidates.Where(MatchMethod).ToList();
            if (matches.Count == 0) throw new MissingMethodException(ownerType.Name, methodName);
            if (matches.Count > 1)
                throw new AmbiguousMatchException("Multiple methods match the specified argument list: \n" + string.Join("\n", matches));
            return matches[0];
        }

        private static readonly object[] emptyObjects = new object[0];

        public static object[] BindParameters(MethodInfo method, IList<JToken> positionalParameters,
            IDictionary<string, JToken> namedParameters)
        {
            var expectedParams = method.GetParameters();
            if (expectedParams.Length == 0) return emptyObjects;
            var parameters = new object[expectedParams.Length];
            var positionalCount = positionalParameters?.Count ?? 0;
            for (int i = 0; i < expectedParams.Length; i++)
            {
                var ep = expectedParams[i];
                JToken jparam;
                if (i < positionalCount)
                    jparam = positionalParameters[i];
                else if (namedParameters != null)
                    namedParameters.TryGetValue(ep.Name, out jparam);
                else
                    jparam = null;
                if (jparam == null)
                {
                    if (ep.IsOptional)
                    {
                        parameters[i] = Type.Missing;
                        continue;
                    }
                    throw new ArgumentException("Arguments mismatch.");
                }
                parameters[i] = jparam.ToObject(ep.ParameterType);
            }
            return parameters;
        }

        public static JToken SerializeReturnValue(object returnValue)
        {
            if (returnValue == null) return JValue.CreateNull();
            if (returnValue is JToken jt) return jt;
            return JToken.FromObject(returnValue);
        }

        private static bool MatchJTokenType(Type parameterType, JTokenType type)
        {
            if (parameterType == typeof(JToken)) return true;
            var ti = parameterType.GetTypeInfo();
            switch (type)
            {
                case JTokenType.Object:
                    return !(ti.IsPrimitive || parameterType == typeof(string));
                case JTokenType.Array:
                    return typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(ti);
                case JTokenType.Boolean:
                    return parameterType == typeof(bool) || parameterType == typeof(bool?);
                case JTokenType.Integer:
                    if (ti.IsEnum) return true;
                    goto case JTokenType.Float;
                case JTokenType.Float:
                    return parameterType == typeof(byte) || parameterType == typeof(byte?)
                           || parameterType == typeof(short) || parameterType == typeof(short?)
                           || parameterType == typeof(int) || parameterType == typeof(int?)
                           || parameterType == typeof(long) || parameterType == typeof(long?)
                           || parameterType == typeof(sbyte) || parameterType == typeof(sbyte?)
                           || parameterType == typeof(ushort) || parameterType == typeof(ushort?)
                           || parameterType == typeof(uint) || parameterType == typeof(uint?)
                           || parameterType == typeof(ulong) || parameterType == typeof(ulong?)
                           || parameterType == typeof(float) || parameterType == typeof(float?)
                           || parameterType == typeof(double) || parameterType == typeof(double?);
                case JTokenType.Date:
                case JTokenType.TimeSpan:
                case JTokenType.Uri:
                case JTokenType.Guid:
                case JTokenType.String:
                    // They are all JSON string
                    return !ti.IsPrimitive
                           || parameterType == typeof(char)
                           || parameterType == typeof(char?);
                case JTokenType.Null:
                    return !parameterType.GetTypeInfo().IsValueType
                           || parameterType.IsConstructedGenericType &&
                           parameterType.GetGenericTypeDefinition() == typeof(Nullable<>);
                default:
                    return false;
            }
        }

    }
}
