using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using QuanLyKhachSan.Data;
namespace QuanLyKhachSan.Forms {
 public class BaseForm:Form {
  protected readonly FlowLayoutPanel Top=new FlowLayoutPanel();
  protected readonly DataGridView Grid=new DataGridView();
  protected readonly Dictionary<string,TextBox> Fields=new Dictionary<string,TextBox>();
  protected BaseForm(string title,string query) {
   Text=title; Width=1050; Height=690; StartPosition=FormStartPosition.CenterParent;
   Top.Dock=DockStyle.Top; Top.Height=170; Top.AutoScroll=true; Top.Padding=new Padding(10); Controls.Add(Top);
   Grid.Dock=DockStyle.Fill; Grid.ReadOnly=true; Grid.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells; Grid.AllowUserToAddRows=false; Controls.Add(Grid);
   RefreshQuery=query; Load+=(s,e)=>RefreshData();
  }
  protected string RefreshQuery;
  protected void Field(string id,string label,string initial="") { var p=new FlowLayoutPanel {Width=215,Height=48}; p.Controls.Add(new Label{Text=label,Width=200,Height=18}); var t=new TextBox{Width=190,Text=initial}; Fields[id]=t; p.Controls.Add(t); Top.Controls.Add(p); }
  protected string V(string id) { return Fields[id].Text.Trim(); }
  protected void Button(string caption,Action action,bool refresh=true) { var b=new Button{Text=caption,AutoSize=true,Height=40,Margin=new Padding(8)}; b.Click+=(s,e)=>Safe(action,refresh); Top.Controls.Add(b); }
  protected void Safe(Action a,bool refresh) { try { a(); if(refresh) { RefreshData(); MessageBox.Show("Thành công"); } } catch(Exception ex) { MessageBox.Show(ex.Message,"Không thể thực hiện",MessageBoxButtons.OK,MessageBoxIcon.Warning); } }
  protected void RefreshData() { try { Grid.DataSource=Db.Query(RefreshQuery); } catch(Exception ex) { MessageBox.Show("Không thể tải dữ liệu: "+ex.Message); } }
  protected void Exec(string proc,params SqlParameter[] ps) { Db.Run(proc,ps); }
  protected SqlParameter P(string n,object v) { return Db.P(n,v); }
  protected int I(string id) { return int.Parse(V(id)); }
  protected decimal D(string id) { return decimal.Parse(V(id)); }
  protected DateTime Day(string id) { return DateTime.Parse(V(id)); }
 }
}
