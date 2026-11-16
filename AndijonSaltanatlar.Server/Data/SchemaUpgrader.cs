using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace AndijonSaltanatlar.Server.Data
{
    /// <summary>
    /// Qo'shimcha (additive) sxema yangilagich: mavjud bazaga YANGI jadval, ustun va indekslarni qo'shadi.
    /// Hech narsa o'chirilmaydi va o'zgartirilmaydi — mavjud ma'lumot saqlanadi (EnsureCreated buni qilolmaydi).
    /// SQLite va PostgreSQL uchun ishlaydi. Ustun turini o'zgartirish/o'chirish kerak bo'lsa — EF migratsiyasi yozilishi kerak.
    /// </summary>
    public static class SchemaUpgrader
    {
        public static void Upgrade(GameDbContext db, ILogger log)
        {
            bool sqlite = (db.Database.ProviderName ?? "").Contains("Sqlite", StringComparison.OrdinalIgnoreCase);
            DbConnection conn = db.Database.GetDbConnection();
            bool opened = conn.State != System.Data.ConnectionState.Open;
            if (opened) conn.Open();
            try
            {
                int tables = RunCreateScript(db, conn);
                int columns = AddMissingColumns(db, conn, sqlite);
                if (columns > 0) RunCreateScript(db, conn); // yangi ustunlarga bog'liq indekslar
                if (tables + columns > 0) log?.LogInformation("Sxema yangilandi: {Tables} ta yangi jadval/indeks, {Columns} ta yangi ustun.", tables, columns);
            }
            finally
            {
                if (opened) conn.Close();
            }
        }

        /// <summary>Modelning CREATE skriptini bo'laklab bajaradi; mavjud obyektlar xatosi e'tiborsiz qoldiriladi.</summary>
        private static int RunCreateScript(GameDbContext db, DbConnection conn)
        {
            int ok = 0;
            foreach (string raw in db.Database.GenerateCreateScript().Split(new[] { ";\r\n", ";\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                string sql = raw.Trim();
                if (sql.Length == 0 || sql == "GO") continue;
                if (Exec(conn, sql)) ok++;
            }
            return ok;
        }

        private static int AddMissingColumns(GameDbContext db, DbConnection conn, bool sqlite)
        {
            int added = 0;
            foreach (IEntityType et in db.Model.GetEntityTypes())
            {
                string table = et.GetTableName();
                if (table == null) continue;
                HashSet<string> existing = Columns(conn, table, sqlite);
                if (existing.Count == 0) continue;
                foreach (IProperty p in et.GetProperties())
                {
                    string col = p.GetColumnName();
                    if (existing.Contains(col)) continue;
                    string def = p.IsColumnNullable() ? "NULL" : "NOT NULL DEFAULT " + DefaultLiteral(p.ClrType, sqlite);
                    if (Exec(conn, "ALTER TABLE \"" + table + "\" ADD COLUMN \"" + col + "\" " + p.GetColumnType() + " " + def)) added++;
                }
            }
            return added;
        }

        private static string DefaultLiteral(Type t, bool sqlite)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            if (t == typeof(string)) return "''";
            if (t == typeof(bool)) return sqlite ? "0" : "FALSE";
            if (t == typeof(DateTime)) return sqlite ? "'0001-01-01 00:00:00'" : "'0001-01-01'";
            return "0";
        }

        private static HashSet<string> Columns(DbConnection conn, string table, bool sqlite)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using DbCommand cmd = conn.CreateCommand();
            cmd.CommandText = sqlite
                ? "PRAGMA table_info(\"" + table + "\")"
                : "SELECT column_name FROM information_schema.columns WHERE table_name = '" + table.Replace("'", "''") + "'";
            try
            {
                using DbDataReader r = cmd.ExecuteReader();
                while (r.Read()) set.Add(r.GetString(sqlite ? 1 : 0));
            }
            catch { }
            return set;
        }

        private static bool Exec(DbConnection conn, string sql)
        {
            try
            {
                using DbCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false; // allaqachon mavjud
            }
        }
    }
}
