using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using TcpClientProgram;

public class TcpClientLogic : IDisposable
{
    private TcpClient tcpClient;
    private DesignForm designForm;
    private MySqlConnection dbConnection;
    private NetworkStream clientStream;
    private CancellationTokenSource cancellationTokenSource;
    private string mysqlMessage;
    private int qty;
    private string ip;
    private int port;
    private bool disposed = false;
    private readonly object exportLock = new object();
    private readonly object messageLock = new object();
    private readonly StringBuilder receiveBuffer = new StringBuilder();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public NetworkStream ClientStream { get { return clientStream; } set { clientStream = value; } }
    public int Qty { get { return qty; } set { qty = value; } }
    public int Port { get { return port; } set { port = value; } }
    public string Ip { get { return ip; } set { ip = value; } }

    public TcpClientLogic(DesignForm form)
    {
        designForm = form;
    }

    public async Task StartClientAsync()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Attempting tcp connection ({1}:{2})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port))));

        try
        {
            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            clientStream = tcpClient.GetStream();

            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Connected to reader ({1}:{2})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port), System.Drawing.Color.Green)));

            cancellationTokenSource = new CancellationTokenSource();
            await ListenForMessages(cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Error connecting to server: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
        }
    }

    private async Task ListenForMessages(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && clientStream != null && !designForm.disconnect)
            {
                byte[] message = new byte[4096];
                int bytesRead = await clientStream.ReadAsync(message, 0, message.Length, cancellationToken);

                if (bytesRead == 0)
                {
                    break;
                }

                string receivedMessage = Encoding.UTF8.GetString(message, 0, bytesRead);

                if (designForm.IsHandleCreated)
                {
                    Logger.Info(receivedMessage);
                    designForm.Invoke(new Action(() => designForm.DisplayMessage(receivedMessage)));
                }

                ProcessIncomingData(receivedMessage);
            }
        }
        catch (ObjectDisposedException)
        {
            Logger.Info("Disconnect");
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Read loop cancelled.");
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Error reading from server: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
        }
        finally
        {
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Disconnected", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Black)));
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

    private void ProcessIncomingData(string incoming)
    {
        List<ReaderScanRecord> extractedRows = new List<ReaderScanRecord>();

        lock (messageLock)
        {
            receiveBuffer.Append(incoming);

            while (true)
            {
                string bufferText = receiveBuffer.ToString();
                int rowEnd = bufferText.IndexOf('$');
                if (rowEnd < 0)
                {
                    break;
                }

                string oneRow = bufferText.Substring(0, rowEnd).Trim();
                receiveBuffer.Remove(0, rowEnd + 1);

                if (string.IsNullOrWhiteSpace(oneRow))
                {
                    continue;
                }

                ReaderScanRecord parsed = ParseSingleRow(oneRow);
                if (parsed != null)
                {
                    extractedRows.Add(parsed);
                }
            }
        }

        if (extractedRows.Count == 0)
        {
            return;
        }

        qty = extractedRows.Count;
        mysqlMessage = BuildRawMessage(extractedRows);

        designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Total read barcode QTY: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), qty))));

        ExportScanOutput(extractedRows, mysqlMessage);
    }

    private string BuildRawMessage(List<ReaderScanRecord> rows)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < rows.Count; i++)
        {
            sb.Append(rows[i].Code);
            sb.Append(";");
            sb.Append(rows[i].Image);
            sb.Append("$");
        }
        return sb.ToString();
    }

    public void SendMessage(string message)
    {
        byte carriageReturn = 13;
        byte[] messageBytes = Encoding.ASCII.GetBytes(string.Format("{0}{1}", message, (char)carriageReturn));

        if (clientStream != null && clientStream.CanWrite)
        {
            try
            {
                clientStream.Write(messageBytes, 0, messageBytes.Length);
                Logger.Info(string.Format("Sent:{0}[CR]", message));
                designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Sent:{1}[CR]", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message), System.Drawing.Color.Blue)));
            }
            catch (Exception e)
            {
                Logger.Error(e);
                designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Data sending error: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), e.Message), System.Drawing.Color.Red)));
            }
        }
        else
        {
            Logger.Error("clientStream cannot write");
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} clientStream cannot write", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Red)));
        }
    }

    public void StopClient()
    {
        if (this.cancellationTokenSource != null)
        {
            this.cancellationTokenSource.Cancel();
            this.cancellationTokenSource.Dispose();
            this.cancellationTokenSource = null;
        }

        if (this.clientStream != null)
        {
            this.clientStream.Dispose();
            this.clientStream = null;
        }

        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient = null;
        }
    }

    public bool ConnectToMysql()
    {
        if (string.IsNullOrWhiteSpace(mysqlMessage))
        {
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} No scan data to upload.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.DarkOrange)));
            return false;
        }

        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-us");
        Logger.Info("Attempting mysql connection");
        designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Attempting mysql connection", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))));
        dbConnection = new MySqlConnection("server=db3;user id=scripts;password=hmhuscripts;database=keyence;");

        try
        {
            dbConnection.Open();
            Logger.Info("Connected to mysql");
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Connected to mysql", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Green)));
            UploadToMysql(mysqlMessage);
            return true;
        }
        catch (MySqlException ex)
        {
            Logger.Error(ex);
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Error connecting to mysql database: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
            return false;
        }
    }

    public void CloseMysqlConnection()
    {
        if (dbConnection != null)
        {
            dbConnection.Close();
        }
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

            Logger.Info("Mysql upload OK");
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Uploaded message to mysql database", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Green)));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Error uploading to mysql database: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
        }
    }

    public void GetIp()
    {
        string line;
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
        catch
        {
        }
    }

    public void GetPort()
    {
        string line;
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
        catch
        {
        }
    }

    private void ExportScanOutput(List<ReaderScanRecord> rows, string rawResponse)
    {
        if (rows == null || rows.Count == 0)
        {
            return;
        }

        string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
        string filePrefix = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        lock (exportLock)
        {
            Directory.CreateDirectory(exportFolder);

            string txtPath = Path.Combine(exportFolder, filePrefix + ".txt");
            string csvPath = Path.Combine(exportFolder, filePrefix + ".csv");
            string jsonPath = Path.Combine(exportFolder, filePrefix + ".json");

            File.WriteAllText(txtPath, rawResponse);
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

    private ReaderScanRecord ParseSingleRow(string row)
    {
        string[] values = row.Split(';');
        if (values.Length == 0)
        {
            return null;
        }

        string code = values[0].Trim();
        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        string image = values.Length > 1 ? values[1].Trim() : string.Empty;

        if (code == "01" || code.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new ReaderScanRecord
        {
            Code = code,
            Image = image,
            Timestamp = DateTime.Now
        };
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
            ReaderScanRecord row = ParseSingleRow(rawRow.Trim());
            if (row != null)
            {
                result.Add(row);
            }
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

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                StopClient();
            }

            disposed = true;
        }
    }

    ~TcpClientLogic()
    {
        Dispose(false);
    }

    public bool IsDisposed()
    {
        return disposed;
    }

    private class ReaderScanRecord
    {
        public string Code { get; set; }
        public string Image { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
