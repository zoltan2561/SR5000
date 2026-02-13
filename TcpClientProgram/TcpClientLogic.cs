using MySql.Data.MySqlClient;
using System;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TcpClientProgram;
using Microsoft.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.IO;
using NLog;
using System.Collections.Generic;

public class TcpClientLogic : IDisposable
{
    private TcpClient tcpClient;
    private DesignForm designForm;
    private MySqlConnection dbConnection;
    private NetworkStream clientStream;
    private bool isListening = false;
    private CancellationTokenSource cancellationTokenSource;
    private string mysqlMessage;
    private int qty;
    private string ip;
    private int port;
    private bool disposed = false;
    private readonly object exportLock = new object();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public NetworkStream ClientStream { get => clientStream; set => clientStream = value; }

    public int Qty { get => qty; set => qty = value; }

    public int Port { get => port; set => port = value; }

    public string Ip { get => ip; set => ip = value; }

    public TcpClientLogic(DesignForm form)
    {
        designForm = form;
    }

    public async Task StartClientAsync()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Attempting tcp connection ({ip}:{port})")));

        try
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            clientStream = tcpClient.GetStream();

            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Connected to reader ({ip}:{port})", System.Drawing.Color.Green)));

            cancellationTokenSource = new CancellationTokenSource();
            await ListenForMessages(cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error connecting to server:{ex.StackTrace} {ex.Message} {ex.InnerException}", System.Drawing.Color.Red)));
        }
    }

    private async Task ListenForMessages(CancellationToken cancellationToken)
    {
        isListening = true;

        try
        {
            while (!cancellationToken.IsCancellationRequested && clientStream != null && !designForm.disconnect)
            {
                byte[] message = new byte[4096];
                int bytesRead = await clientStream.ReadAsync(message, 0, 4096, cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                string receivedMessage = Encoding.UTF8.GetString(message, 0, bytesRead);

                if (designForm.IsHandleCreated)
                {
                    //Log(receivedMessage);
                    Logger.Info(receivedMessage);
                    designForm.Invoke(new Action(() => designForm.DisplayMessage(receivedMessage)));
                }
            }
        }
        catch (ObjectDisposedException e)
        {
            Logger.Info("Disconnect");
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Disconnected", System.Drawing.Color.Black)));
        }
        catch (Exception ex)
        {
            //Log(ex.Message + "\n" + ex.StackTrace + "\n" + ex.InnerException);
            Logger.Error(ex);
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error reading from server: {ex.Message}", System.Drawing.Color.Red)));
        }
        finally
        {
            isListening = false;
            if (clientStream != null)
            {
                clientStream.Close();
            }
            if (tcpClient != null)
            {
                tcpClient.Close();
            }
        }
    }

    public void SendMessage(string message)
    {
    byte carriageReturn = 13; // ASCII code for carriage return

    byte[] messageBytes = Encoding.ASCII.GetBytes($"{message}{(char)carriageReturn}");

        if (clientStream != null && clientStream.CanWrite)
        {
            try
            {
                clientStream.Write(messageBytes, 0, messageBytes.Length);
                //Log($"Sent:{message}[CR]");
                Logger.Info($"Sent:{message}[CR]");
                designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Sent:{message}[CR]", System.Drawing.Color.Blue)));

                // Add logging for responses
                if(message == "LOFF")
                {
                    byte[] responseBytes = new byte[4096];
                    int responseBytesRead = clientStream.Read(responseBytes, 0, 4096);
                    string receivedResponse = Encoding.UTF8.GetString(responseBytes, 0, responseBytesRead);
                    qty = CountOccurrences(receivedResponse, "$");
                    string pattern = ";01";
                    mysqlMessage = receivedResponse;
                    //MessageBox.Show(receivedResponse);
                    //Log(receivedResponse);
                    Logger.Info(receivedResponse);
                    //designForm.Invoke(new Action(() => designForm.DisplayMessage($"\n{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Received response from server: \n{receivedResponse}")));
                    designForm.Invoke(new Action(() => designForm.DisplayMessage($"\n{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Received response from server: \n{Regex.Replace(receivedResponse.Replace('$','\n').Replace('$','\n'),pattern,"")}")));
                    designForm.Invoke(new Action(() => designForm.DisplayMessage($"\n{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Total read barcode QTY: {qty}")));
                    ExportScanOutput(receivedResponse);
                }

            }
            catch (Exception e)
            {
                //Log(e.Message + "\n" + e.StackTrace + "\n" + e.InnerException);
                Logger.Error(e);
                designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Data sending error:{e.StackTrace} {e.Message} {e.InnerException}", System.Drawing.Color.Red)));
            }
        }
        else
        {
            //Log("clientStream cannot write");
            Logger.Error("clientStream cannot write");
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} clientStream cannot write", System.Drawing.Color.Red)));
        }
    }

    public void StopClient()
    {
        if (this.cancellationTokenSource != null)
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
        }
        if (this.clientStream != null)
        {
            this.clientStream.Dispose();
        }
        try
        {
            isListening = false;
            //cancellationTokenSource?.Cancel();
            if (tcpClient != null)
            {
                tcpClient.Close();
            }
        }
        catch(Exception ex)
        {
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} disconnected", System.Drawing.Color.Red)));
        }
    }

    public bool ConnectToMysql()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
        //Log("Attempting mysql connection");
        Logger.Info("Attempting mysql connection");
        designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Attempting mysql connection")));
        dbConnection = new MySqlConnection("server=db3;user id=scripts;password=hmhuscripts;database=keyence;");

        try
        {
            dbConnection.Open();
            //Log("Connected to mysql");
            Logger.Info("Connected to mysql");
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Connected to mysql", System.Drawing.Color.Green)));
            UploadToMysql(mysqlMessage);
            return true;
        }
        catch (MySqlException ex)
        {
            //Log(ex.Message + "\n" + ex.StackTrace + "\n" + ex.InnerException);
            Logger.Error(ex);
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error connecting to mysql database:{ex.StackTrace} {ex.Message} {ex.InnerException}", System.Drawing.Color.Red)));
            return false;
        }
    }

    public void CloseMysqlConnection()
    {
        dbConnection.Close();
    }

    public void UploadToMysql(string message)
    {
        List<ReaderScanRecord> rows = ParseReaderResponse(message);

        try
        {
            using (dbConnection)
            {
                foreach (ReaderScanRecord row in rows)
                {
                    string sql = "INSERT INTO test (code, image, created_at) VALUES (@code, @image, NOW())";

                    using (MySqlCommand cmd = new MySqlCommand(sql, dbConnection))
                    {
                        cmd.Parameters.AddWithValue("@code", row.Code);
                        cmd.Parameters.AddWithValue("@image", row.Image);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            //Log("Mysql upload OK");
            Logger.Info("Mysql upload OK");
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Uploaded message to mysql database", System.Drawing.Color.Green)));
        }
        catch(Exception ex)
        {
            Logger.Error(ex);
            //Log(ex.Message + "\n" + ex.StackTrace + "\n" + ex.InnerException);
            designForm.Invoke(new Action(() => designForm.DisplayMessage($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error uploading to mysql database:{ex.StackTrace} {ex.Message} {ex.InnerException}", System.Drawing.Color.Red)));
        }

    }

    public void Log(string message)
    {
        /*string today = DateTime.Now.ToString("yyyyMMdd");
        if (message.StartsWith(DateTime.Now.ToString("yyyy-mm-dd")))
        {
            File.AppendAllText($".\\log\\{today}.log", message + Environment.NewLine);
        }
        else
        {
            File.AppendAllText($".\\log\\{today}.log", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message + Environment.NewLine);
        }*/
        

    }

    public void GetIp()
    {
        String line;
        try
        {
            StreamReader sr = new StreamReader("settings.ini");
            line = sr.ReadLine();
            while (line != null)
            {
                if (line.StartsWith("ip"))
                {
                    this.ip = line.Split('=')[1];
                    sr.Close();
                    return;
                }
                line = sr.ReadLine();
            }
            sr.Close();
        }
        catch (Exception ex)
        {
            //
        }
    }

    public void GetPort()
    {
        String line;
        try
        {
            StreamReader sr = new StreamReader("settings.ini");
            line = sr.ReadLine();
            while (line != null)
            {
                if (line.StartsWith("port"))
                {
                    this.port = Int32.Parse(line.Split('=')[1]);
                    sr.Close();
                    return;
                }
                line = sr.ReadLine();
            }
            sr.Close();
        }
        catch (Exception ex)
        {
            //
        }
    }

    private int CountOccurrences(string input, string substring)
    {
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(substring))
        {
            return 0;
        }

        int count = 0;
        int index = 0;

        while ((index = input.IndexOf(substring, index)) != -1)
        {
            count++;
            index += substring.Length; // Move past the substring
        }

        return count;
    }

    private void ExportScanOutput(string response)
    {
        List<ReaderScanRecord> rows = ParseReaderResponse(response);
        if (rows.Count == 0)
        {
            return;
        }

        string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
        string filePrefix = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        lock (exportLock)
        {
            Directory.CreateDirectory(exportFolder);

            string txtPath = Path.Combine(exportFolder, filePrefix + ".txt");
            string csvPath = Path.Combine(exportFolder, filePrefix + ".csv");
            string jsonPath = Path.Combine(exportFolder, filePrefix + ".json");

            File.WriteAllText(txtPath, response);
            File.WriteAllLines(csvPath, BuildCsvLines(rows));
            File.WriteAllText(jsonPath, BuildJson(rows));

            designForm.Invoke(new Action(() => designForm.DisplayMessage(
                string.Format("{0} Exported scan output: {1}, {2}, {3}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Path.GetFileName(txtPath),
                Path.GetFileName(csvPath),
                Path.GetFileName(jsonPath)),
                System.Drawing.Color.DarkGreen)));
        }
    }

    private List<ReaderScanRecord> ParseReaderResponse(string response)
    {
        List<ReaderScanRecord> result = new List<ReaderScanRecord>();
        if (string.IsNullOrWhiteSpace(response))
        {
            return result;
        }

        string[] rows = response.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawRow in rows)
        {
            string row = rawRow.Trim();
            if (string.IsNullOrEmpty(row))
            {
                continue;
            }

            string[] values = row.Split(';');
            if (values.Length == 0)
            {
                continue;
            }

            string code = values[0].Trim();
            string image = values.Length > 1 ? values[1].Trim() : string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                continue;
            }

            result.Add(new ReaderScanRecord
            {
                Code = code,
                Image = image,
                Timestamp = DateTime.Now
            });
        }

        return result;
    }

    private IEnumerable<string> BuildCsvLines(IEnumerable<ReaderScanRecord> rows)
    {
        List<string> lines = new List<string>();
        lines.Add("code;image;timestamp");

        foreach (ReaderScanRecord row in rows)
        {
            lines.Add(string.Format("{0};{1};{2}",
                EscapeCsvValue(row.Code),
                EscapeCsvValue(row.Image),
                row.Timestamp.ToString("o")));
        }

        return lines;
    }

    private string EscapeCsvValue(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        if (value.Contains(";") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        return value;
    }

    private string BuildJson(IEnumerable<ReaderScanRecord> rows)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("[");
        bool first = true;

        foreach (ReaderScanRecord row in rows)
        {
            if (!first)
            {
                sb.AppendLine(",");
            }

            sb.Append("  {\"code\":\"").Append(JsonEscape(row.Code))
              .Append("\",\"image\":\"").Append(JsonEscape(row.Image))
              .Append("\",\"timestamp\":\"").Append(row.Timestamp.ToString("o"))
              .Append("\"}");
            first = false;
        }

        sb.AppendLine();
        sb.Append("]");
        return sb.ToString();
    }

    private string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private class ReaderScanRecord
    {
        public string Code { get; set; }
        public string Image { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this); // Megakadályozza a finalize futását
    }

    // Ez a metódus végzi az erőforrások tényleges felszabadítását
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Itt szabadítjuk fel a managed erőforrásokat (pl. más IDisposable objektumokat)
                // Példa:
                // if (managedResource != null)
                // {
                //     managedResource.Dispose();
                //     managedResource = null;
                // }
            }

            // Itt szabadítjuk fel az unmanaged erőforrásokat (ha vannak)
            // Példa:
            // if (unmanagedResource != IntPtr.Zero)
            // {
            //     Marshal.FreeHGlobal(unmanagedResource);
            //     unmanagedResource = IntPtr.Zero;
            // }

            disposed = true; // Az objektum most már dispose-olva van
        }
    }

    // Destruktor (finalizer), ami akkor fut le, ha az objektumot a GC felszabadítja
    ~TcpClientLogic()
    {
        Dispose(false);
    }

    // Nyilvános metódus, amely ellenőrzi, hogy az objektum dispose-olva van-e
    public bool IsDisposed()
    {
        return disposed;
    }
}
