using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace MyZipper
{
    public class Sqlite
    {
#if DEBUG
        private const string DATA_SRC_PATH = @"D:\export-done\test\out\development.sqlite3";
#else
        private const string DATA_SRC_PATH = @"D:\data\src\ror\myapp\db\development.sqlite3";
#endif

        public static void GetTwtUserInfo(string twitter_id, TwtRow row)
        {
            using (var cn = GetSQLiteConnection())
            {
                cn.Open();
                using (var cmd = new SQLiteCommand(cn))
                {
                    cmd.CommandText = $"SELECT * FROM twitters WHERE twtid = '{twitter_id}'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                row.TwtID = (string)reader["twtid"];
                                if (reader["twtname"].Equals(DBNull.Value))
                                {
                                    row.ScreenName = "unset";
                                }
                                else
                                {
                                    row.ScreenName = (string)reader["twtname"];
                                }
                            }
                            catch (FormatException ex)
                            {
                                Log.E("NULL??{0}", ex);
                                row.ScreenName = "";
                            }
                        }
                        else
                        {
                            Log.D($"no read '{twitter_id}'");
                        }
                    }
                }
            }
        }

        public static void GetPxvUserInfo(int pxvid, PxvRow row)
        {
            using (var cn = GetSQLiteConnection())
            {
                cn.Open();
                using (var cmd = new SQLiteCommand(cn))
                {
                    cmd.CommandText = $"SELECT * FROM artists WHERE pxvid = '{pxvid}'";
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            row.PxvID = (long)reader["pxvid"];
                            row.PxvName = (string)reader["pxvname"];
                            row.Rating = (long)reader["rating"];
                            row.R18 = (string)reader["r18"];
                            row.Feature = (string)reader["feature"];
                            row.Filenum = (long)reader["filenum"];
                            row.Status = (string)reader["status"];
                            row.Warnings = (string)reader["warnings"];
                        }
                        else
                        {
                            Log.D($"no read '{pxvid}'");
                        }
                    }
                }
            }
        }

        public static void UpdatePxvRecord_ZippedAt(int pxv_user_id)
        {
            using (var cn = GetSQLiteConnection())
            {
                cn.Open();
                using (var cmd = new SQLiteCommand(cn))
                {
                    //cmd.CommandText = $"SELECT * FROM artists WHERE pxvid = '{pxvid}'";
                    var tbl_name = "artists";
                    var col_name = "zipped_at";
                    var col_name_cond = "pxvid";
                    cmd.CommandText = $"UPDATE {tbl_name} set {col_name} = CURRENT_TIMESTAMP WHERE {col_name_cond} = {pxv_user_id}";
                    var result = cmd.ExecuteNonQuery();
                    if (result > 0)
                    {
                        Log.D($"更新成功'{pxv_user_id}'");
                    }
                    else
                    {
                        Log.D($"更新失敗'{pxv_user_id}'");
                    }
                }
            }
        }

        private static SQLiteConnection GetSQLiteConnection()
        {
            var sqlConnectionSb = new SQLiteConnectionStringBuilder { DataSource = DATA_SRC_PATH };
            return new SQLiteConnection(sqlConnectionSb.ToString());
        }
    }

    public class TwtRow
    {
        public string TwtID { set; get; }
        public string ScreenName { set; get; }

        public override string ToString()
        {
            return ScreenName + "(@" + TwtID +")";
        }
    }

    public class PxvRow
    {
        public long PxvID { set; get; }
        public string PxvName { set; get; }
        public long Rating { set; get; }
        public string R18 { set; get; }
        public string Feature { set; get; }

        public long Filenum { set; get; }
        public string Status { set; get; }

        public string Warnings { set; get; }

        public override string ToString()
        {
            return $"【{Rating}】{R18}|{PxvName}({PxvID})-{Status}|{Warnings}({Filenum})";
        }
    }
}