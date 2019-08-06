using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MultiTypeBinderExpr.Models;

namespace MultiTypeBinderExpr.Utilities
{
    /// <summary>
    /// Utility to create an assignment 
    /// </summary>
    public static class BasicPropertyInfoBuildUtility
    {
        /// <summary>
        /// Generate assignment function from the provided member
        /// </summary>
        /// <param name="expr"></param>
        /// <returns></returns>
        public static BasicPropertyInfoBuild Resolve<TClass, TProperty>(Expression<Func<TClass, TProperty>> expr)
        {
            var memberInfo = new MemberExpressionVisitor(expr).ResolveMemberInfo();
            var source = Expression.Parameter(typeof(TClass));
            var property = Expression.Parameter(typeof(TProperty));

            var propertyInfo = (PropertyInfo) memberInfo;
            var memberAccessExpr = property;
            var setterMethodInfo = propertyInfo.GetSetMethod();
            var getterMethodInfo = propertyInfo.GetGetMethod();

            if (setterMethodInfo == null)
            {
                throw new Exception($"Setter for member: {memberInfo.Name} does not exist");
            }

            if (getterMethodInfo == null)
            {
                throw new Exception($"Getter for member: {memberInfo.Name} does not exist");
            }

            var (getterExpr, setterExpr) = (
                Expression.Call(source, getterMethodInfo),
                Expression.Call(source, setterMethodInfo, memberAccessExpr)
            );

            var (getterFunc, setterFunc) = (
                Expression.Lambda<Func<TClass, TProperty>>(getterExpr, source).Compile(),
                Expression.Lambda<Action<TClass, TProperty>>(setterExpr, source, property).Compile()
            );

            return new BasicPropertyInfoBuild
            {
                GetValue = x => getterFunc((TClass) x),
                SetValue = (x, y) => setterFunc((TClass) x, (TProperty) y)
            };
        }
    }

    /// <inheritdoc />
    /// <summary>
    /// Expression visitor to resolve MemberInfo from an Expression
    /// </summary>
    public sealed class MemberExpressionVisitor : ExpressionVisitor
    {
        private ImmutableList<MemberInfo> _members;

        private readonly Expression _expr;

        /// <inheritdoc />
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="expr"></param>
        public MemberExpressionVisitor(Expression expr)
        {
            _expr = expr;
            _members = ImmutableList.Create<MemberInfo>();

            Visit(expr);
        }

        /// <summary>
        /// Return the MemberInfo, throw an exception if count of MemberInfos is not equal to one
        /// </summary>
        /// <returns></returns>
        public MemberInfo ResolveMemberInfo()
        {
            return _members.Count == 1
                ? _members.First()
                : throw new Exception($"Expression: `{_expr}` is not a valid member expression");
        }

        /// <inheritdoc />
        /// <summary>
        /// Add the MemberInfo to the list (there should be a single MemberInfo at the end)
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            _members = _members.Add(node.Member);

            return base.VisitMember(node);
        }
    }
}