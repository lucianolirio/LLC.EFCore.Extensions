using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Brazil.EFCore.Extensions
{
    internal static class DataReaderExtension
    {
        public static Type EntityFactoryByDataReader(this IDataReader dataReader, string entityName)
        {

            string nspace = "Brazil.EFCore.Extensions";
            entityName = string.IsNullOrEmpty(entityName) ? "Query" : entityName;

            // Our intermediate language generator
            ILGenerator ilgen;
            // The assembly builder           

            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(nspace), AssemblyBuilderAccess.RunAndCollect);
            // The module builder
            ModuleBuilder modBuilder = asmBuilder.DefineDynamicModule(nspace);
            // The person class builder
            TypeBuilder objectBuilder = modBuilder.DefineType("Row", TypeAttributes.Class | TypeAttributes.Public);
            // The default constructor
            ConstructorBuilder ctorBuilder = objectBuilder.DefineDefaultConstructor(MethodAttributes.Public);
            // Custom attributes for get, set accessors
            MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
            for (int i = 0; i < dataReader.FieldCount; i++)
            {
                var typeField = dataReader.GetFieldType(i);
                var name = dataReader.GetName(i);

                // Two fields: m_firstname, m_lastname
                FieldBuilder fBuilderField = objectBuilder.DefineField("_" + name, typeField, FieldAttributes.Private);
                // Two properties for this object: FirstName, LastName
                PropertyBuilder pBuilderField = objectBuilder.DefineProperty(name, System.Reflection.PropertyAttributes.HasDefault, typeField, null);
                // get,set accessors for FirstName
                MethodBuilder mGetFieldBuilder = objectBuilder.DefineMethod("get_" + name, getSetAttr, typeField, Type.EmptyTypes);
                // Code generation
                ilgen = mGetFieldBuilder.GetILGenerator();
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Ldfld, fBuilderField); // returning the firstname field
                ilgen.Emit(OpCodes.Ret);

                MethodBuilder mSetFieldBuilder = objectBuilder.DefineMethod("set_" + name, getSetAttr, null, new Type[] { typeField });

                // Code generation
                ilgen = mSetFieldBuilder.GetILGenerator();
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Ldarg_1);
                ilgen.Emit(OpCodes.Stfld, fBuilderField); // setting the firstname field from the first argument (1)
                ilgen.Emit(OpCodes.Ret);

                // Assigning get/set accessors
                pBuilderField.SetGetMethod(mGetFieldBuilder);
                pBuilderField.SetSetMethod(mSetFieldBuilder);
            }

            return objectBuilder.CreateType();
        }
    }
}
