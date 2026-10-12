using HtmlAgilityPack;
using Microsoft.Data.SqlClient;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Data;
using System.Security.Policy;
using System.Timers;

namespace WeatherDataService
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        static string pg1 = "https://www.ncei.noaa.gov/data/automated-surface-observing-system-one-minute-pg1/access/";
        static string pg2 = "https://www.ncei.noaa.gov/data/automated-surface-observing-system-one-minute-pg2/access/";
        static List<string> wx1 = new List<string>();
        static List<string> wx2 = new List<string>();
        static bool list1Complete = false;
        static bool list2Complete = false;

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                this.WindowState = FormWindowState.Maximized;
                int ht = 0;
                ht = this.Height - 650;
                tb1.Height = ht;                          


            }          
            catch (Exception ex)
            {
                tb2.Text += ex.Message + "\r\n";
                tb2.Text += ex.StackTrace + "\r\n";
            }
        }

        public List<string> GetwxLinks(string url)
        {
            List<string> wxData = new List<string>();
            try
            {

                var content = "";
                HttpClient client = new HttpClient();
                content = client.GetStringAsync(url).Result;

                var success = client.GetAsync(url).Result.IsSuccessStatusCode;
                if (success)
                {
                    var doc = new HtmlAgilityPack.HtmlDocument();
                    doc.LoadHtml(content);
                    var rows = doc.DocumentNode.SelectNodes("//tr[td]");
                    for (var i = 3; i < rows.Count; i++)
                    {
                        if (url.IndexOf("pg2") > 0)
                        {
                            wxData.Add(url + rows[i].ChildNodes[0].InnerText);
                        }
                        else
                        {
                            wxData.Add(url + rows[i].ChildNodes[0].InnerText);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                tb2.Text += ex.StackTrace?.ToString() + "\r\n";

            }

            return wxData;
        }




        private void UpdateSQLProdTables()
        {
            try
            {
                tb2.Text = "";

                list1Complete = ProcessPG1WX(wx1);
                bool list2Complte = ProcessPG2WX(wx2);

                if (list1Complete && list2Complete)
                {
                    SqlConnection conn5 = new SqlConnection(AppConfig.Require("WX_RAW_CONNECTION"));
                    SqlConnection conn6 = new SqlConnection(AppConfig.Require("WX_CLEAN_CONNECTION"));

                    SqlCommand cmd = new SqlCommand("MergePg1Pg2Location", conn5);
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 36000;
                    conn5.Open();
                    object result1 = cmd.ExecuteScalar();
                    if (result1 != null)
                    {
                        tb1.Text += "MergePg1Pg2Location stored procedure had this result:" + result1 + "\r\n";
                    }

                    SqlCommand cmd1 = new SqlCommand("UpdateGeoPoint", conn6);
                    cmd1.CommandType = CommandType.StoredProcedure;
                    cmd1.CommandTimeout = 3600;
                    conn6.Open();
                    object result2 = cmd1.ExecuteScalar();
                    if (result2 != null)
                    {
                        tb1.Text += "MergePg1Pg2Location stored procedure had this result:" + result2 + "\r\n";
                    }

                }

            }
            catch (Exception ex)
            {
                tb2.Text += ex.Message + "\r\n";
                tb2.Text += ex.StackTrace + "\r\n";
            }

        }

        private bool ProcessPG1WX(List<string> urlList)
        {
            DataTable dt1 = new DataTable();
            dt1.Columns.Add(new DataColumn("WBAN")); //0
            dt1.Columns.Add(new DataColumn("ICAO"));  //1            
            dt1.Columns.Add(new DataColumn("Callsign"));//2
            dt1.Columns.Add(new DataColumn("Year"));//3
            dt1.Columns.Add(new DataColumn("Month"));//4
            dt1.Columns.Add(new DataColumn("Day"));//5
            dt1.Columns.Add(new DataColumn("LocalTime"));//6
            dt1.Columns.Add(new DataColumn("UTC"));//7            
            dt1.Columns.Add(new DataColumn("VEC"));//8           
            dt1.Columns.Add(new DataColumn("[Day/Night]"));//9          
            dt1.Columns.Add(new DataColumn("[2minuteWindDir]"));//10            
            dt1.Columns.Add(new DataColumn("[2minuteWindSpd]"));//11            
            dt1.Columns.Add(new DataColumn("[5minuteWindDir]"));//12          
            dt1.Columns.Add(new DataColumn("[5minuteWindSpd]"));//13          
            dt1.Columns.Add(new DataColumn("ActiveRWY"));//14        
            dt1.Columns.Add(new DataColumn("RVR"));//15

            int rowcount = 0;


            foreach (string url in urlList)
            {
                rowcount = 0;
                var success = false;

                dt1.Clear();

                var content = "";
                try
                {
                    HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(4) };

                    content = client.GetStringAsync(url).Result;

                    success = client.GetAsync(url).Result.IsSuccessStatusCode;

                }
                catch (Exception ex)
                {
                    tb2.Text += ex.Message + "\r\n";
                    tb2.Text += ex.StackTrace + "\r\n";
                }

                if (success)
                {
                    StringReader reader = new StringReader(content);
                    TextFieldParser parser = new TextFieldParser(reader);
                    parser.TrimWhiteSpace = false;
                    parser.HasFieldsEnclosedInQuotes = false;
                    parser.TextFieldType = FieldType.FixedWidth;
                    //                    0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24
                    parser.SetFieldWidths(5, 4, 1, 3, 4, 2, 2, 4, 4, 4, 5, 2, 1, 30, 3, 3, 2, 2, 3, 3, 2, 3, 2, 1, 3);

                    while (!parser.EndOfData)
                    {
                        var fields = parser.ReadFields();

                        if (fields?.Length < 25)
                        {
                            Array.Resize(ref fields, 25);
                        }

                        for (int i = 0; i < fields.Length; i++)
                        {
                            if (fields[i] == null)
                                fields[i] = string.Empty;
                        }

                        DataRow dr = dt1.NewRow();
                        dr[0] = fields[0];
                        dr[1] = fields[1];
                        dr[2] = fields[3];
                        dr[3] = fields[4];
                        dr[4] = fields[5];
                        dr[5] = fields[6];
                        dr[6] = fields[7];
                        dr[7] = fields[8];
                        dr[8] = fields[10];
                        dr[9] = fields[12];
                        dr[10] = fields[14];
                        dr[11] = fields[16];
                        dr[12] = fields[18];
                        dr[13] = fields[20];
                        dr[14] = fields[22];
                        dr[15] = fields[24];
                        dt1.Rows.Add(dr);
                    }//parser adding rows to dt1                 

                    reader.Close();

                    string createTempTableQuery = "CREATE TABLE #TempTable (";
                    createTempTableQuery += "[WBAN] varchar(6),[ICAO] varchar(5),[Callsign] varchar(4),[Year] int,[Month] int,[Day] int,[LocalTime] int";
                    createTempTableQuery += ",[UTC] int,[VEC] varchar(5),[Day-Night] varchar(5),[2minuteWindDir] varchar(4),[2minuteWindSpd] varchar(3),";
                    createTempTableQuery += "[5minuteWindDir] varchar(4),[5minuteWindSpd] varchar(3),[ActiveRWY] varchar(3),[RVR] varchar(4))";

                    SqlConnection conn = new SqlConnection();
                    conn.ConnectionString = new SqlConnectionStringBuilder(AppConfig.Require("WX_RAW_CONNECTION")) { ConnectTimeout = 600 }.ConnectionString;
                    conn.Open();


                    try
                    {
                        SqlCommand cmd = new SqlCommand(createTempTableQuery, conn);
                        cmd.CommandTimeout = 1200;
                        cmd.ExecuteNonQuery();
                        cmd.Dispose();
                    }
                    catch (Exception ex)
                    {
                        tb2.Text += ex.Message + "\r\n";
                        tb2.Text = ex.StackTrace + "\r\n";
                    }
                    try
                    {
                        SqlBulkCopy bc = new SqlBulkCopy(conn);
                        bc.DestinationTableName = "#TempTable";
                        bc.WriteToServer(dt1);
                        bc.Close();
                    }
                    catch (Exception ex)
                    {
                        tb2.Text += ex.Message + "\r\n";
                        tb2.Text = ex.StackTrace + "\r\n";
                    }


                    string checkQuery = "SELECT t.WBAN, t.ICAO, t.callsign, t.Year, t.Month, t.Day, t.LocalTime, t.UTC, t.VEC, t.[Day-Night], t.[2minuteWindDir], t.[2minuteWindSpd], ";
                    checkQuery += "t.[5minuteWindDir], t.[5minuteWindSpd], t.ActiveRWY, t.RVR ";
                    checkQuery += "FROM #TempTable t";
                    checkQuery += " WHERE NOT EXISTS (SELECT 1 FROM WX_RAW.dbo.WXP1 y WHERE t.ICAO = y.ICAO AND t.[YEAR] = y.[YEAR] AND ";
                    checkQuery += " t.Month = y.Month AND t.Day = y.Day AND t.LocalTime = y.LocalTime)";
                    DataTable dt3 = new DataTable();
                    try
                    {

                        SqlCommand cmd1 = new SqlCommand(checkQuery, conn);

                        SqlDataReader reader1 = cmd1.ExecuteReader();

                        if (reader1.HasRows)
                        {
                            dt3.Columns.Add(new DataColumn("WBAN")); //0
                            dt3.Columns.Add(new DataColumn("ICAO"));  //1            
                            dt3.Columns.Add(new DataColumn("Callsign"));//2
                            dt3.Columns.Add(new DataColumn("Year"));//3
                            dt3.Columns.Add(new DataColumn("Month"));//4
                            dt3.Columns.Add(new DataColumn("Day"));//5
                            dt3.Columns.Add(new DataColumn("LocalTime"));//6
                            dt3.Columns.Add(new DataColumn("UTC"));//7            
                            dt3.Columns.Add(new DataColumn("VEC"));//8           
                            dt3.Columns.Add(new DataColumn("[Day/Night]"));//9          
                            dt3.Columns.Add(new DataColumn("[2minuteWindDir]"));//10            
                            dt3.Columns.Add(new DataColumn("[2minuteWindSpd]"));//11            
                            dt3.Columns.Add(new DataColumn("[5minuteWindDir]"));//12          
                            dt3.Columns.Add(new DataColumn("[5minuteWindSpd]"));//13          
                            dt3.Columns.Add(new DataColumn("ActiveRWY"));//14        
                            dt3.Columns.Add(new DataColumn("RVR"));//15

                            while (reader1.Read())
                            {
                                DataRow dr3 = dt3.NewRow();
                                dr3[0] = reader1[0];
                                dr3[1] = reader1[1];
                                dr3[2] = reader1[2];
                                dr3[3] = reader1[3];
                                dr3[4] = reader1[4];
                                dr3[5] = reader1[5];
                                dr3[6] = reader1[6];
                                dr3[7] = reader1[7];
                                dr3[8] = reader1[8];
                                dr3[9] = reader1[9];
                                dr3[10] = reader1[10];
                                dr3[11] = reader1[11];
                                dr3[12] = reader1[12];
                                dr3[13] = reader1[13];
                                dr3[14] = reader1[14];
                                dr3[15] = reader1[15];
                                dt3.Rows.Add(dr3);
                                rowcount++;

                            }//while reader.read
                        }
                        reader1.Close();
                        cmd1.Dispose();

                    }
                    catch (Exception ex)
                    {
                        tb2.Text += ex.Message + "\r\n";
                        tb2.Text += ex.StackTrace + "\n\r";

                    }

                    try
                    {
                        SqlBulkCopy bc1 = new SqlBulkCopy(conn);
                        bc1.DestinationTableName = "WXP1";
                        bc1.WriteToServer(dt3);
                        bc1.Close();
                    }
                    catch (Exception ex)
                    {
                        tb2.Text = ex.Message + "\r\n";
                        tb2.Text += ex.StackTrace + "\r\n";
                    }

                    try
                    {
                        string dropTempTableQuery = "DROP TABLE #TempTable;";
                        SqlCommand cmd2 = new SqlCommand(dropTempTableQuery, conn);
                        cmd2.CommandTimeout = 1200;
                        cmd2.ExecuteNonQuery();
                        cmd2.Dispose();
                    }
                    catch (Exception ex2)
                    {
                        tb2.Text += ex2.Message + "\r\n";
                        tb2.Text += ex2.StackTrace + "\r\n";
                    }

                }//success getting text file

                tb1.Text += url + " " + rowcount + " records added. \r\n";


            }// foreach url
            return true;


        }

        private bool ProcessPG2WX(List<string> urlList)
        {

            SqlConnection conn = new SqlConnection();
            conn.ConnectionString = AppConfig.Require("WX_RAW_CONNECTION");
            conn.Open();
            SqlConnection conn1 = new SqlConnection();
            conn1.ConnectionString = AppConfig.Require("WX_RAW_CONNECTION");
            conn1.Open();
            int rowcount = 0;
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("WBAN")); //0
            dt.Columns.Add(new DataColumn("ICAO"));  //1            
            dt.Columns.Add(new DataColumn("Callsign"));//2
            dt.Columns.Add(new DataColumn("Year"));//3
            dt.Columns.Add(new DataColumn("Month"));//4
            dt.Columns.Add(new DataColumn("Day"));//5
            dt.Columns.Add(new DataColumn("LocalTime"));//6
            dt.Columns.Add(new DataColumn("UTC"));//7            
            dt.Columns.Add(new DataColumn("[PrecipType+Intensity]"));//8    
            dt.Columns.Add(new DataColumn("Lightning"));//9            
            dt.Columns.Add(new DataColumn("PrecipAmtPerMinute"));//10            
            dt.Columns.Add(new DataColumn("FrzPrecipFreq"));//11          
            dt.Columns.Add(new DataColumn("Baro1"));//12          
            dt.Columns.Add(new DataColumn("Baro2"));//13        
            dt.Columns.Add(new DataColumn("Baro3"));//14
            dt.Columns.Add(new DataColumn("Temp"));//15
            dt.Columns.Add(new DataColumn("Dewpoint"));//16

            var success = false;

            foreach (string url in urlList)
            {
                rowcount = 0;
                dt.Clear();
                var content = "";
                try
                {
                    HttpClient client = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };

                    content = client.GetStringAsync(url).Result;

                    success = client.GetAsync(url).Result.IsSuccessStatusCode;
                }
                catch (Exception ex)
                {
                    tb2.Text += ex.Message + "\r\n";
                    tb2.Text += ex.StackTrace + "\r\n";
                }

                try
                {

                    if (success)
                    {
                        StringReader reader = new StringReader(content);

                        TextFieldParser parser1 = new TextFieldParser(reader);
                        parser1.TrimWhiteSpace = false;
                        parser1.HasFieldsEnclosedInQuotes = false;
                        parser1.TextFieldType = FieldType.FixedWidth;
                        //                                          00,  01, 02,03,04,05, 06,07,08,09,10,11,12,13,14,15,16, 17,18,19,20,21,22,23,24,25,26
                        parser1.SetFieldWidths(5, 4, 1, 3, 4, 2, 2, 4, 4, 3, 2, 2, 4, 4, 4, 14, 5, 3, 6, 2, 6, 2, 6, 3, 2, 3, 2);

                        while (!parser1.EndOfData)
                        {
                            var fields = parser1.ReadFields();
                            //The mismatch below between column numbers and field number is because the document has 27 fields but we only keep 17 of them

                            DataRow dr = dt.NewRow();
                            dr[0] = fields[0];
                            dr[1] = fields[1];
                            dr[2] = fields[3];
                            dr[3] = fields[4];
                            dr[4] = fields[5];
                            dr[5] = fields[6];
                            dr[6] = fields[7];
                            dr[7] = fields[8];
                            dr[8] = fields[10];
                            dr[9] = fields[12];
                            dr[10] = fields[14];
                            dr[11] = fields[16];
                            dr[12] = fields[18];
                            dr[13] = fields[20];
                            dr[14] = fields[22];
                            dr[15] = fields[24];
                            dr[16] = fields[26];
                            dt.Rows.Add(dr);
                        }
                        reader.Close();

                        string createTempTableQuery = "CREATE TABLE #TempTable (";
                        createTempTableQuery += "[WBAN] varchar(6),[ICAO] varchar(5),[Callsign] varchar(4),[Year] int,[Month] int,[Day] int,[LocalTime] int";
                        createTempTableQuery += ",[UTC] int,[PrecipType+Intensity] varchar(3),[Lightning] varchar(5),[PrecipAmtPerMinute] varchar(5),[FrzPrecipFreq] varchar(6),";
                        createTempTableQuery += "[Baro1] varchar(7),[Baro2] varchar(7),[Baro3] varchar(7),[Temp] varchar(2),[Dewpoint] varchar(2))";

                        if (conn.State != ConnectionState.Open)
                        {
                            conn.Open();
                        }
                        using (SqlCommand cmd = new SqlCommand(createTempTableQuery, conn))
                        {
                            cmd.CommandTimeout = 600;
                            cmd.ExecuteNonQuery();
                        }


                        using (SqlBulkCopy bc = new SqlBulkCopy(conn))
                        {
                            bc.DestinationTableName = "#TempTable";
                            bc.WriteToServer(dt);
                        }



                    }
                }
                catch (Exception ex)
                {
                    tb2.Text += ex.Message + "\r\n";
                    tb2.Text += ex.StackTrace + "\r\n";
                }

                try
                {
                    string checkQuery = "SELECT t.WBAN, t.ICAO, t.Callsign, t.Year, t.Month, t.Day, t.LocalTime, t.UTC, t.[PrecipType+Intensity], t.Lightning, t.PrecipAmtPerMinute, t.FrzPrecipFreq, ";
                    checkQuery += "t.Baro1, t.Baro2, t.Baro3, t.Temp, t.Dewpoint ";
                    checkQuery += "FROM #TempTable t";
                    checkQuery += " WHERE NOT EXISTS (SELECT 1 FROM dbo.WXP2 y WHERE t.ICAO = y.ICAO AND t.[YEAR] = y.[YEAR] AND ";
                    checkQuery += " t.Month = y.Month AND t.Day = y.Day AND t.LocalTime = y.LocalTime)";
                    if (conn.State != ConnectionState.Open)
                    {
                        conn.Open();
                    }
                    using (SqlCommand cmd = new SqlCommand(checkQuery, conn))
                    {
                        cmd.CommandTimeout = 600;
                        using (SqlDataReader reader1 = cmd.ExecuteReader())
                        {
                            if (reader1.HasRows)
                            {
                                DataTable dt4 = new DataTable();
                                dt4.Columns.Add(new DataColumn("WBAN")); //0
                                dt4.Columns.Add(new DataColumn("ICAO"));  //1            
                                dt4.Columns.Add(new DataColumn("Callsign"));//2
                                dt4.Columns.Add(new DataColumn("Year"));//3
                                dt4.Columns.Add(new DataColumn("Month"));//4
                                dt4.Columns.Add(new DataColumn("Day"));//5
                                dt4.Columns.Add(new DataColumn("LocalTime"));//6
                                dt4.Columns.Add(new DataColumn("UTC"));//7            
                                dt4.Columns.Add(new DataColumn("[PrecipType+Intensity]"));//8    
                                dt4.Columns.Add(new DataColumn("Lightning"));//9            
                                dt4.Columns.Add(new DataColumn("PrecipAmtPerMinute"));//10            
                                dt4.Columns.Add(new DataColumn("FrzPrecipFreq"));//11          
                                dt4.Columns.Add(new DataColumn("Baro1"));//12          
                                dt4.Columns.Add(new DataColumn("Baro2"));//13        
                                dt4.Columns.Add(new DataColumn("Baro3"));//14
                                dt4.Columns.Add(new DataColumn("Temp"));//15
                                dt4.Columns.Add(new DataColumn("Dewpoint"));//16

                                while (reader1.Read())
                                {
                                    DataRow dr3 = dt4.NewRow();
                                    dr3[0] = reader1[0];
                                    dr3[1] = reader1[1];
                                    dr3[2] = reader1[2];
                                    dr3[3] = reader1[3];
                                    dr3[4] = reader1[4];
                                    dr3[5] = reader1[5];
                                    dr3[6] = reader1[6];
                                    dr3[7] = reader1[7];
                                    dr3[8] = reader1[8];
                                    dr3[9] = reader1[9];
                                    dr3[10] = reader1[10];
                                    dr3[11] = reader1[11];
                                    dr3[12] = reader1[12];
                                    dr3[13] = reader1[13];
                                    dr3[14] = reader1[14];
                                    dr3[15] = reader1[15];
                                    dr3[16] = reader1[16];
                                    dt4.Rows.Add(dr3);
                                    rowcount++;

                                }
                                if (conn.State != ConnectionState.Open)
                                {
                                    conn.Open();
                                }
                                using (SqlBulkCopy bc1 = new SqlBulkCopy(conn1))
                                {
                                    bc1.DestinationTableName = "WXP2";
                                    bc1.WriteToServer(dt4);
                                }

                            }
                            reader1.Close();
                        }
                    }

                    string dropTempTableQuery = "DROP TABLE #TempTable;";
                    using (SqlCommand cmd = new SqlCommand(dropTempTableQuery, conn))
                    {
                        cmd.CommandTimeout = 600;
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                   
                }
                catch (Exception ex)
                {
                    tb2.Text += ex.Message + "\r\n";
                    tb2.Text += ex.StackTrace + "\r\n";
                }
                tb1.Text += url + " added " + rowcount + " new rows!\r\n";
            }
            return true;
        }
           
            

      

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {

                DateTime dt2 = cal1.SelectionStart .Date;
                string curYear = dt2.Year.ToString(); //"2025"; //                           
                string curMonth = dt2.Month.ToString();
                if (curMonth.Length < 2) { curMonth = "0" + curMonth; }
                string urlDate = curYear.ToString() + "/" + curMonth + "/";

                string wxURL1 = pg1 + urlDate;
                string wxURL2 = pg2 + urlDate;

                wx1 = GetwxLinks(wxURL1);
                wx2 = GetwxLinks(wxURL2);

                if (wx1.Count > 0)
                {
                    list1Complete = ProcessPG1WX(wx1);
                }

                if (wx2.Count > 0)
                {
                    list2Complete = ProcessPG2WX(wx2);
                }

                if (list1Complete && list2Complete)
                {
                    UpdateSQLProdTables();
                }
            }
            catch (Exception ex)
            {
                tb2.AppendText(ex.StackTrace);
            }
        }
    }
}
