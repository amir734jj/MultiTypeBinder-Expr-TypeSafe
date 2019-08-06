using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using MultiTypeBinderExpr.Interfaces;
using MultiTypeBinderExpr.Models;
using MultiTypeBinderExpr.Utilities;

namespace MultiTypeBinderExpr
{
    public class MultiTypeItem<TEnum> : IMultiTypeItem<TEnum> where TEnum : Enum
    {
        private readonly Dictionary<TEnum, BasicPropertyInfoUse> _table;

        public MultiTypeItem(Dictionary<TEnum, BasicPropertyInfoUse> table)
        {
            _table = table;
        }

        public object this[TEnum key]
        {
            get => _table[key].GetValue();
            set => _table[key].SetValue(value);
        }
    }

    public class MultiTypeBinder<TEnum> : IMultiTypeBinder<TEnum> where TEnum : Enum
    {
        private readonly Dictionary<Type, Dictionary<TEnum, BasicPropertyInfoBuild>> _basicTypeInfos;

        public MultiTypeBinder(Dictionary<Type, Dictionary<TEnum, BasicPropertyInfoBuild>> basicTypeInfos)
        {
            _basicTypeInfos = basicTypeInfos;
        }

        public List<MultiTypeItem<TEnum>> Map(IEnumerable<object> items)
        {
            return items?.Select(x =>
            {
                if (x == null)
                {
                    throw new NullReferenceException("Object is null");
                }
                
                var key = _basicTypeInfos.Keys.FirstOrDefault(y => y.IsInstanceOfType(x)) ?? throw new Exception($"There is no binder registered for type of {x.GetType().Name}");

                var value = _basicTypeInfos[key].ToDictionary(z => z.Key, z => new BasicPropertyInfoUse
                {
                    GetValue = () => z.Value.GetValue(x),
                    SetValue = a => z.Value.SetValue(x, a)
                });
                
                return new MultiTypeItem<TEnum>(value);
            }).ToList();
        }
    }

    public class MultiTypeBinderBuilder<TEnum> : IMultiTypeBinderBuilder<TEnum> where TEnum : Enum
    {
        public readonly Dictionary<Type, Dictionary<TEnum, BasicPropertyInfoBuild>> BasicTypeInfos;

        public MultiTypeBinderBuilder()
        {
            BasicTypeInfos = new Dictionary<Type, Dictionary<TEnum, BasicPropertyInfoBuild>>();
        }
        
        public IMultiTypeBinderBuilder<TEnum> WithType<TClass>(
            Func<IBindTypeBuilder<TEnum, TClass>, IMultiTypeBinderBuilder<TEnum>> opt)
        {
            return opt(new BindTypeBuilder<TEnum, TClass>(this));
        }

        public IMultiTypeBinder<TEnum> Build()
        {
            return new MultiTypeBinder<TEnum>(BasicTypeInfos);
        }
    }

    public class BindTypeBuilder<TEnum, TClass> : IBindTypeBuilder<TEnum, TClass> where TEnum : Enum
    {
        private readonly Dictionary<TEnum, BasicPropertyInfoBuild> BasicPropertyInfos;

        private readonly MultiTypeBinderBuilder<TEnum> _multiTypeBinderBuilder;

        public BindTypeBuilder(MultiTypeBinderBuilder<TEnum> multiTypeBinderBuilder)
        {
            _multiTypeBinderBuilder = multiTypeBinderBuilder;
            BasicPropertyInfos = new Dictionary<TEnum, BasicPropertyInfoBuild>();
        }

        public IBindTypeBuilder<TEnum, TClass> WithProperty<TProperty>(Expression<Func<TClass, TProperty>> property, TEnum key)
        {
            BasicPropertyInfos[key] = BasicPropertyInfoBuildUtility.Resolve(property);
            
            return this;
        }

        public IMultiTypeBinderBuilder<TEnum> FinalizeType()
        {
            foreach (var key in Enum.GetValues(typeof(TEnum)).Cast<TEnum>())
            {
                if (!BasicPropertyInfos.ContainsKey(key))
                {
                    BasicPropertyInfos[key] = InvalidPropertyInfo(key);
                }
            }
            
            _multiTypeBinderBuilder.BasicTypeInfos[typeof(TClass)] = BasicPropertyInfos;

            return _multiTypeBinderBuilder;
        }

        private static BasicPropertyInfoBuild InvalidPropertyInfo(TEnum key)
        {
            return new BasicPropertyInfoBuild
            {
                GetValue = _ => throw new Exception($"Getter for {key} is not defined"),
                SetValue = (x, _) => throw new Exception($"Setter for {key} is not defined")
            };
        }
    }
}