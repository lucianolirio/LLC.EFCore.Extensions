using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Reflection;

namespace LLC.EFCore.Extensions;

public static class SelectExtension
{
    private static Dictionary<string, string> GetFieldNames(this IDataReader reader)
    {
        var result = new Dictionary<string, string>();
        for (int i = 0; i < reader.FieldCount; i++)
        {
            result.Add(reader.GetName(i), reader.GetName(i));
        }
        return result;
    }

    private static List<object> Select(this DbContext context, Type type, string sql, params object[] parameters)
    {
        var result = new List<object>();
        var connection = context.Database.GetDbConnection();
        connection.Open();
        using (var command = context.Database.GetDbConnection().CreateCommand())
        {
            command.CommandTimeout = connection.ConnectionTimeout;
            command.CommandText = sql;

            foreach (var param in parameters) { command.Parameters.Add(param); }

            using (var dataReader = command.ExecuteReader(CommandBehavior.CloseConnection))
            {
                if (!dataReader.HasRows)
                    return result;

                Type entityType = type != null ? type : dataReader.EntityFactoryByDataReader("select");
                var properties = entityType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var fields = dataReader.GetFieldNames();

                while (dataReader.Read())
                {
                    var entity = Activator.CreateInstance(entityType);
                    foreach (var prop in properties)
                    {
                        if (!fields.ContainsKey(prop.Name))
                            continue;

                        if (dataReader[prop.Name].Equals(DBNull.Value))
                            continue;

                        prop.SetValue(entity, dataReader[prop.Name], null);
                    }
                    result.Add(entity);
                }
            }
        }
        return result;
    }

    public static List<T> Select<T>(this DbContext context, string sql, params object[] parameters) where T : class, new()
    {
        return context.Select(typeof(T), sql, parameters).Cast<T>().ToList();
    }

    public static List<object> Select(this DbContext context, string sql, params object[] parameters)
    {
        return context.Select(null, sql, parameters);
    }
}
