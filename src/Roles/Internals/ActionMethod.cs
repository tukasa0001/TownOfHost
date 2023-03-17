#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles.Internals;


public static class MethodInfoExtension
{
    public static object? InvokeAligned(this MethodInfo info, object obj, params object[] parameters)
    {
        return info.Invoke(obj, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, AlignFunctionParameters(info, parameters), null);
    }


    private static object[] AlignFunctionParameters(MethodInfo method, IEnumerable<object?> allParameters)
    {
        List<object?> allParametersList = allParameters.ToList();
        List<object> functionSpecificParameters = new();

        int i = 1;
        foreach (ParameterInfo parameter in method.GetParameters())
        {
            int matchingParamIndex = allParametersList.FindIndex(obj => obj != null && obj.GetType().IsAssignableTo(parameter.ParameterType));
            if (matchingParamIndex == -1 && !parameter.IsOptional)
                throw new ArgumentException($"Invocation of {method.Name} does not contain all required arguments. Argument {i} ({parameter.Name}) was not supplied.");
            functionSpecificParameters.Add(allParametersList.Pop(matchingParamIndex)!);
            i++;
        }

        return functionSpecificParameters.ToArray();
    }
}
