using System;
using System.Data;
using Microsoft.Data.SqlClient;
namespace QuanLyKhachSan.Data {
 public static class Db {
  public static string Cs { get { return Environment.GetEnvironmentVariable("HOTEL_DB_CONNECTION") ?? "Server=localhost\\SQLEXPRESS;Database=QuanLyKhachSanLab3;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10"; } }
  public static DataTable Query(string sql, params SqlParameter[] ps) { using(var c=new SqlConnection(Cs)) using(var cmd=new SqlCommand(sql,c)) using(var a=new SqlDataAdapter(cmd)) { cmd.Parameters.AddRange(ps); var d=new DataTable(); a.Fill(d); return d; } }
  public static void Run(string proc, params SqlParameter[] ps) { using(var c=new SqlConnection(Cs)) using(var cmd=new SqlCommand(proc,c)) { cmd.CommandType=CommandType.StoredProcedure; cmd.Parameters.AddRange(ps); c.Open(); cmd.ExecuteNonQuery(); } }
  public static SqlParameter P(string name, object value) { return new SqlParameter(name,value ?? DBNull.Value); }
 }
}
