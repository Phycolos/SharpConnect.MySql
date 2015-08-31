﻿//MIT 2015, brezza27, EngineKit and contributors

using System;
using System.Collections.Generic;
using SharpConnect.MySql;

namespace MySqlTest
{
    public class TestSet1 : MySqlTestSet
    {
        [Test]
        public static void T_OpenAndClose()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {

                var conn = new MySqlConnection(connStr);
                conn.Open();
                //connList.Add(conn);
                conn.Close();
            });
            Report.WriteLine("avg:" + avg);
        }
        [Test]
        public static void T_OpenNotClose()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            List<MySqlConnection> connList = new List<MySqlConnection>();
            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {

                var conn = new MySqlConnection(connStr);
                conn.Open();
                connList.Add(conn);
            });
            Report.WriteLine("avg:" + avg);

            //clear
            foreach (var conn in connList)
            {
                conn.Close();
            }
            connList.Clear();
        }
        [Test]
        public static void T_OpenAndCloseWithConnectionPool()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {
                var conn = new MySqlConnection(connStr);
                conn.UseConnectionPool = true;
                conn.Open();
                conn.Close();
            });

            Report.WriteLine("avg:" + avg);
        }


        [Test]
        public static void T_Select_sysdate()
        {
            int n = 100;
            long total;
            long avg;
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            Test(n, TimeUnit.Ticks, out total, out avg, () =>
            {
                var cmd = new MySqlCommand("select sysdate()", conn);
                var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var dtm = reader.GetDateTime(0);
                }
                reader.Close();
            });

            Report.WriteLine("avg:" + avg);

            conn.Close();
        }

        [Test]
        public static void T_CreateTable()
        {


            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            var cmd = new MySqlCommand("create table user_info2(uid int(10),u_name varchar(45));", conn);
            cmd.ExecuteNonQuery();
            Report.WriteLine("ok");
            conn.Close();
        }

        [Test]
        public static void T_DropCreateInsert()
        {


            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            //1. drop table
            {
                var cmd = new MySqlCommand("drop table if exists user_info2", conn);
                cmd.ExecuteNonQuery();
            }
            //2. create new one
            {
                var cmd = new MySqlCommand("create table user_info2(uid int(10),u_name varchar(45));", conn);
                cmd.ExecuteNonQuery();
            }
            //3. add some data
            {
                var cmd = new MySqlCommand("insert into user_info2(uid, u_name) values(?uid, 'abc')", conn);
                cmd.Parameters.AddWithValue("uid", 10);
                cmd.ExecuteNonQuery();
            }

            Report.WriteLine("ok");
            conn.Close();
        }

        [Test]
        public static void T_StringEscape()
        {


            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();
            //1. drop table
            {
                var cmd = new MySqlCommand("drop table if exists user_info2", conn);
                cmd.ExecuteNonQuery();
            }
            //2. create new one
            {
                var cmd = new MySqlCommand("create table user_info2(uid int(10),u_name varchar(45));", conn);
                cmd.ExecuteNonQuery();
            }
            //3. add some data
            {
                var cmd = new MySqlCommand("insert into user_info2(uid, u_name) values(?uid, '?????')", conn);
                cmd.Parameters.AddWithValue("uid", 10);
                cmd.ExecuteNonQuery();
            }

            Report.WriteLine("ok");
            conn.Close();
        }

        [Test]
        public static void T_DateTimeData()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            {
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, myname varchar(20), col1 datetime," +
                "col2 date,col3 time,col4 timestamp, primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "insert into test001(myname,col1,col2,col3,col4) values(?myname,?col1,?col2,?col3,?col4)";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("myname", "OKOK!");
                cmd.Parameters.AddWithValue("col1", DateTime.Now);
                cmd.Parameters.AddWithValue("col2", DateTime.Now);
                cmd.Parameters.AddWithValue("col3", DateTime.Now);
                cmd.Parameters.AddWithValue("col4", DateTime.Now);
                cmd.ExecuteNonQuery();
            }
            conn.Close();
            Report.WriteLine("ok");
        }

        [Test]
        public static void T_StringData1()
        {
            var connStr = GetMySqlConnString();
            var conn = new MySqlConnection(connStr);
            conn.Open();

            {
                string sql = "drop table if exists test001";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "create table test001(col_id  int(10) unsigned not null auto_increment, myname varchar(20), col1 char(2)," +
                "col2 varchar(10), primary key(col_id) )";
                var cmd = new MySqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
            }

            {
                string sql = "insert into test001(myname,col1,col2) values(?myname,?col1,?col2)";
                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("myname", "OKOK!");
                cmd.Parameters.AddWithValue("col1", "OK"); //2
                cmd.Parameters.AddWithValue("col2", "1000");
                cmd.ExecuteNonQuery();
            }
            conn.Close();
            Report.WriteLine("ok");
        }
       
    }
}