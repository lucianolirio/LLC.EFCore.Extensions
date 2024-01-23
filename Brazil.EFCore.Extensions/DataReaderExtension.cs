using System.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace LLC.EFCore.Extensions;

internal static class DataReaderExtension
{
    private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
    private const string Namespace = "LLC.EFCore.Extensions";

    public static Type EntityFactoryByDataReader2(this IDataReader dataReader, string entityName)
    {
        entityName = string.IsNullOrEmpty(entityName) ? "Query" : entityName;

        // Use caching to avoid regenerating the same type
        if (_typeCache.TryGetValue(entityName, out var cachedType))
        {
            return cachedType;
        }

        AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(Namespace), AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(Namespace);
        TypeBuilder typeBuilder = moduleBuilder.DefineType(entityName, TypeAttributes.Class | TypeAttributes.Public);

        MethodAttributes getSetAttr = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName;
        ILGenerator ilGenerator;

        for (int i = 0; i < dataReader.FieldCount; i++)
        {
            Type fieldType = dataReader.GetFieldType(i);
            string fieldName = dataReader.GetName(i);
            FieldBuilder fieldBuilder = typeBuilder.DefineField("_" + fieldName, fieldType, FieldAttributes.Private);

            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(fieldName, PropertyAttributes.HasDefault, fieldType, null);

            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod("get_" + fieldName, getSetAttr, fieldType, Type.EmptyTypes);
            ilGenerator = getMethodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod("set_" + fieldName, getSetAttr, null, new Type[] { fieldType });
            ilGenerator = setMethodBuilder.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Ldarg_1);
            ilGenerator.Emit(OpCodes.Stfld, fieldBuilder);
            ilGenerator.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }

        Type createdType = typeBuilder.CreateType();
        _typeCache[entityName] = createdType; // Cache the created type

        return createdType;
    }

    public static Type EntityFactoryByDataReader(this IDataReader dataReader, string entityName)
    {
        string nspace = "LLC.EFCore.Extensions";
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
            PropertyBuilder pBuilderField = objectBuilder.DefineProperty(name, PropertyAttributes.HasDefault, typeField, null);
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
