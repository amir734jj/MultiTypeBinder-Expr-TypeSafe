using System;

namespace MultiTypeBinderExprTypeSafe.Interfaces
{
    public interface ICustomTypeGenerator
    {
        Type EmittedType { get; }
    }
}