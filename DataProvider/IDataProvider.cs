namespace DataProvider
{
    /// <summary>
    /// Popisuje triedu pre pracu s databazou.
    /// </summary>
    using System;
    using System.Data;

    public interface IDataProvider : Kros.KORM.IDatabase
    {

        /// <summary>
        /// Vykona stored proceduru s in/out parametrami.
        /// </summary>
        /// <typeparam name="TOutputValue"></typeparam>
        /// <param name="storedProcedureName"></param>
        /// <param name="outParamName">Nazov vystupneho parametra, ktoreho hodnota sa vrati.</param>
        /// <param name="params">Input/Output parametre</param>
        /// <returns>Hodnota prveho output parametra</returns>
        //new TOutputValue ExecuteStoredProcedure<TOutputValue>(string storedProcedureName, string outParamName,
        //    params StoredProcedureParameter[] @params) where TOutputValue : struct;

        /// <summary>
        /// Vykona dotaz a vrati pocet riadkov, ktore dany dotaz zmenil.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        Int32 ExecuteCommand(string query);

        /// <summary>
        /// Vykona dotaz a vrati pocet riadkov, ktore dany dotaz zmenil.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        Int32 ExecuteCommand(string query, params object[] @params);

        /// <summary>
        /// Vykona dotaz a vrati IDataReader.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        IDataReader ExecuteReader(string query);

        /// <summary>
        /// Vykona dotaz a vrati IDataReader.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        IDataReader ExecuteReader(string query, params object[] @params);

        /// <summary>
        /// Vykona dotaz a vrati hodnotu prveho stlpeca, prveho riadku. Ostatne riadky a stlpce sa ignoruju.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        new string ExecuteScalar(string query);

        /// <summary>
        /// Vykona dotaz a vrati hodnotu prveho stlpeca, prveho riadku. Ostatne riadky a stlpce sa ignoruju.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        /// <param name="params">Parametre</param>
        new string ExecuteScalar(string query, params object[] @params);

        /// <summary>
        /// Vykona dotaz a vrati hodnotu prveho stlpeca, prveho riadku. Ostatne riadky a stlpce sa ignoruju.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        new Nullable<T> ExecuteScalar<T>(string query) where T : struct;

        /// <summary>
        /// Vykona dotaz a vrati hodnotu prveho stlpeca, prveho riadku. Ostatne riadky a stlpce sa ignoruju.
        /// </summary>
        /// <param name="query">SQL dotaz.</param>
        /// <param name="params">Parametre</param>
        new Nullable<T> ExecuteScalar<T>(string query, params object[] @params) where T : struct;

        /// <summary>
        /// Nastavi zdielany adresar, ktory sa pouziva na vytvaranie zamkov, autocounter a drzanie prihlasenych uzivatelov.
        /// </summary>
        string GetSharedFolder();


        //Services.Locks.IDbLock CreateRecordLock(Int32 idUzivatel, Int32 idTabulka, Int32 idZaznam, Int32 idCol);

        //Services.Locks.IDbLock CreateTableLock(Int32 idUzivatel, Int32 idTabulka);

        bool IsMsSql();
    }

}
