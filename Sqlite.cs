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
        private const string DATA_SRC_PATH = @"D:\data\src\ror\myapp\db\development.sqlite3";

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
                            row.TwtID = (string)reader["twtid"];
                            row.ScreenName = (string)reader["twtname"];
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
            return $"【{Rating}】{R18}|{PxvName}({PxvID})-{Status}|{Warnings}";
        }
    }
}