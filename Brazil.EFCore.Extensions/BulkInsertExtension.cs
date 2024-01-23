using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Brazil.EFCore.Extensions
{
    public static class BulkInsertExtension
    {
        private static readonly object locker = new object();

        internal static IEntityType GetEntityType(this DbContext context, Type type) => context.Model.FindEntityType(type);

        internal static string GetSchema(this DbContext context, Type type) => context.Model.FindEntityType(type)?.GetSchema() ?? "dbo";

        internal static string GetTableName(this DbContext context, Type type) => context.Model.FindEntityType(type).GetTableName();

        internal static string GetFullTableName(this DbContext context, Type type) => $"{context.GetSchema(type)}.{context.GetTableName(type)}";

        internal static IReadOnlyList<IProperty> GetPrimaryKeys(this DbContext context, Type type) => context.GetEntityType(type).FindPrimaryKey().Properties;

        internal static Dictionary<string, IProperty> GetPropertyAnnotations2(this DbContext context, Type type)
        {
            var result = new Dictionary<string, IProperty>();

            var entityType = context.Model.FindEntityType(type);
            if (entityType != null)
            {
                var properties = entityType.GetProperties();
                foreach (var property in properties)
                {
                    result.Add(property.Name, property);
                }
            }

            return result;
        }


        internal static List<IProperty> GetProperties(this DbContext context, Type type)
        {
            var result = new List<IProperty>();

            var entityType = context.Model.FindRuntimeEntityType(type);
            foreach (var property in entityType.GetProperties())
            {
                if (property.IsPrimaryKey() && property.ValueGenerated == ValueGenerated.OnAdd)
                    continue;
                result.Add(property);
            }

            return result;
        }

        internal static object GetDiscriminatorValue(this DbContext context, Type type)
        {
            return context.Model.FindRuntimeEntityType(type)?
                .GetAnnotation("Relational:DiscriminatorValue")?.Value;
        }

        internal static List<IProperty> GetPropertyInfo(this DbContext context, Type type)
        {
            var list = context.GetProperties(type);
            var result = new List<IProperty>();

            foreach (var item in list)
            {
                result.Add(item);
            }

            return result;
        }

        internal static DataTable CreateTable(this DbContext context, Type type)
        {
            var result = new DataTable(context.GetFullTableName(type));

            foreach (var property in context.GetProperties(type))
            {
                if (property.IsPrimaryKey() && property.ValueGenerated == ValueGenerated.OnAdd)
                    continue;

                result.Columns.Add(property.Name);
            }

            //context.GetProperties(type)
            //    .ForEach(e => result.Columns.Add(
            //        e.Name,
            //        Nullable.GetUnderlyingType(e.PropertyInfo.PropertyType) ?? e.PropertyInfo.PropertyType)
            //    );
            return result;
        }

        internal static DataTable GetDataTable<T>(this DbContext context, List<T> list)
        {
            var type = list.First().GetType();
            var entityType = context.Model.FindEntityType(type);
            var discriminatorProperty = entityType.FindDiscriminatorProperty();
            var entityTypeName = entityType.GetDiscriminatorPropertyName();
            var entityTypeValue = entityType.GetDefaultDiscriminatorValue();

            if (entityType == null)
                throw new InvalidOperationException("O tipo especificado não é uma entidade conhecida no contexto.");

            var dataTable = new DataTable();
            foreach (var property in entityType.GetProperties())
            {
                dataTable.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType);
            }

            foreach (var entity in list)
            {
                var row = dataTable.NewRow();
                foreach (var property in entityType.GetProperties())
                {

                    try
                    {
                        if (property.ValueGenerated != ValueGenerated.OnAdd)
                        {
                            if (discriminatorProperty.Equals(property))
                            {
                                row[property.Name] = entityTypeValue;
                            }
                            else
                            {
                                var value = property.PropertyInfo.GetValue(entity);
                                var defaultValue = property.FindAnnotation("Relational:DefaultValue");
                                row[property.Name] = value ?? defaultValue?.Value ?? DBNull.Value;
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        internal static SqlBulkCopy GetSqlBulkCopy(this DbContext context, IDbContextTransaction transaction)
        {
            var connecteion = (SqlConnection)context.Database.GetDbConnection();

            var result = new SqlBulkCopy(
                connecteion,
                SqlBulkCopyOptions.Default,
                (SqlTransaction)transaction.GetDbTransaction());

            result.BulkCopyTimeout = connecteion.ConnectionTimeout;

            return result;
        }

        private static void SetIdentityValue<T>(this DbContext context, List<T> list, IDbContextTransaction transaction)
        {
            var pk = context.GetPrimaryKeys(typeof(T)).FirstOrDefault();

            if (pk == null || pk.ValueGenerated != ValueGenerated.OnAdd)
                return;

            using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.Transaction = (SqlTransaction)transaction.GetDbTransaction();
                command.CommandText = String.Format("Select top {0} {1} from {2} order by 1 desc", list.Count, pk.Name, context.GetFullTableName(typeof(T)));

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.HasRows)
                        return;

                    var key = typeof(T).GetProperty(pk.Name);

                    int i = list.Count;
                    while (reader.Read())
                    {
                        key.SetValue(list[--i], reader[pk.Name]);
                    }
                }
            }
        }

        private static string GetTableNamex(this DbContext context, Type entityType)
        {
            var model = context.Model;
            var entityTypeInfo = model.FindEntityType(entityType);

            if (entityTypeInfo == null)
            {
                throw new InvalidOperationException($"O tipo {entityType.Name} não é uma entidade conhecida no contexto.");
            }

            return entityTypeInfo.GetTableName();
        }


        private static void BulkInsertInternal<T>(this DbContext context, List<T> list)
        {
            using (var transaction = context.Database.BeginTransaction())
            {
                var grupos = (from i in list
                              group i by i.GetType() into g
                              select new { Tipo = g.Key, Itens = g.ToList() }).ToList();

                foreach (var item in grupos)
                {
                    using (var bulkCopy = context.GetSqlBulkCopy(transaction))
                    {
                        var dataTable = context.GetDataTable(item.Itens);

                        if (dataTable.Columns.Count == 0)
                            return;

                        bulkCopy.DestinationTableName = GetTableNamex(context, list.First().GetType());

                        foreach (DataColumn column in dataTable.Columns)
                        {
                            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                        }

                        bulkCopy.WriteToServer(dataTable);
                        dataTable.Rows.Clear();

                        context.SetIdentityValue(list, transaction);

                        context.Database.CommitTransaction();
                    }
                }
            }
        }

        public static void BulkInsert<T>(this DbContext context, List<T> list)
        {
            lock (locker) { context.BulkInsertInternal(list); }
        }

        public static void BulkInsert<T>(this DbContext context, List<T> list, int size = int.MaxValue)
        {
            lock (locker)
            {
                var group = list
                    .Select((x, i) => new { Index = i, Value = x })
                    .GroupBy(x => x.Index / size).Select(x => x.Select(v => v.Value).ToList())
                    .ToList();

                foreach (var item in group)
                {
                    context.BulkInsertInternal(item);
                }
            }
        }
    }
}