﻿//MIT, 2015-2016, brezza92, EngineKit and contributors

using System;
using SharpConnect.MySql.Internal;
namespace SharpConnect.MySql
{
    namespace SyncPatt
    {
        public static partial class MySqlSyncPattExtension
        {

            public static void Prepare(this MySqlCommand cmd)
            {
                cmd.InternalPrepare();
            }
            public static MySqlDataReader ExecuteReader(this MySqlCommand cmd)
            {
                return cmd.InternalExecuteReader();
            }
            public static object ExecuteScalar(this MySqlCommand cmd)
            {
                object result = null;
                MySqlDataReader reader = cmd.InternalExecuteReader();
                if (reader.Read())
                {
                    result = reader.GetValue(0);
                }
                reader.Close();
                return result;
            }

            public static int ExecuteNonQuery(this MySqlCommand cmd)
            {
                cmd.InternalExecuteNonQuery();
                return (int)cmd.AffectedRows;
            }
        }
    }
    namespace AsyncPatt
    {
        public static partial class MySqlAsyncPattExtension
        {

            public static void Prepare(this MySqlCommand cmd, Action nextAction)
            {
                cmd.InternalPrepare(nextAction);
            }
            public static void ExecuteReader(this MySqlCommand cmd, Action<MySqlDataReader> readerReady)
            {
                cmd.InternalExecuteReader(readerReady);
            }
            public static void ExecuteSubTableReader(this MySqlCommand cmd, Action<MySqlDataReader> readerReady)
            {
                cmd.InternalExecuteSubTableReader(readerReady);
            }
            /// <summary>
            /// non-blocking, read only single value
            /// </summary>
            /// <param name="nextAction"></param>
            /// <returns></returns>
            public static void ExecuteScalar(this MySqlCommand cmd, Action<object> resultReady)
            {
                cmd.InternalExecuteSubTableReader(reader =>
                {
                    object result = reader.GetValue(0);
                    //call user result ready***
                    resultReady(result);
                    //
                });
            }
            /// <summary>
            /// sync/async execute non query
            /// </summary>
            /// <param name="nextAction"></param>
            public static void ExecuteNonQuery(this MySqlCommand cmd, Action nextAction)
            {
                cmd.InternalExecuteNonQuery(nextAction);
            }
        }
    }

    public class MySqlCommand
    {
        Query _query;
        bool _isPreparedStmt;
        SqlStringTemplate _sqlStringTemplate;

        public MySqlCommand(string sql)
            : this(new SqlStringTemplate(sql), new CommandParams(), null)
        {
        }
        public MySqlCommand(string sql, MySqlConnection conn)
            : this(new SqlStringTemplate(sql), new CommandParams(), conn)
        {
        }
        public MySqlCommand(string sql, CommandParams cmds, MySqlConnection conn)
            : this(new SqlStringTemplate(sql), cmds, conn)
        {

        }
        public MySqlCommand(SqlStringTemplate sql, MySqlConnection conn)
            : this(sql, new CommandParams(), conn)
        {
        }

        public MySqlCommand(SqlStringTemplate sql, CommandParams cmds, MySqlConnection conn)
        {
            _sqlStringTemplate = sql;
            Connection = conn;
            Parameters = cmds;
        }

        public CommandParams Parameters
        {
            get;
            private set;
        }
        public string CommandText
        {
            get { return this._sqlStringTemplate.UserRawSql; }
        }
        public MySqlConnection Connection { get; set; }

        public IStringConverter StringConverter
        {
            get;
            set;
        }
        /// <summary>
        /// sync/async prepare
        /// </summary>
        /// <param name="nextAction"></param>
        internal void InternalPrepare(Action nextAction = null)
        {
            //prepare sql command;
            _isPreparedStmt = true;
            _query = new Query(Connection.Conn, _sqlStringTemplate, Parameters);
            _query.Prepare(nextAction);
        }
        /// <summary>
        /// sync execute reader
        /// </summary>
        /// <returns></returns>
        internal MySqlDataReader InternalExecuteReader()
        {
            if (!_isPreparedStmt)
            {
                _query = new Query(this.Connection.Conn, _sqlStringTemplate, Parameters);
            }
            var reader = new MySqlQueryDataReader(_query);

            reader.StringConverter = this.StringConverter;
            _query.Execute(true, null);
            reader.WaitUntilFirstDataArrive();
            //
            //after execute in sync mode (this method)
            //reader will wait unit first result arrive            
            return reader;
        }

        /// <summary>
        /// async exec reader, notify the when reader is ready
        /// </summary>
        internal void InternalExecuteReader(Action<MySqlDataReader> readerReady)
        {
            if (!_isPreparedStmt)
            {
                _query = new Query(this.Connection.Conn, _sqlStringTemplate, Parameters);
            }
            var reader = new MySqlQueryDataReader(_query);
            reader.StringConverter = this.StringConverter;
            //in non bloking mode, set this
            reader.SetFirstDataArriveDelegate(dataReader =>
            {
                //data reader is ready
                //then start async read on each sub table
                readerReady(dataReader);
            });
            //after execute in asyn mode( this method)
            //reader just return, not block,
            //
            //and when the first data arrive,
            //in invoke dataReaderReader delegate
            _query.Execute(true, () => { });//send empty lambda for async  
        }
        /// <summary>
        /// async exec, on each sub table
        /// </summary>
        internal void InternalExecuteSubTableReader(Action<MySqlDataReader> onEachSubTable)
        {
            if (!_isPreparedStmt)
            {
                _query = new Query(this.Connection.Conn, _sqlStringTemplate, Parameters);
            }
            var reader = new MySqlQueryDataReader(_query);
            reader.StringConverter = this.StringConverter;
            //in non bloking mode, set this
            reader.SetFirstDataArriveDelegate(dataReader =>
            {
                //data reader is ready
                //then start async read on each sub table
                dataReader.ReadSubTable(subt =>
                {
                    //table is ready for read***
                    //just read single value 
                    var subtReader = subt.CreateDataReader();
                    subtReader.StringConverter = this.StringConverter;
                    onEachSubTable(subtReader);

                    if (subt.IsLastTable)
                    {
                        //auto close reader 
                        dataReader.InternalClose(() => { });//send empty lambda for async  
                    }
                });
            });
            //after execute in asyn mode( this method)
            //reader just return, not block,
            //
            //and when the first data arrive,
            //in invoke dataReaderReader delegate
            _query.Execute(true, () => { });//send empty lambda for async  
        }

        /// <summary>
        /// sync/async execute non query
        /// </summary>
        /// <param name="nextAction"></param>
        internal void InternalExecuteNonQuery(Action nextAction = null)
        {
            if (!_isPreparedStmt)
            {
                _query = new Query(Connection.Conn, _sqlStringTemplate, Parameters);
            }
            _query.Execute(false, nextAction);
        }

        public uint LastInsertedId
        {
            get
            {
                //after execute non query
                return _query.OkPacket.insertId;
            }
        }
        public uint AffectedRows
        {
            get
            {//after execute non query
                return _query.OkPacket.affectedRows;
            }
        }
    }



}