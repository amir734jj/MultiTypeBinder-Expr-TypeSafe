using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MultiTypeBinderExprTypeSafe.Utilities
{
    /// <summary>
    /// Utility to create an assignment 
    /// </summary>
    public static class BasicPropertyInfoBuildUtility
    {
        /// <summary>
        /// Generate assignment function from the provided member
        /// </summary>
        /// <param name="fromExpr"></param>
        /// <param name="toExpr"></param>
        /// <typeparam name="TCommon"></typeparam>
        /// <typeparam name="TClass"></typeparam>
        /// <typeparam name="TProperty"></typeparam>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static KeyValuePair<string, string> Resolve<TCommon, TClass, TProperty>(
            Expression<Func<TCommon, TProperty>> fromExpr, Expression<Func<TClass, TProperty>> toExpr)
        {
            return new KeyValuePair<string, string>(
                new MemberExpressionVisitor(fromExpr).ResolveMemberInfo().Name,
                new MemberExpressionVisitor(toExpr).ResolveMemberInfo().Name
            );
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