using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace DegreeQuest
{
    /* Database communication abstraction */
    public class DBReader
    {
        public static readonly String myConnectionString = "server=10.25.71.66;uid=dbu309gb2;pwd=ODcxNjc3ZGI4;database=db309gb2;";
        public static readonly MySqlConnection conn = new MySqlConnection(myConnectionString);

        /// <summary>
        /// current purpose is to test the connection by query the whole table and printing out everything in the table
        /// </summary>
        /// <param name="tableName">the name of the table that is to be queried</param>
        public static void queryAll(String tableName)
        {
            try
            {
                conn.Open();
                String sSQL = "SELECT * FROM " + tableName;
                MySqlCommand cmd = new MySqlCommand(sSQL, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                Console.WriteLine(rdr.ToString());
                rdr.Close();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        /// <summary>
        /// this function takes in the table name the colmn name and a unique key and finds the record 
        /// associated with that key and deletes it.
        /// </summary>
        /// <param name="tableName">the name of the table in MySQL</param>
        /// <param name="columnName">the name of the colomn that contains the primary key</param>
        /// <param name="key">the string value of the primary key that wants to be deleted</param>
        public static void deleteRecordUsingKey(String tableName, String columnName, String key)
        {
            try
            {
                conn.Open();
                String sSQL = "DELETE FROM " + tableName + "WHERE " + columnName + "='" + key + "'";
                MySqlCommand cmd = new MySqlCommand(sSQL, conn);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        /// <summary>
        /// this function takes in the table name the colmn names, type, and defaults and creates the table in the database.
        /// destroys previous table if it exists.
        /// completely unchecked.
        /// </summary>
        /// <param name="tableName">the name of the table in MySQL</param>
        /// <param name="columnNames">the name of the colomns that contains the primary key</param>
        /// <param name="types">the type for each column</param>
        /// <param name="defaults">the default value for each column</param>
        public static void createTable(String tableName, String[] colmnNames, String[] types, String[] defaults)
        {
            try
            {
                conn.Open();
                String sSQL = "drop table if exists " + tableName;
                MySqlCommand cmd = new MySqlCommand(sSQL, conn);
                cmd.ExecuteNonQuery();

                sSQL = "CREATE TABLE " + tableName + '(';
                sSQL = sSQL + "id INT UNSIGNED NOT NULL AUTO_INCREMENT,";
                for (int i = 0; i < colmnNames.Length; i++)
                {
                    sSQL = sSQL  + colmnNames[i] + " " + types[i] + " NOT NULL";
                    if(!(defaults[i] == null))
                        sSQL = sSQL + " DEFAULT '" + defaults[i];
                    sSQL = sSQL + "' ,";
                }
                sSQL = sSQL + " PRIMARY KEY (id));";
                cmd = new MySqlCommand(sSQL, conn);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        /// <summary>
        ///takes in the table name the name of the colomns that the values go into
        ///and the values as parameters and creates a new record and inserts it into the table
        /// </summary>
        /// <param name="tableName">the name of the table that is to be inserted into</param>
        /// <param name="colmnNames">the colomn names that of the table</param>
        /// <param name="values">the values to insert into the colomns</param>
        public static void insertRecord(String tableName, String[] colmnNames, String[] values)
        {
            try
            {
                conn.Open();
                String sSQL = "insert into " + tableName + '(';
                for(int i = 0; i < colmnNames.Length; i++)
                {
                    if (i == 0)
                        sSQL = sSQL + colmnNames[i];
                    else
                        sSQL = sSQL + "," + colmnNames[i];
                }
                sSQL = sSQL + ") VALUES (";
                for (int i = 0; i < values.Length; i++)
                {
                    if (i == 0)
                        sSQL = sSQL + "'" + values[i] + "'";
                    else
                        sSQL = sSQL + ",'" + values[i] + "'";
                }
                sSQL = sSQL + ");";
                MySqlCommand cmd = new MySqlCommand(sSQL, conn);
                cmd.ExecuteNonQuery();
            }
            catch(MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            }
            conn.Close();
        }

        ///<summary>
        ///This function updates parts of the table.
        /// </summary>
        ///<param name="tableName">the name of the table that is changed.</param>
        ///<param name="colmnNames">the list of column names that are changed.</param>
        ///<param name="values">the list of values that colmnNames are changed to.</param>
        ///<param name="colmnID">the column name that is used as the key.</param>
        ///<param name="key">the key that the program uses to find which row it will change.</param>
        public static void updateRow(String tableName, String[] colmnNames, String[] values, String colmnID, String key)
        {
            //incase nothing is passed into colmnNames
            if (colmnNames.Length <= 0)
                throw new System.ArgumentNullException("colmnNames", "There is nothing in the array of colmnNames");
            if(colmnNames.Length != values.Length)
                throw new System.ArgumentException("The vales array is not the same length as the colmnNames array", "values");

            try
            {
                conn.Open();
                String sSQL = "UPDATE " + tableName + " SET ";
                sSQL = sSQL + colmnNames[0] + " = '" + values[0] + "' ";
                for (int i = 1; i < colmnNames.Length; i++)
                {
                    sSQL = sSQL + ", " +colmnNames[i] + " = " + values[i];
                }
                sSQL = sSQL + " WHERE " + colmnID + " = " + key;
                MySqlCommand cmd = new MySqlCommand(sSQL, conn);
                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.ToString());
            } 
            conn.Close();
            
        }

        /// <summary>
        /// This is a general method that returns a string of what has been found from the query.
        /// </summary>
        /// <param name="tableName">the name of the table to be queried.</param>
        /// <param name="colmnNames">the name of the colomns that data is wanted from.</param>
        /// <param name="colmnConditionID">the id of the colomn that is to be checked.</param>
        /// <param name="condition">the condition that the colomn id needs to be met.</param>
        /// <returns></returns>
        public static String selectQuery(String tableName, String[] colmnNames, String colmnConditionID, String condition)
        {
            //incase nothing is passed into the funciton
            if (colmnNames.Length <= 0)
                throw new System.ArgumentNullException("colmnNames", "There is nothing in the colmnNames array");
            String readed = "";
            try
            {
                conn.Open();
                String query = "SELECT " + colmnNames[0];
                for(int i = 1; i < colmnNames.Length; i++)
                {
                    query = query + ", " + colmnNames[i];
                }
                query = query + " WHERE " + colmnConditionID + "=" + condition;
                MySqlCommand cmd = new MySqlCommand(query, conn);
                readed = interpretReader(cmd.ExecuteReader());
            }
            catch(MySqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                conn.Close();
            }
            return readed; 
        }


        public static String[] random(String tableName)
        {
            String[] readed = null;
            try
            {
                conn.Open();
                String query = "SELECT COUNT(*) FROM " + tableName;
                MySqlCommand cmd = new MySqlCommand(query, conn);
                int num = new Random().Next(1,Convert.ToInt32(cmd.ExecuteScalar())+1);
                query = "SELECT * FROM " + tableName + " WHERE id = '"+num.ToString()+"'";
                cmd = new MySqlCommand(query, conn);
                readed = readRow(cmd.ExecuteReader());
            }
            catch (MySqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                conn.Close();
            }
            return readed;
        }

        private static String[] readRow(MySqlDataReader row)
        {
            int n = row.FieldCount;
            String[] values = new String[n];

            try
            {
                row.Read();
                for(int i=0;i<n;i++)
                {
                    values[i] = row.GetString(i);
                }

            }
            catch (MySqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                row.Close();
            }
            return values;
        }

        /*
         *Helper method that takes a MySqlDataReader object and returns a string that 
         * is what is in the reader object 
         */
        private static String interpretReader(MySqlDataReader reader)
        {
            String values = "";
            try
            {
            
                while (reader.Read())
                {
                    values = values + reader.GetString(0);
                }
  
            }
            catch(MySqlException e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {
                reader.Close();
            }
            return values;
        }
    }
}
