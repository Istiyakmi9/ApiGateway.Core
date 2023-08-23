using MySql.Data.MySqlClient;
using System.Data;
using System.Reflection;

namespace BottomhalfCore.Services.Code
{
    public static class Converter
    {
        public static decimal TwoDecimalValue(decimal num)
        {
            string strNum = num.ToString();
            if (strNum.IndexOf(".") == -1)
                return num;
            else
                return (decimal)Math.Floor(num * 100) / 100;
        }

        public static List<T> ToList<T>(this DataTable table) where T : new()
        {
            IList<PropertyInfo> availableProperties = typeof(T).GetProperties().ToList();
            IList<PropertyInfo> properties = new List<PropertyInfo>();
            List<T> result = new List<T>();

            DataColumnCollection columns = table.Columns;
            string name = null;
            int index = 0;
            while (index < availableProperties.Count)
            {
                name = availableProperties.ElementAt(index).Name;
                if (columns.Contains(name))
                    properties.Add(availableProperties[index]);
                index++;
            }

            foreach (var row in table.Rows)
            {
                var item = CreateItemFromRow<T>((DataRow)row, properties);
                result.Add(item);
            }

            return result;
        }

        public static T ToType<T>(this DataTable table) where T : new()
        {
            IList<PropertyInfo> availableProperties = typeof(T).GetProperties().ToList();
            IList<PropertyInfo> properties = new List<PropertyInfo>();
            T result = new T();

            DataColumnCollection columns = table.Columns;
            string name = null;
            int index = 0;
            while (index < availableProperties.Count)
            {
                name = availableProperties.ElementAt(index).Name;
                if (columns.Contains(name))
                    properties.Add(availableProperties[index]);
                index++;
            }

            if (table.Rows.Count > 0)
            {
                var row = table.Rows[0];
                result = CreateItemFromRow<T>((DataRow)row, properties);
            }
            return result;
        }

        private static T CreateItemFromRow<T>(DataRow row, IList<PropertyInfo> properties) where T : new()
        {
            T item = new T();
            foreach (var property in properties)
            {
                try
                {
                    if (property.PropertyType == typeof(System.DayOfWeek))
                    {
                        DayOfWeek day = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), row[property.Name].ToString());
                        property.SetValue(item, day, null);
                    }
                    else
                    {
                        if (row[property.Name] == DBNull.Value)
                        {
                            property.SetValue(item, null, null);
                        }
                        else
                        {
                            if (property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?))
                            {

                                if (row[property.Name].ToString() == "1")
                                    property.SetValue(item, true, null);
                                else
                                    property.SetValue(item, false, null);
                            }
                            else if (property.PropertyType == typeof(DateTime) || property.PropertyType == typeof(DateTime?))
                                property.SetValue(item, Convert.ToDateTime(row[property.Name].ToString()), null);
                            else if (property.PropertyType == typeof(int) || property.PropertyType == typeof(int?))
                                property.SetValue(item, Convert.ToInt32(row[property.Name].ToString()), null);
                            else if (property.PropertyType == typeof(decimal) || property.PropertyType == typeof(decimal?))
                                property.SetValue(item, Convert.ToDecimal(row[property.Name].ToString()), null);
                            else if (property.PropertyType == typeof(short) || property.PropertyType == typeof(short?))
                                property.SetValue(item, Convert.ToInt16(row[property.Name].ToString()), null);
                            else if (property.PropertyType == typeof(long) || property.PropertyType == typeof(long?))
                                property.SetValue(item, Convert.ToInt64(row[property.Name].ToString()), null);
                            else
                                property.SetValue(item, row[property.Name], null);
                        }
                    }
                }

                finally { }
            }

            return item;
        }

        public static DataSet ToDataSet<T>(IList<T> list)
        {
            Type elementType = typeof(T);
            DataSet ds = new DataSet();
            DataTable t = new DataTable();
            ds.Tables.Add(t);

            //add a column to table for each public property on T
            foreach (var propInfo in elementType.GetProperties())
            {
                Type ColType = Nullable.GetUnderlyingType(propInfo.PropertyType) ?? propInfo.PropertyType;
                t.Columns.Add(propInfo.Name, ColType);
            }

            //go through each property on T and add each value to the table
            foreach (T item in list)
            {
                DataRow row = t.NewRow();

                foreach (var propInfo in elementType.GetProperties())
                {
                    row[propInfo.Name] = propInfo.GetValue(item, null) ?? DBNull.Value;
                }

                t.Rows.Add(row);
            }

            return ds;
        }

        public static DataTable ToDataTable<T>(IEnumerable<T> Linqlist)
        {
            DataTable dt = new DataTable();
            PropertyInfo[] columns = null;

            if (Linqlist == null) return dt;

            foreach (T Record in Linqlist)
            {

                if (columns == null)
                {
                    columns = ((Type)Record.GetType()).GetProperties();
                    foreach (PropertyInfo GetProperty in columns)
                    {
                        Type colType = GetProperty.PropertyType;

                        if ((colType.IsGenericType) && (colType.GetGenericTypeDefinition()
                               == typeof(Nullable<>)))
                        {
                            colType = colType.GetGenericArguments()[0];
                        }

                        dt.Columns.Add(new DataColumn(GetProperty.Name, colType));
                    }
                }

                DataRow dr = dt.NewRow();

                foreach (PropertyInfo pinfo in columns)
                {
                    dr[pinfo.Name] = pinfo.GetValue(Record, null) == null ? DBNull.Value : pinfo.GetValue
                           (Record, null);
                }

                dt.Rows.Add(dr);
            }
            return dt;
        }

        private static List<string> ValidateHeaders(DataTable table, List<PropertyInfo> fileds)
        {
            List<string> header = new List<string>();
            List<string> columnList = new List<string>();

            foreach (DataColumn column in table.Columns)
            {
                if (!columnList.Contains(column.ColumnName))
                {
                    columnList.Add(column.ColumnName);
                }
                else
                {
                    throw new Exception($"Multiple header found \"{column.ColumnName}\" field.");
                }
            }

            foreach (PropertyInfo pinfo in fileds)
            {
                var field = columnList.Find(x => x == pinfo.Name);
                if (field == null)
                {
                    throw new Exception($"Excel doesn't contain \"{field}\" field.");
                }
                else
                {
                    header.Add(pinfo.Name);
                }
            }

            return header;
        }

        public static List<T> MapToList<T>(DataTable table) where T : new()
        {
            string TypeName = string.Empty;
            DateTime date = DateTime.Now;
            DateTime defaultDate = Convert.ToDateTime("1976-01-01");
            List<T> items = new List<T>();

            try
            {
                List<PropertyInfo> props = typeof(T).GetProperties().ToList();
                List<string> fieldNames = ValidateHeaders(table, props);

                if (table.Rows.Count > 0)
                {
                    int i = 0;
                    DataRow dr = null;
                    while (i < table.Rows.Count)
                    {
                        dr = table.Rows[i];

                        T t = new T();
                        props.ForEach(x =>
                        {
                            if (fieldNames.Contains(x.Name))
                            {
                                try
                                {
                                    if (x.PropertyType.IsGenericType)
                                        TypeName = x.PropertyType.GenericTypeArguments.First().Name;
                                    else
                                        TypeName = x.PropertyType.Name;

                                    switch (TypeName)
                                    {
                                        case nameof(Boolean):
                                            x.SetValue(t, Convert.ToBoolean(dr[x.Name]));
                                            break;
                                        case nameof(Int32):
                                            x.SetValue(t, Convert.ToInt32(dr[x.Name]));
                                            break;
                                        case nameof(Int64):
                                            x.SetValue(t, Convert.ToInt64(dr[x.Name]));
                                            break;
                                        case nameof(Decimal):
                                            x.SetValue(t, Convert.ToDecimal(dr[x.Name]));
                                            break;
                                        case nameof(String):
                                            x.SetValue(t, dr[x.Name].ToString());
                                            break;
                                        case nameof(DateTime):
                                            if (dr[x.Name].ToString() != null)
                                            {
                                                date = Convert.ToDateTime(dr[x.Name].ToString());
                                                date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                                                x.SetValue(t, date);
                                            }
                                            else
                                            {
                                                x.SetValue(t, defaultDate);
                                            }
                                            break;
                                        default:
                                            x.SetValue(t, dr[x.Name]);
                                            break;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    throw ex;
                                }
                            }
                        });

                        items.Add(t);
                        i++;
                    }
                }
            }
            catch
            {
                throw;
            }

            return items;
        }
    }
}
