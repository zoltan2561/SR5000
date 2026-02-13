using MySql.Data.MySqlClient;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly StringBuilder incomingBuffer = new StringBuilder();
    private readonly List<ReaderScanRecord> currentBatch = new List<ReaderScanRecord>();

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

        SafeUiMessage(string.Format(
            "{0} Attempting tcp connection ({1}:{2})",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port));

        try
        {
            StopClient();

            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);

            clientStream = tcpClient.GetStream();
            cancellationTokenSource = new CancellationTokenSource();

            SafeUiMessage(string.Format(
                "{0} Connected to reader ({1}:{2})",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port),
                System.Drawing.Color.Green);

            listenTask = Task.Factory.StartNew(
                    () => ListenLoop(cancellationTokenSource.Token),
                    cancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default)
                .Unwrap();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format(
                "{0} Error connecting to reader: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message),
                System.Drawing.Color.Red);
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
                if (bytesRead <= 0) break;

                string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Logger.Info(chunk);

                SafeUiMessage(chunk);

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
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format(
                "{0} Error reading from reader: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message),
                System.Drawing.Color.Red);
        }
        finally
        {
            SafeUiMessage(string.Format(
                "{0} Disconnected",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Black);
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
                if (idx < 0) break;

                string oneRow = text.Substring(0, idx).Trim();
                incomingBuffer.Remove(0, idx + 1);

                ReaderScanRecord row = ParseRow(oneRow);
                if (row != null) parsed.Add(row);
            }

            if (captureActive && parsed.Count > 0)
            {
                currentBatch.AddRange(parsed);
                qty = currentBatch.Count;
                mysqlMessage = BuildRawMessage(currentBatch);
            }
        }
    }

    public void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            SafeUiMessage(string.Format(
                "{0} Empty message ignored.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.DarkOrange);
            return;
        }

        if (clientStream == null || !clientStream.CanWrite)
        {
            SafeUiMessage(string.Format(
                "{0} clientStream cannot write",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Red);
            return;
        }

        byte[] payload = Encoding.ASCII.GetBytes(message + (char)13);

        try
        {
            // LON: capture start
            if (message.Equals("LON", StringComparison.OrdinalIgnoreCase))
            {
                lock (sync)
                {
                    captureActive = true;
                    qty = 0;
                    mysqlMessage = string.Empty;
                    currentBatch.Clear();
                    // fontos: nem törlünk incomingBuffer-t, mert ha a reader már küld,
                    // akkor később LOFF-nál még kellhet. Ha akarod: incomingBuffer.Clear();
                }
            }

            clientStream.Write(payload, 0, payload.Length);
            Logger.Info(string.Format("Sent:{0}[CR]", message));

            SafeUiMessage(string.Format(
                "{0} Sent:{1}[CR]",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message),
                System.Drawing.Color.Blue);

            // LOFF: capture stop + export
            if (message.Equals("LOFF", StringComparison.OrdinalIgnoreCase))
            {
                FinalizeCaptureAndExport();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format(
                "{0} Data sending error: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message),
                System.Drawing.Color.Red);
        }
    }

    private void FinalizeCaptureAndExport()
    {
        List<ReaderScanRecord> snapshot;
        string rawBuffer;

        lock (sync)
        {
            captureActive = false;

            snapshot = new List<ReaderScanRecord>(currentBatch);

            // ha valamiért nem $-ra darabolva ment át, itt még próbáljuk regex-szel
            rawBuffer = incomingBuffer.ToString();
            if (snapshot.Count == 0 && !string.IsNullOrWhiteSpace(rawBuffer))
            {
                snapshot = ParseReaderResponse(rawBuffer);
            }

            qty = snapshot.Count;
            mysqlMessage = BuildRawMessage(snapshot);

            // lezárás után tisztítunk, hogy a következő run tiszta legyen
            incomingBuffer.Clear();
            currentBatch.Clear();
        }

        SafeUiMessage(string.Format(
            "{0} Total read barcode QTY: {1}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), qty));

        if (snapshot.Count > 0)
        {
            ExportScanOutput(snapshot, mysqlMessage);
        }
        else
        {
            SafeUiMessage(string.Format(
                "{0} No barcode captured.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.DarkOrange);
        }
    }

    public void StopClient()
    {
        if (cancellationTokenSource != null)
        {
            try { cancellationTokenSource.Cancel(); } catch { }
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }

        if (clientStream != null)
        {
            try { clientStream.Dispose(); } catch { }
            clientStream = null;
        }

        if (tcpClient != null)
        {
            try { tcpClient.Close(); } catch { }
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
            SafeUiMessage(string.Format(
                "{0} No scan data to upload.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.DarkOrange);
            return false;
        }

        SafeUiMessage(string.Format(
            "{0} Attempting mysql connection",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

        // TODO: ezt inkább configból, ne hardcode-olva
        dbConnection = new MySqlConnection("server=db3;user id=scripts;password=hmhuscripts;database=keyence;");

        try
        {
            dbConnection.Open();
            SafeUiMessage(string.Format(
                "{0} Connected to mysql",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Green);

            UploadToMysql(messageToUpload);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format(
                "{0} Error connecting to mysql database: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message),
                System.Drawing.Color.Red);
            return false;
        }
    }

    public void CloseMysqlConnection()
    {
        if (dbConnection != null)
        {
            try { dbConnection.Close(); } catch { }
        }
    }

    public void UploadToMysql(string message)
    {
        if (dbConnection == null || dbConnection.State != System.Data.ConnectionState.Open)
        {
            SafeUiMessage(string.Format(
                "{0} MySQL connection is not open.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Red);
            return;
        }

        List<ReaderScanRecord> rows = ParseReaderResponse(message);

        try
        {
            foreach (ReaderScanRecord row in rows)
            {
                const string sql = "INSERT INTO test (data, image) VALUES (@data, @image)";
                using (MySqlCommand cmd = new MySqlCommand(sql, dbConnection))
                {
                    cmd.Parameters.AddWithValue("@data", row.Code);
                    cmd.Parameters.AddWithValue("@image", row.Image ?? string.Empty);
                    cmd.ExecuteNonQuery();
                }
            }

            SafeUiMessage(string.Format(
                "{0} Uploaded message to mysql database (rows: {1})",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), rows.Count),
                System.Drawing.Color.Green);
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format(
                "{0} Error uploading to mysql database: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message),
                System.Drawing.Color.Red);
        }
    }

    public void GetIp()
    {
        try
        {
            using (StreamReader sr = new StreamReader("settings.ini"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("ip", StringComparison.OrdinalIgnoreCase))
                    {
                        this.ip = line.Split('=')[1].Trim();
                        return;
                    }
                }
            }
        }
        catch
        {
        }
    }

    public void GetPort()
    {
        try
        {
            using (StreamReader sr = new StreamReader("settings.ini"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("port", StringComparison.OrdinalIgnoreCase))
                    {
                        this.port = Int32.Parse(line.Split('=')[1].Trim());
                        return;
                    }
                }
            }
        }
        catch
        {
        }
    }

    private ReaderScanRecord ParseRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row)) return null;

        string[] values = row.Split(';');
        if (values.Length == 0) return null;

        string firstToken = values[0].Trim();
        string code = ExtractBarcode(firstToken);
        string image = values.Length > 1 ? values[1].Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(code) ||
            code == "01" ||
            code.Equals("OK", StringComparison.OrdinalIgnoreCase))
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
            sb.Append(row.Image ?? string.Empty);
            sb.Append('$');
        }
        return sb.ToString();
    }

    private List<ReaderScanRecord> ParseReaderResponse(string response)
    {
        List<ReaderScanRecord> result = new List<ReaderScanRecord>();
        if (string.IsNullOrWhiteSpace(response)) return result;

        if (response.Contains("$"))
        {
            string[] rows = response.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawRow in rows)
            {
                ReaderScanRecord record = ParseRow(rawRow.Trim());
                if (record != null) result.Add(record);
            }
            return result;
        }

        // fallback: ha “ömlesztett” válasz, kinyerünk 8-20 számjegyet
        MatchCollection matches = Regex.Matches(response, @"(?<!\d)\d{8,20}(?!\d)");
        foreach (Match match in matches)
        {
            if (!match.Success) continue;

            result.Add(new ReaderScanRecord
            {
                Code = match.Value,
                Image = string.Empty,
                Timestamp = DateTime.Now
            });
        }

        return result;
    }

    private string ExtractBarcode(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        Match match = Regex.Match(input.Trim(), @"^\d{8,20}");
        return match.Success ? match.Value : string.Empty;
    }

    private void ExportScanOutput(List<ReaderScanRecord> rows, string rawResponse)
    {
        string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
        string filePrefix = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        Directory.CreateDirectory(exportFolder);

        string txtPath = Path.Combine(exportFolder, filePrefix + ".txt");
        string csvPath = Path.Combine(exportFolder, filePrefix + ".csv");
        string jsonPath = Path.Combine(exportFolder, filePrefix + ".json");

        File.WriteAllText(txtPath, rawResponse ?? string.Empty);
        File.WriteAllLines(csvPath, BuildCsvLines(rows));
        File.WriteAllText(jsonPath, BuildJson(rows));

        SafeUiMessage(string.Format(
            "{0} Exported scan output: {1}, {2}, {3}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Path.GetFileName(txtPath),
            Path.GetFileName(csvPath),
            Path.GetFileName(jsonPath)),
            System.Drawing.Color.DarkGreen);
    }

    private IEnumerable<string> BuildCsvLines(IEnumerable<ReaderScanRecord> rows)
    {
        List<string> lines = new List<string> { "data" };

        foreach (ReaderScanRecord row in rows)
        {
            lines.Add(EscapeCsvValue(row.Code));
        }

        return lines;
    }

    private string EscapeCsvValue(string value)
    {
        if (value == null) return string.Empty;

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
            if (!first) sb.AppendLine(",");

            sb.Append("  {\"data\":\"")
              .Append(JsonEscape(row.Code))
              .Append("\"}");

            first = false;
        }

        sb.AppendLine();
        sb.Append("]");
        return sb.ToString();
    }

    private string JsonEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private void SafeUiMessage(string message, System.Drawing.Color? color = null)
    {
        try
        {
            if (designForm == null) return;
            if (!designForm.IsHandleCreated) return;

            if (designForm.InvokeRequired)
            {
                designForm.Invoke(new Action(() =>
                {
                    if (color.HasValue) designForm.DisplayMessage(message, color.Value);
                    else designForm.DisplayMessage(message);
                }));
            }
            else
            {
                if (color.HasValue) designForm.DisplayMessage(message, color.Value);
                else designForm.DisplayMessage(message);
            }
        }
        catch
        {
            // UI race esetén inkább csendben elengedjük
        }
    }

    public void Dispose()
    {
        if (disposed) return;

        StopClient();
        CloseMysqlConnection();

        disposed = true;
        GC.SuppressFinalize(this);
    }

    ~TcpClientLogic()
    {
        try
        {
            StopClient();
            CloseMysqlConnection();
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
