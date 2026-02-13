using MySql.Data.MySqlClient;
using NLog;
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
    private readonly DesignForm designForm;
    private readonly object sync = new object();

    private TcpClient tcpClient;
    private NetworkStream clientStream;
    private CancellationTokenSource cancellationTokenSource;
    private Task listenTask;

    private MySqlConnection dbConnection;
    private string ip;
    private int port;
    private int qty;
    private string mysqlMessage;

    private bool disposed;
    private bool captureActive;
    private StringBuilder incomingBuffer = new StringBuilder();
    private List<ReaderScanRecord> currentBatch = new List<ReaderScanRecord>();
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
        this.designForm = form;
    }

    public async Task StartClientAsync()
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        designForm.Invoke(new Action(() =>
            designForm.DisplayMessage(string.Format("{0} Attempting tcp connection ({1}:{2})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port))));
        designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Attempting tcp connection ({1}:{2})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port))));

        try
        {
            StopClient();

            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            clientStream = tcpClient.GetStream();
            cancellationTokenSource = new CancellationTokenSource();

            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Connected to reader ({1}:{2})", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port), System.Drawing.Color.Green)));

            listenTask = Task.Factory.StartNew(
                () => ListenLoop(cancellationTokenSource.Token),
                cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Error connecting to reader: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
        }
    }

    private async Task ListenLoop(CancellationToken token)
    {
        byte[] buffer = new byte[4096];

        try
        {
            while (!token.IsCancellationRequested && clientStream != null)
            {
                int bytesRead = await clientStream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead <= 0)
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

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Logger.Info(chunk);

                if (designForm.IsHandleCreated)
                {
                    designForm.Invoke(new Action(() => designForm.DisplayMessage(chunk)));
                }

                ProcessIncomingChunk(chunk);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Reader listen loop cancelled.");
        }
        catch (ObjectDisposedException)
        {
            Logger.Info("Reader stream disposed.");
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
            if (designForm.IsHandleCreated)
            {
                designForm.Invoke(new Action(() =>
                    designForm.DisplayMessage(string.Format("{0} Error reading from reader: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
            }
        }
        finally
        {
            if (designForm.IsHandleCreated)
            {
                designForm.Invoke(new Action(() =>
                    designForm.DisplayMessage(string.Format("{0} Disconnected", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Black)));
            }
        }
    }

    private void ProcessIncomingChunk(string chunk)
    {
        List<ReaderScanRecord> parsed = new List<ReaderScanRecord>();

        lock (sync)
        {
            incomingBuffer.Append(chunk);

            while (true)
            {
                string text = incomingBuffer.ToString();
                int idx = text.IndexOf('$');
                if (idx < 0)
                {
                    break;
                }

                string oneRow = text.Substring(0, idx).Trim();
                incomingBuffer.Remove(0, idx + 1);

                ReaderScanRecord row = ParseRow(oneRow);
                if (row != null)
                {
                    parsed.Add(row);
                }
            }

            if (captureActive && parsed.Count > 0)
            {
                currentBatch.AddRange(parsed);
                qty = currentBatch.Count;
                mysqlMessage = BuildRawMessage(currentBatch);
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
        byte[] payload = Encoding.ASCII.GetBytes(message + (char)13);

        if (clientStream == null || !clientStream.CanWrite)
        {
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} clientStream cannot write", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Red)));
            return;
        }

        try
        {
            if (message == "LON")
            {
                lock (sync)
                {
                    captureActive = true;
                    qty = 0;
                    mysqlMessage = string.Empty;
                    currentBatch.Clear();
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
            }

            clientStream.Write(payload, 0, payload.Length);
            Logger.Info(string.Format("Sent:{0}[CR]", message));

            if (designForm.IsHandleCreated)
            {
                designForm.Invoke(new Action(() =>
                    designForm.DisplayMessage(string.Format("{0} Sent:{1}[CR]", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message), System.Drawing.Color.Blue)));
            }

            if (message == "LOFF")
            {
                FinalizeCaptureAndExport();
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
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            if (designForm.IsHandleCreated)
            {
                designForm.Invoke(new Action(() =>
                    designForm.DisplayMessage(string.Format("{0} Data sending error: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
                Logger.Error(e);
                designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Data sending error: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), e.Message), System.Drawing.Color.Red)));
            }
        }
    }

    private void FinalizeCaptureAndExport()
    {
        List<ReaderScanRecord> snapshot;

        lock (sync)
        {
            captureActive = false;
            snapshot = new List<ReaderScanRecord>(currentBatch);
            qty = snapshot.Count;
            mysqlMessage = BuildRawMessage(snapshot);
        }

        if (designForm.IsHandleCreated)
        {
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Total read barcode QTY: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), qty))));
        }

        if (snapshot.Count > 0)
        {
            ExportScanOutput(snapshot, mysqlMessage);
            Logger.Error("clientStream cannot write");
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} clientStream cannot write", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Red)));
        }
    }

    public void StopClient()
    {
        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        if (clientStream != null)
        {
            clientStream.Dispose();
            clientStream = null;
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

        listenTask = null;
    }

    public bool ConnectToMysql()
    {
        string messageToUpload;

        lock (sync)
        {
            messageToUpload = mysqlMessage;
        }

        if (string.IsNullOrWhiteSpace(messageToUpload))
        {
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} No scan data to upload.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.DarkOrange)));
            return false;
        }

        designForm.Invoke(new Action(() =>
            designForm.DisplayMessage(string.Format("{0} Attempting mysql connection", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))));

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
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Connected to mysql", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Green)));

            UploadToMysql(messageToUpload);
            Logger.Info("Connected to mysql");
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Connected to mysql", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Green)));
            UploadToMysql(mysqlMessage);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Error connecting to mysql database: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
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

            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Uploaded message to mysql database", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Green)));
            Logger.Info("Mysql upload OK");
            designForm.Invoke(new Action(() => designForm.DisplayMessage(string.Format("{0} Uploaded message to mysql database", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")), System.Drawing.Color.Green)));
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Error uploading to mysql database: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message), System.Drawing.Color.Red)));
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

    private ReaderScanRecord ParseRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row))
        {
            return null;
        }

        string[] values = row.Split(';');
        if (values.Length == 0)
        {
            return null;
        }

        string code = values[0].Trim();
        string image = values.Length > 1 ? values[1].Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(code) || code == "01" || code.Equals("OK", StringComparison.OrdinalIgnoreCase))
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

    private string BuildRawMessage(List<ReaderScanRecord> rows)
    {
        StringBuilder sb = new StringBuilder();
        foreach (ReaderScanRecord row in rows)
        {
            sb.Append(row.Code);
            sb.Append(';');
            sb.Append(row.Image);
            sb.Append('$');
    private List<ReaderScanRecord> ParseReaderResponse(string response)
    {
        List<ReaderScanRecord> result = new List<ReaderScanRecord>();
        if (string.IsNullOrWhiteSpace(response))
        {
            return result;
        }
    }

        string[] rows = response.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);

        return sb.ToString();
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
            ReaderScanRecord record = ParseRow(rawRow.Trim());
            if (record != null)
            {
                result.Add(record);
            }
        }

        return result;
    }

    private void ExportScanOutput(List<ReaderScanRecord> rows, string rawResponse)
    {
        string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
        string filePrefix = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        Directory.CreateDirectory(exportFolder);

        string txtPath = Path.Combine(exportFolder, filePrefix + ".txt");
        string csvPath = Path.Combine(exportFolder, filePrefix + ".csv");
        string jsonPath = Path.Combine(exportFolder, filePrefix + ".json");

        File.WriteAllText(txtPath, rawResponse);
        File.WriteAllLines(csvPath, BuildCsvLines(rows));
        File.WriteAllText(jsonPath, BuildJson(rows));

        if (designForm.IsHandleCreated)
        {
            designForm.Invoke(new Action(() =>
                designForm.DisplayMessage(string.Format("{0} Exported scan output: {1}, {2}, {3}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Path.GetFileName(txtPath),
                    Path.GetFileName(csvPath),
                    Path.GetFileName(jsonPath)),
                    System.Drawing.Color.DarkGreen)));
        }
    }

    private IEnumerable<string> BuildCsvLines(IEnumerable<ReaderScanRecord> rows)
    {
        List<string> lines = new List<string>();
        lines.Add("code;image;timestamp");

        foreach (ReaderScanRecord row in rows)
        {
            lines.Add(string.Format("{0};{1};{2}", EscapeCsvValue(row.Code), EscapeCsvValue(row.Image), row.Timestamp.ToString("o")));
        }

        return lines;
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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
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
                StopClient();
            }

            disposed = true;
        }

        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        StopClient();
        disposed = true;
        GC.SuppressFinalize(this);
    }

    ~TcpClientLogic()
    {
        try
        {
            StopClient();
        }
        catch
        {
        }
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
