using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using MultiTypeBinderExprTypeSafe.Interfaces;
using MultiTypeBinderExprTypeSafe.Utilities;
using static MultiTypeBinderExprTypeSafe.Utilities.BasicPropertyInfoBuildUtility;

namespace MultiTypeBinderExprTypeSafe
{
    public class MultiTypeBinder<TCommon> : IMultiTypeBinder<TCommon>
    {
        private readonly IReadOnlyDictionary<KeyValuePair<Type, IReadOnlyDictionary<string, string>>, Type>
            _augmentedTable;

        public MultiTypeBinder(IReadOnlyDictionary<Type, IReadOnlyDictionary<string, string>> table)
        {
            var cmType = typeof(TCommon);

            _augmentedTable =
                table.ToDictionary(x => x, x => new CustomTypeGenerator(x.Key, cmType, x.Value).EmittedType);
        }

        public List<TCommon> Map(IEnumerable<object> items)
        {
            return items?.Select(item =>
            {
                var mapper = _augmentedTable.FirstOrDefault(x => x.Key.Key.IsInstanceOfType(item));

                if (mapper.Equals(
                    default(KeyValuePair<KeyValuePair<Type, IReadOnlyDictionary<string, string>>, Type>))
                )
                {
                    throw new Exception($"Missing mapper for type: {item.GetType().Name}");
                }

                var proxyInstance = (TCommon) Activator.CreateInstance(mapper.Value, item);

                return proxyInstance;
            }).ToList();
        }
    }

    public class MultiTypeBinderBuilder<TCommon> : IMultiTypeBinderBuilder<TCommon>
    {
        private ImmutableDictionary<Type, IReadOnlyDictionary<string, string>> _table;

        public MultiTypeBinderBuilder()
        {
            _table = ImmutableDictionary<Type, IReadOnlyDictionary<string, string>>.Empty;
        }

        public IMultiTypeBinderBuilder<TCommon> WithType<TClass>(
            Func<IBindTypeBuilder<TCommon, TClass>, IVoid> opt)
        {
            var rslt = new BindTypeBuilder<TCommon, TClass>();

            opt(rslt);

            _table = _table.Add(typeof(TClass), rslt.Map);

            return this;
        }

        public IMultiTypeBinder<TCommon> Build()
        {
            return new MultiTypeBinder<TCommon>(_table);
        }
    }

    public class BindTypeBuilder<TCommon, TClass> : IBindTypeBuilder<TCommon, TClass>
    {
        public ImmutableDictionary<string, string> Map { get; private set; }

        public BindTypeBuilder()
        {
            Map = ImmutableDictionary.Create<string, string>();
        }

        public IBindTypeBuilder<TCommon, TClass> WithProperty<TProperty>(Expression<Func<TCommon, TProperty>> fromExpr,
            Expression<Func<TClass, TProperty>> toExpr)
        {
            var (fromExprName, toExprName) = Resolve(fromExpr, toExpr);

            Map = Map.Add(fromExprName, toExprName);

            return this;
        }

        public IVoid FinalizeType()
        {
            // Nothing
            return new Void();
        }
    }

    public class Void : IVoid
    {
    }
}