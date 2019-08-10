using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq.Expressions;

namespace MultiTypeBinderExprTypeSafe.Interfaces
{
    public interface IMultiTypeBinder<TCommon>
    {
        List<TCommon> Map(IEnumerable<object> items);
    }
    
    public interface  IMultiTypeBinderBuilder<TCommon>
    {
        IMultiTypeBinderBuilder<TCommon> WithType<TClass>(Func<IBindTypeBuilder<TCommon, TClass>, IVoid> opt);

        IMultiTypeBinder<TCommon> Build();
    }

    public interface IBindTypeBuilder<TCommon, TClass>
    {
        ImmutableDictionary<string, string> Map { get; }

        IBindTypeBuilder<TCommon, TClass>
            WithProperty<TProperty>(Expression<Func<TCommon, TProperty>> fromExpr, Expression<Func<TClass, TProperty>> toExpr);

        IVoid FinalizeType();
    }
}