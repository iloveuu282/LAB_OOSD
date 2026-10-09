using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using QuanLyCongTyDuLich.Data;
using QuanLyCongTyDuLich.Services;

namespace QuanLyCongTyDuLich.Tests
{
    internal static class Program
    {
        private static readonly DangKyDoanService Doan = new DangKyDoanService();
        private static readonly PhanCongService PC = new PhanCongService();
        private static readonly KetThucService KT = new KetThucService();
        private static readonly ChuyenLeService Chuyen = new ChuyenLeService();
        private static DateTime FutureDoan { get { return Later(new DateTime(2027, 1, 5), DateTime.Today.AddDays(90)); } }
        private static DateTime Later(DateTime a, DateTime b) { return a > b ? a : b; }
        private static void Assert(bool ok, string detail) { if (!ok) throw new Exception(detail); }
        private static void Ok(KetQuaXuLy k) { Assert(k.ThanhCong, k.ThongBao); }
        private static void Fail(KetQuaXuLy k) { Assert(!k.ThanhCong, "Phải từ chối nhưng đã thành công: " + k.ThongBao); }
        private static int Count(string sql, params SqlParameter[] p) { return Convert.ToInt32(Db.Scalar(sql,p)); }
        private static decimal Money(string sql, params SqlParameter[] p) { return Convert.ToDecimal(Db.Scalar(sql,p)); }
        private static void SafeDatabase()
        {
            var b = new SqlConnectionStringBuilder(Db.ConnectionString);
            if (!Regex.IsMatch(b.InitialCatalog, @"^[A-Za-z][A-Za-z0-9_]{0,80}_Test$"))
                throw new Exception("Bộ test chỉ chạy trên database có hậu tố _Test. Cấu hình bằng Configure-Database.ps1.");
        }
        private static void Reset()
        {
            SafeDatabase();
            var b = new SqlConnectionStringBuilder(Db.ConnectionString);
            string database = b.InitialCatalog;
            b.InitialCatalog = "master";
            using (var cn = new SqlConnection(b.ConnectionString))
            {
                cn.Open();
                string sql = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"QuanLyCongTyDuLich.sql"), Encoding.UTF8)
                    .Replace("QuanLyCongTyDuLich",database);
                foreach (string batch in Regex.Split(sql, @"^\s*GO\s*(?:--[^\r\n]*)?\r?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    using(var cmd = new SqlCommand(batch,cn)) { cmd.CommandTimeout=60; cmd.ExecuteNonQuery(); }
                }
            }
            // Chỉ dời các lịch tương lai khi chúng đã qua; giữ lịch sử và ngày đăng ký gốc cho TC23/24.
            DateTime cl2 = Later(new DateTime(2026,11,15),DateTime.Today.AddDays(30));
            DateTime cl3 = Later(new DateTime(2026,11,20),DateTime.Today.AddDays(40));
            DateTime dd2 = Later(new DateTime(2026,12,10),DateTime.Today.AddDays(60));
            Db.Execute("UPDATE ChuyenLe SET NgayDi=@d, NgayVe=@v WHERE MaChuyen='CL002'",Db.P("@d",cl2),Db.P("@v",cl2.AddDays(2)));
            Db.Execute("UPDATE ChuyenLe SET NgayDi=@d, NgayVe=@v WHERE MaChuyen='CL003'",Db.P("@d",cl3),Db.P("@v",cl3.AddDays(3)));
            Db.Execute("UPDATE DangKyDoan SET NgayDi=@d, NgayKetThucDuKien=@v WHERE SoDKDoan='DD002'",Db.P("@d",dd2),Db.P("@v",dd2.AddDays(3)));
        }
        private static List<ThanhVienDoanItem> Members(int count)
        {
            return Enumerable.Range(1,count).Select(i => new ThanhVienDoanItem {HoTen="Khách thử " + i,NgaySinh=new DateTime(2000,1,1),SoGiayTo="TEST"+i}).ToList();
        }
        private static KetQuaXuLy Register(bool bh=false, int people=15, decimal deposit=30000000M, List<ThanhVienDoanItem> members=null)
        {
            return Doan.DangKy("DD003","DK03","Đoàn thử nghiệm","10 Lê Lợi, TP.HCM","0900000000","Nguyễn Văn A", "T003",FutureDoan,people,"TP.HCM",bh,deposit,members);
        }
        private static void AssignToCancel() { Ok(PC.PhanCong("PC_HUY","HDV03",QuyDinh.Doan,"DD002",1000000M)); }
        private static void CreateTrip() { Ok(Chuyen.ThemChuyen("CL004","T001",new DateTime(2026,12,15),"Nhà Văn hóa Thanh Niên TP.HCM")); }
        private static Dictionary<string,Action> Cases()
        {
            return new Dictionary<string,Action>
            {
                {"TC01",()=> { Ok(new DanhMucService().ThemHDV("HDV04","HDV thử","0900000004",9000000M)); Assert(Count("SELECT COUNT(*) FROM HuongDanVien WHERE MaHDV='HDV04'")==1,"Không lưu được HDV04"); }},
                {"TC02",()=> { Ok(new TourService().ThemTour("T004","Phú Quốc",3,2,5500000M,"TP.HCM - Phú Quốc - TP.HCM")); Assert(Money("SELECT DonGiaKhach FROM Tour WHERE MaTour='T004'")==5500000M,"Sai đơn giá"); }},
                {"TC03",()=> { Fail(new TourService().ThemDiemDung("T001",2,"Cần Thơ",false,true,true,3,"")); Assert(Count("SELECT COUNT(*) FROM TourDiemDung WHERE MaTour='T001' AND ThuTu=2")==1,"Trùng điểm dừng"); }},
                {"TC04",()=> { CreateTrip(); Assert(Convert.ToDateTime(Db.Scalar("SELECT NgayVe FROM ChuyenLe WHERE MaChuyen='CL004'"))==new DateTime(2026,12,17),"Ngày về sai"); }},
                {"TC05",()=> { Ok(new DangKyLeService().DangKy("DKL003","CL002","DB01","Khách thử","0900000000",4)); Assert(Money("SELECT ThanhTien FROM DangKyLe WHERE SoDKLe='DKL003'")==10000000M,"Sai thành tiền"); }},
                {"TC06",()=> { Fail(new DangKyLeService().DangKy("DKL003","CL002","DB01","Khách thử","0900000000",12)); Assert(Count("SELECT COUNT(*) FROM DangKyLe WHERE SoDKLe='DKL003'")==0,"Đã lưu phiếu không hợp lệ"); }},
                {"TC07",()=> { Fail(new DangKyLeService().DangKy("DKL003","CL001","DB01","Khách thử","0900000000",4)); }},
                {"TC08",()=> { Ok(Register()); Assert(Money("SELECT TongTienDuKien FROM DangKyDoan WHERE SoDKDoan='DD003'")==133500000M,"Sai tổng dự kiến"); Assert(Convert.ToDateTime(Db.Scalar("SELECT NgayKetThucDuKien FROM DangKyDoan WHERE SoDKDoan='DD003'"))==FutureDoan.AddDays(4),"Sai ngày kết thúc"); }},
                {"TC09",()=> { Fail(Register(false,12)); }},
                {"TC10",()=> { Fail(Register(true,15,30000000M,Members(14))); Assert(Count("SELECT COUNT(*) FROM DangKyDoan WHERE SoDKDoan='DD003'")==0,"Phiếu bảo hiểm không hợp lệ đã lưu"); }},
                {"TC11",()=> { Fail(Register(false,15,0)); }},
                {"TC12",()=> { AssignToCancel(); Ok(Doan.HuyDangKy("DD002")); Assert(Convert.ToString(Db.Scalar("SELECT TrangThai FROM DangKyDoan WHERE SoDKDoan='DD002'"))==QuyDinh.HuyMatCoc,"Sai trạng thái hủy"); Assert(Count("SELECT COUNT(*) FROM PhanCongHDV WHERE SoDKDoan='DD002'")==0,"Còn phân công"); }},
                {"TC13",()=> { Ok(PC.PhanCong("PC004","HDV01",QuyDinh.Le,"CL002",1500000M)); }},
                {"TC14",()=> { Ok(PC.PhanCong("PC004","HDV01",QuyDinh.Le,"CL002",1500000M)); Fail(PC.PhanCong("PC005","HDV02",QuyDinh.Le,"CL002",1500000M)); }},
                {"TC15",()=> { Fail(PC.PhanCong("PC004","HDV02",QuyDinh.Doan,"DD001",1500000M)); }},
                {"TC16",()=> { Ok(PC.PhanCong("PC004","HDV01",QuyDinh.Doan,"DD001",1500000M)); Assert(Count("SELECT COUNT(*) FROM PhanCongHDV WHERE SoDKDoan='DD001'")==3,"Đoàn phải có 3 HDV"); }},
                {"TC17",()=> { Fail(KT.ThanhToanDoan("TT001","DD001",new DateTime(2026,9,15),50000000M,"")); Assert(Count("SELECT COUNT(*) FROM ThanhToanDoan")==0,"Đã ghi tiền vượt mức"); }},
                {"TC18",()=> { Ok(KT.ThanhToanDoan("TT001","DD001",new DateTime(2026,9,15),40000000M,"")); Assert(Convert.ToString(Db.Scalar("SELECT TrangThai FROM DangKyDoan WHERE SoDKDoan='DD001'"))==QuyDinh.HoanTatThanhToan,"Chưa chuyển trạng thái thanh toán đủ"); }},
                {"TC19",()=> { Ok(Register()); Fail(KT.ThanhToanDoan("TT001","DD003",FutureDoan.AddDays(3),1000000M,"")); }},
                {"TC20",()=> { Ok(KT.GuiKhaoSat("KS002",QuyDinh.Doan,"DD001",new DateTime(2026,9,15))); Assert(Count("SELECT COUNT(*) FROM KhaoSat WHERE MaKhaoSat='KS002'")==1,"Chưa lưu khảo sát"); }},
                {"TC21",()=> { Fail(KT.GuiKhaoSat("KS002",QuyDinh.Le,"DKL002",DateTime.Today)); }},
                {"TC22",()=> { Ok(KT.GuiKhaoSat("KS002",QuyDinh.Doan,"DD001",new DateTime(2026,9,15))); Ok(KT.GhiPhanHoi("KS002",new DateTime(2026,9,16),4,"Lịch trình hợp lý")); Assert(Count("SELECT DiemDanhGia FROM KhaoSat WHERE MaKhaoSat='KS002'")==4,"Sai điểm khảo sát"); }},
                {"TC23",()=> { var dt=new ThongKeService().LuongHDV(9,2026); var expected=new Dictionary<string,decimal>{{"HDV01",10500000M},{"HDV02",11500000M},{"HDV03",10500000M}}; foreach(DataRow r in dt.Rows) Assert(Convert.ToDecimal(r["TongLuong"])==expected[Convert.ToString(r["MaHDV"])],"Sai lương HDV"); Assert(dt.Rows.Count==3,"Sai số HDV"); }},
                {"TC24",()=> { var dt=new ThongKeService().TongHop(new DateTime(2026,1,1),new DateTime(2026,10,1)); Assert(dt.Rows.Count==5,"Thiếu chỉ số"); var row=dt.Rows.Cast<DataRow>().Single(r=>Convert.ToString(r["ChiSo"])=="Đăng ký khách lẻ"); Assert(Convert.ToInt32(row["SoLuong"])==2 && Convert.ToDecimal(row["GiaTri"])==12500000M,"Sai tổng khách lẻ"); }},
                {"TC25",()=> { CreateTrip(); Assert(Count("SELECT COUNT(*) FROM ChuyenLe WHERE MaChuyen='CL004' AND TrangThai=N'Mở đăng ký'")==1,"Không lưu trạng thái mở"); }},
                {"TC26",()=> { CreateTrip(); Fail(Chuyen.ThemChuyen("CL004","T001",new DateTime(2026,12,15),"TP.HCM")); Assert(Count("SELECT COUNT(*) FROM ChuyenLe WHERE MaChuyen='CL004'")==1,"Đã tạo trùng chuyến"); }},
                {"TC27",()=> { Db.Execute("UPDATE Tour SET DangMoBan=0 WHERE MaTour='T001'"); Fail(Chuyen.ThemChuyen("CL004","T001",new DateTime(2026,12,15),"TP.HCM")); Assert(Count("SELECT COUNT(*) FROM ChuyenLe WHERE MaChuyen='CL004'")==0,"Tạo chuyến cho tour ngừng bán"); }},
                {"TC28",()=> { AssignToCancel(); Ok(Doan.HuyDangKy("DD002")); Assert(Money("SELECT TienCoc FROM DangKyDoan WHERE SoDKDoan='DD002'")==12000000M,"Cọc đã bị sửa"); Assert(Count("SELECT COUNT(*) FROM PhanCongHDV WHERE SoDKDoan='DD002'")==0,"Phân công chưa xóa"); }},
                {"TC29",()=> { Ok(Doan.HuyDangKy("DD002")); Fail(Doan.HuyDangKy("DD002")); Assert(Money("SELECT TienCoc FROM DangKyDoan WHERE SoDKDoan='DD002'")==12000000M,"Cọc thay đổi khi hủy lại"); }},
                {"TC30",()=> { Fail(Doan.HuyDangKy("DD001")); Assert(Count("SELECT COUNT(*) FROM PhanCongHDV WHERE SoDKDoan='DD001'")==2,"Phân công lịch sử bị xóa"); }},
                {"TC31",()=> { CreateTrip(); var psi=new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName,"--assert-persist"){UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true,CreateNoWindow=true}; using(var proc=Process.Start(psi)) { string log=proc.StandardOutput.ReadToEnd()+proc.StandardError.ReadToEnd(); proc.WaitForExit(); Assert(proc.ExitCode==0,"Tiến trình mới không đọc được CL004: "+log); } }},
                {"TC32",()=> { var config=ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None); var setting=config.ConnectionStrings.ConnectionStrings["QuanLyCongTyDuLichDB"]; string original=setting.ConnectionString; try { var b=new SqlConnectionStringBuilder(original); b.InitialCatalog="QuanLyCongTyDuLich_MISSING_"+Guid.NewGuid().ToString("N"); b.ConnectTimeout=3; setting.ConnectionString=b.ConnectionString; config.Save(ConfigurationSaveMode.Modified); ConfigurationManager.RefreshSection("connectionStrings"); bool rejected=false; try { new DanhMucService().LayPhuongTien(); } catch(SqlException) {rejected=true;} Assert(rejected,"Database sai không bị từ chối"); } finally { setting.ConnectionString=original; config.Save(ConfigurationSaveMode.Modified); ConfigurationManager.RefreshSection("connectionStrings"); } }}
            };
        }
        private static string Quote(string s) { return "\""+(s??"").Replace("\"","\"\"")+"\""; }
        private static string Arg(string[] args,string name,string fallback)
        {
            int i=Array.IndexOf(args,name); return i>=0 && i+1<args.Length ? args[i+1] : fallback;
        }
        private static int Main(string[] args)
        {
            Console.OutputEncoding=Encoding.UTF8;
            try
            {
                SafeDatabase();
                if(args.Contains("--assert-persist")) { Assert(Count("SELECT COUNT(*) FROM ChuyenLe WHERE MaChuyen='CL004'")==1,"CL004 không tồn tại"); return 0; }
                string output=Arg(args,"--output",Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"TestResults"));
                string selected=Arg(args,"--case",""); var cases=Cases();
                if(selected!=""&&!cases.ContainsKey(selected)) throw new Exception("Mã test không tồn tại: "+selected);
                Directory.CreateDirectory(output);string stamp=DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string csv=Path.Combine(output,"ServiceTests_"+stamp+".csv"),log=Path.Combine(output,"ServiceTests_"+stamp+".log");
                int failed=0;
                using(var writer=new StreamWriter(csv,false,new UTF8Encoding(true)))
                using(var logger=new StreamWriter(log,false,new UTF8Encoding(true)))
                {
                    writer.WriteLine("TC,PhamVi,KetQuaThucTe,DatKhongDat,NgayChay,AnhMinhChung");
                    var connection=new SqlConnectionStringBuilder(Db.ConnectionString);
                    logger.WriteLine("Server="+connection.DataSource+"; Database="+connection.InitialCatalog);
                    logger.WriteLine("Phạm vi: Service + SQL Server, không xác nhận tương tác Form hoặc ảnh UI.");
                    logger.WriteLine("Reset dữ liệu trước mỗi test. Ngày hiện tại="+DateTime.Today.ToString("yyyy-MM-dd")+"; DD003="+FutureDoan.ToString("yyyy-MM-dd"));
                    foreach(var pair in cases.OrderBy(x=>x.Key))
                    {
                        if(selected!=""&&selected!=pair.Key) continue;
                        string status="Đạt",detail="Các assertion Service/CSDL đạt.";
                        try { Reset(); pair.Value(); }
                        catch(Exception ex) { status="Không đạt";detail=ex.ToString();failed++; }
                        writer.WriteLine(string.Join(",",new[]{pair.Key,"Service và SQL Server",detail,status,DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),""}.Select(Quote)));
                        writer.Flush();logger.WriteLine(pair.Key+" "+status+"\n"+detail);logger.Flush();Console.WriteLine(pair.Key+" "+status);
                    }
                }
                Console.WriteLine("CSV: "+csv+"\nLog: "+log);
                return failed==0?0:1;
            }
            catch(Exception ex) { Console.Error.WriteLine(ex.ToString()); return 2; }
        }
    }
}
