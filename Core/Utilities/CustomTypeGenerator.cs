using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using MultiTypeBinderExprTypeSafe.Interfaces;

namespace MultiTypeBinderExprTypeSafe.Utilities
{
    /// <summary>
    /// Creates a new type dynamically
    /// </summary>
    public class CustomTypeGenerator : ICustomTypeGenerator
    {
        private readonly TypeBuilder _tb;

        private readonly FieldBuilder _entityFieldBldr;

        private readonly Type _srcType;

        private readonly Type _cmType;

        /// <summary>
        /// Initialize custom type builder
        /// </summary>
        /// <param name="srcType"></param>
        /// <param name="cmType"></param>
        /// <param name="members"></param>
        /// <exception cref="Exception"></exception>
        public CustomTypeGenerator(Type srcType, Type cmType, IReadOnlyDictionary<string, string> members)
        {
            _cmType = cmType;
            _srcType = srcType;

            var objType = typeof(object);

            if (!_cmType.IsInterface)
            {
                throw new Exception("Type has to be an interface");
            }

            var assemblyName = Path.GetRandomFileName();
            var typeSignature = Path.GetRandomFileName();

            var assemblyBuilder =
                AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run);

            var moduleBuilder = assemblyBuilder.DefineDynamicModule(Path.GetRandomFileName());

            _tb = moduleBuilder.DefineType(typeSignature,
                TypeAttributes.Public |
                TypeAttributes.Serializable |
                TypeAttributes.Class |
                TypeAttributes.Sealed |
                TypeAttributes.AutoLayout, objType);

            _tb.AddInterfaceImplementation(_cmType);

            _entityFieldBldr = EmitSourceField();

            _tb.DefineDefaultConstructor(
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName);

            var constructorBuilder = _tb.DefineConstructor(MethodAttributes.Public,
                CallingConventions.Standard,
                new[] {_srcType});

            constructorBuilder.DefineParameter(0, ParameterAttributes.None, "entity");

            var constructorIl = constructorBuilder.GetILGenerator();
            constructorIl.Emit(OpCodes.Ldarg_0);
            constructorIl.Emit(OpCodes.Ldarg_1);
            constructorIl.Emit(OpCodes.Stfld, _entityFieldBldr);
            constructorIl.Emit(OpCodes.Ret);

            foreach (var (commonPrpName, sourcePrpName) in members)
            {
                EmitProperty(commonPrpName, sourcePrpName);
            }

            foreach (var notImplementedProp in cmType.GetProperties().Where(x => !members.ContainsKey(x.Name)))
            {
                EmitNotImplemented(notImplementedProp);
            }

            EmittedType = _tb.CreateType();
        }

        public Type EmittedType { get; }

        private FieldBuilder EmitSourceField()
        {
            var entityBldr = _tb.DefineField("_" + "entity", _srcType,
                FieldAttributes.Private |
                FieldAttributes.InitOnly);

            return entityBldr;
        }

        private void EmitNotImplemented(PropertyInfo cmProp)
        {
            var (cmPropGetter, cmPropSetter) = (cmProp.GetGetMethod(), cmProp.GetSetMethod());

            var propertyBldr =
                _tb.DefineProperty(cmProp.Name, PropertyAttributes.None, cmProp.PropertyType, Type.EmptyTypes);

            if (cmPropGetter != null)
            {
                var getPropMthdBldr = _tb.DefineMethod($"get_{cmProp.Name}",
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    cmProp.PropertyType, Type.EmptyTypes);

                var getIl = getPropMthdBldr.GetILGenerator();
                var getPropertyLbl = getIl.DefineLabel();
                var exitGetLbl = getIl.DefineLabel();

                getIl.MarkLabel(getPropertyLbl);
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.ThrowException(typeof(NotImplementedException));
                getIl.MarkLabel(exitGetLbl);
                getIl.Emit(OpCodes.Ret);

                _tb.DefineMethodOverride(getPropMthdBldr, cmPropGetter);

                propertyBldr.SetGetMethod(getPropMthdBldr);
            }

            if (cmPropSetter != null)
            {
                var setPropMthdBldr = _tb.DefineMethod($"set_{cmProp.Name}",
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    null, new[] {cmProp.PropertyType});

                var setIl = setPropMthdBldr.GetILGenerator();
                var modifyPropertyLbl = setIl.DefineLabel();
                var exitSetLbl = setIl.DefineLabel();

                setIl.MarkLabel(modifyPropertyLbl);
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.ThrowException(typeof(NotImplementedException));
                setIl.MarkLabel(exitSetLbl);
                setIl.Emit(OpCodes.Ret);

                _tb.DefineMethodOverride(setPropMthdBldr, cmPropSetter);

                propertyBldr.SetSetMethod(setPropMthdBldr);
            }
        }

        private void EmitProperty(string cPn, string sPn)
        {
            var srcProp = _srcType.GetProperty(sPn, BindingFlags.Public | BindingFlags.Instance);
            var cmProp = _cmType.GetProperty(cPn, BindingFlags.Public | BindingFlags.Instance);
            var cmPt = cmProp.PropertyType;

            if (cmPt != srcProp.PropertyType || cmPt != srcProp.PropertyType)
            {
                throw new Exception("Property types does not match");
            }

            var propertyBldr = _tb.DefineProperty(cPn, PropertyAttributes.None, cmPt, Type.EmptyTypes);

            var overrideGetterPropMthdInfo = cmProp.GetGetMethod();
            var getterMethodInfo = srcProp.GetGetMethod();

            if (overrideGetterPropMthdInfo != null && getterMethodInfo != null)
            {
                var getPropMthdBldr = _tb.DefineMethod($"get_{cPn}",
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    cmPt, Type.EmptyTypes);

                var getIl = getPropMthdBldr.GetILGenerator();
                var getPropertyLbl = getIl.DefineLabel();
                var exitGetLbl = getIl.DefineLabel();

                getIl.MarkLabel(getPropertyLbl);
                getIl.Emit(OpCodes.Ldarg_0);
                getIl.Emit(OpCodes.Ldfld, _entityFieldBldr);
                getIl.Emit(OpCodes.Callvirt, getterMethodInfo);
                getIl.MarkLabel(exitGetLbl);
                getIl.Emit(OpCodes.Ret);

                _tb.DefineMethodOverride(getPropMthdBldr, overrideGetterPropMthdInfo);

                propertyBldr.SetGetMethod(getPropMthdBldr);
            }
            else if (overrideGetterPropMthdInfo == null && getterMethodInfo == null)
            {
                // Do not generate getter method
            }
            else
            {
                throw new Exception("Missing getter");
            }

            var overrideSetterPropMthdInfo = cmProp.GetSetMethod();
            var setterMethodInfo = srcProp.GetSetMethod();

            if (overrideSetterPropMthdInfo != null && setterMethodInfo != null)
            {
                var setPropMthdBldr = _tb.DefineMethod($"set_{cPn}",
                    MethodAttributes.Public |
                    MethodAttributes.Virtual |
                    MethodAttributes.SpecialName |
                    MethodAttributes.HideBySig,
                    null, new[] {cmPt});

                var setIl = setPropMthdBldr.GetILGenerator();
                var modifyPropertyLbl = setIl.DefineLabel();
                var exitSetLbl = setIl.DefineLabel();

                setIl.MarkLabel(modifyPropertyLbl);
                setIl.Emit(OpCodes.Ldarg_0);
                setIl.Emit(OpCodes.Ldfld, _entityFieldBldr);
                setIl.Emit(OpCodes.Ldarg_1);
                setIl.Emit(OpCodes.Callvirt, setterMethodInfo);
                setIl.MarkLabel(exitSetLbl);
                setIl.Emit(OpCodes.Ret);

                _tb.DefineMethodOverride(setPropMthdBldr, overrideSetterPropMthdInfo);

                propertyBldr.SetSetMethod(setPropMthdBldr);
            }
            else if (overrideSetterPropMthdInfo == null && setterMethodInfo == null)
            {
                // Do not generate setter method
            }
            else
            {
                throw new Exception("Missing setter");
            }
        }
    }
}