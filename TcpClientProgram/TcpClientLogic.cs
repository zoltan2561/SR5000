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

    private string lastRawMessage;

    private bool disposed;
    private bool captureActive;

    private readonly StringBuilder incomingBuffer = new StringBuilder();
    private readonly List<ReaderScanRecord> currentBatch = new List<ReaderScanRecord>();

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // QR rekord kezdete: 10 számjegy (OPC)
    private static readonly Regex QrStartRegex = new Regex(@"(?<!\d)\d{10}", RegexOptions.Compiled);

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

        SafeUiMessage(string.Format("{0} Attempting tcp connection ({1}:{2})",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port));

        try
        {
            StopClient();

            tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(ip, port);
            clientStream = tcpClient.GetStream();
            cancellationTokenSource = new CancellationTokenSource();

            SafeUiMessage(string.Format("{0} Connected to reader ({1}:{2})",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ip, port),
                System.Drawing.Color.Green);

            listenTask = Task.Factory.StartNew(
                () => ListenLoop(cancellationTokenSource.Token),
                cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format("{0} Error connecting to reader: {1}",
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
            SafeUiMessage(string.Format("{0} Error reading from reader: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message),
                System.Drawing.Color.Red);
        }
        finally
        {
            SafeUiMessage(string.Format("{0} Disconnected",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Black);
        }
    }

    private void ProcessIncomingChunk(string chunk)
    {
        List<ReaderScanRecord> parsedDollar = new List<ReaderScanRecord>();

        lock (sync)
        {
            incomingBuffer.Append(chunk);

            // ha van $ framing, abból azonnal szedjük a sorokat
            while (true)
            {
                string text = incomingBuffer.ToString();
                int idx = text.IndexOf('$');
                if (idx < 0) break;

                string oneRow = text.Substring(0, idx).Trim();
                incomingBuffer.Remove(0, idx + 1);

                ReaderScanRecord row = ParseRow(oneRow);
                if (row != null) parsedDollar.Add(row);
            }

            if (captureActive && parsedDollar.Count > 0)
            {
                currentBatch.AddRange(parsedDollar);
                qty = currentBatch.Count;
            }
        }
    }

    public void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            SafeUiMessage(string.Format("{0} Empty message ignored.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.DarkOrange);
            return;
        }

        if (clientStream == null || !clientStream.CanWrite)
        {
            SafeUiMessage(string.Format("{0} clientStream cannot write",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Red);
            return;
        }

        byte[] payload = Encoding.ASCII.GetBytes(message + (char)13);

        try
        {
            if (message.Equals("LON", StringComparison.OrdinalIgnoreCase))
            {
                lock (sync)
                {
                    captureActive = true;
                    qty = 0;
                    lastRawMessage = string.Empty;
                    currentBatch.Clear();
                    // incomingBuffer.Clear(); // NE töröld, lehet késleltetett válasz
                }
            }

            clientStream.Write(payload, 0, payload.Length);
            Logger.Info(string.Format("Sent:{0}[CR]", message));

            SafeUiMessage(string.Format("{0} Sent:{1}[CR]",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message),
                System.Drawing.Color.Blue);

            if (message.Equals("LOFF", StringComparison.OrdinalIgnoreCase))
            {
                // .NET 4.0 / régebbi: nincs Task.Run -> ThreadPool
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Thread.Sleep(800); // állítható
                    FinalizeCaptureAndExport();
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format("{0} Data sending error: {1}",
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

            rawBuffer = incomingBuffer.ToString();
            lastRawMessage = rawBuffer ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(rawBuffer))
            {
                var fromBuffer = ParseReaderResponse(rawBuffer);
                if (fromBuffer.Count > 0)
                {
                    snapshot.AddRange(fromBuffer);
                }
            }

            snapshot = DeduplicateByCode(snapshot);
            qty = snapshot.Count;

            incomingBuffer.Clear();
            currentBatch.Clear();
        }

        SafeUiMessage(string.Format("{0} Total read barcode QTY: {1}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), qty));

        if (snapshot.Count > 0)
        {
            ExportScanOutput(snapshot);
        }
        else
        {
            SafeUiMessage(string.Format("{0} No barcode captured.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.DarkOrange);
        }
    }

    private List<ReaderScanRecord> DeduplicateByCode(List<ReaderScanRecord> rows)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var outList = new List<ReaderScanRecord>();

        foreach (var r in rows)
        {
            if (r == null) continue;
            var code = (r.Code ?? string.Empty).Trim();
            if (code.Length == 0) continue;

            if (seen.Add(code))
            {
                outList.Add(r);
            }
        }
        return outList;
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

    // ========= MYSQL =========

    public bool ConnectToMysql()
    {
        // feltöltéshez a legutolsó raw dumpból parse-olunk
        List<ReaderScanRecord> snapshot;
        lock (sync)
        {
            snapshot = ParseReaderResponse(lastRawMessage);
            snapshot = DeduplicateByCode(snapshot);
        }

        if (snapshot == null || snapshot.Count == 0)
        {
            SafeUiMessage(string.Format("{0} No scan data to upload.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.DarkOrange);
            return false;
        }

        SafeUiMessage(string.Format("{0} Attempting mysql connection",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));

        dbConnection = new MySqlConnection("server=db3;user id=scripts;password=hmhuscripts;database=keyence;");

        try
        {
            dbConnection.Open();
            SafeUiMessage(string.Format("{0} Connected to mysql",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Green);

            // public helyett legyen private/internal -> megszűnik az accessibility hiba
            UploadToMysqlInternal(snapshot);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format("{0} Error connecting to mysql database: {1}",
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

    // CS0051 fix: ne legyen public, mert ReaderScanRecord private nested type
    private void UploadToMysqlInternal(List<ReaderScanRecord> rows)
    {
        if (dbConnection == null || dbConnection.State != System.Data.ConnectionState.Open)
        {
            SafeUiMessage(string.Format("{0} MySQL connection is not open.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.Red);
            return;
        }

        try
        {
            const string sql = "INSERT INTO scans (data, image,processed) VALUES (@data, @image,0)";
            using (var cmd = new MySqlCommand(sql, dbConnection))
            {
                var pData = cmd.Parameters.Add("@data", MySqlDbType.VarChar);
                var pImg = cmd.Parameters.Add("@image", MySqlDbType.VarChar);

                int inserted = 0;

                foreach (var row in rows)
                {
                    if (row == null) continue;
                    var code = (row.Code ?? string.Empty).Trim();
                    if (code.Length == 0) continue;

                    pData.Value = code;
                    pImg.Value = (row.Image ?? string.Empty);

                    cmd.ExecuteNonQuery();
                    inserted++;
                }

                SafeUiMessage(string.Format("{0} Uploaded to mysql database (rows: {1})",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), inserted),
                    System.Drawing.Color.Green);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            SafeUiMessage(string.Format("{0} Error uploading to mysql database: {1}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ex.Message),
                System.Drawing.Color.Red);
        }
    }

    // ========= SETTINGS =========

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
        catch { }
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
        catch { }
    }

    // ========= PARSE =========

    private ReaderScanRecord ParseRow(string row)
    {
        if (string.IsNullOrWhiteSpace(row)) return null;

        string codePart = row;
        string imagePart = string.Empty;

        if (row.Contains(";"))
        {
            var values = row.Split(';');
            codePart = values.Length > 0 ? values[0] : row;
            imagePart = values.Length > 1 ? values[1] : string.Empty;
        }

        string code = ExtractFullCode(codePart);
        if (string.IsNullOrWhiteSpace(code)) return null;

        return new ReaderScanRecord
        {
            Code = code,
            Image = imagePart == null ? string.Empty : imagePart.Trim(),
            Timestamp = DateTime.Now
        };
    }

    private string ExtractFullCode(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        string s = input.Trim();
        if (s.Equals("OK", StringComparison.OrdinalIgnoreCase)) return string.Empty;
        if (s.Equals("01", StringComparison.OrdinalIgnoreCase)) return string.Empty;

        return s;
    }

    private List<ReaderScanRecord> ParseReaderResponse(string response)
    {
        var result = new List<ReaderScanRecord>();
        if (string.IsNullOrWhiteSpace(response)) return result;

        int noiseIdx = response.IndexOf("/webscripts/", StringComparison.OrdinalIgnoreCase);
        if (noiseIdx >= 0)
        {
            response = response.Substring(0, noiseIdx);
        }

        if (response.Contains("$"))
        {
            string[] rows = response.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string rawRow in rows)
            {
                var rec = ParseRow(rawRow.Trim());
                if (rec != null) result.Add(rec);
            }
            return result;
        }

        MatchCollection m = QrStartRegex.Matches(response);

        if (m.Count == 0)
        {
            var one = response.Trim().Trim(',');
            if (!string.IsNullOrWhiteSpace(one))
            {
                result.Add(new ReaderScanRecord { Code = one, Image = "", Timestamp = DateTime.Now });
            }
            return result;
        }

        for (int i = 0; i < m.Count; i++)
        {
            int start = m[i].Index;
            int end = (i + 1 < m.Count) ? m[i + 1].Index : response.Length;

            string piece = response.Substring(start, end - start).Trim();
            piece = piece.Trim().Trim(',');

            if (!Regex.IsMatch(piece, @"^\d{10}")) continue;

            result.Add(new ReaderScanRecord
            {
                Code = piece,
                Image = string.Empty,
                Timestamp = DateTime.Now
            });
        }

        return result;
    }

    // ========= EXPORT =========

    private void ExportScanOutput(List<ReaderScanRecord> rows)
    {
        string exportFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
        string filePrefix = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");

        Directory.CreateDirectory(exportFolder);

        string txtPath = Path.Combine(exportFolder, filePrefix + ".txt");
        string csvPath = Path.Combine(exportFolder, filePrefix + ".csv");
        string jsonPath = Path.Combine(exportFolder, filePrefix + ".json");

        File.WriteAllText(txtPath, lastRawMessage ?? string.Empty);
        File.WriteAllLines(csvPath, BuildCsvLines(rows));
        File.WriteAllText(jsonPath, BuildJson(rows));

        SafeUiMessage(string.Format("{0} Exported scan output: {1}, {2}, {3}",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Path.GetFileName(txtPath),
                Path.GetFileName(csvPath),
                Path.GetFileName(jsonPath)),
            System.Drawing.Color.DarkGreen);
    }

    private IEnumerable<string> BuildCsvLines(IEnumerable<ReaderScanRecord> rows)
    {
        List<string> lines = new List<string>();
        lines.Add("data");

        foreach (ReaderScanRecord row in rows)
        {
            lines.Add(EscapeCsvValue(row.Code));
        }

        return lines;
    }

    private string EscapeCsvValue(string value)
    {
        if (value == null) return string.Empty;

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
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
            if (row == null) continue;
            string code = (row.Code ?? string.Empty).Trim();
            if (code.Length == 0) continue;

            if (!first) sb.AppendLine(",");

            sb.Append("  {\"data\":\"")
              .Append(JsonEscape(code))
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

    // ========= UI =========

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
        catch { }
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
        catch { }
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
