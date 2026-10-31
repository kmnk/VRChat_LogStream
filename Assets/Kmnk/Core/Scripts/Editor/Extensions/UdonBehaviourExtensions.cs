using System;
using VRC.Udon;
using VRC.Udon.Common;
using VRC.Udon.Common.Interfaces;

namespace Kmnk.Core
{
    internal static class UdonBehaviourExtensions
    {
        internal static T GetPublicVariable<T>(this UdonBehaviour self, string symbolName)
        {
            if (!self.publicVariables.TryGetVariableValue(symbolName, out var variableValue))
            {
                throw new Exception($"any error occurred on get {symbolName} variable");
            }
            return (T)variableValue;
        }

        internal static void SetPublicVariable<T>(this UdonBehaviour self, string symbolName, T value)
        {
            IUdonVariable CreateUdonVariable(string s, object v)
            {
                var t = typeof(UdonVariable<>).MakeGenericType(typeof(T));
                return (IUdonVariable)Activator.CreateInstance(t, s, v);
            }

            self.publicVariables.RemoveVariable(symbolName);
            if (!self.publicVariables.TryAddVariable(CreateUdonVariable(symbolName, value)))
            {
                throw new Exception($"any error occurred on set value to {symbolName} variable");
            }
        }
    }
}