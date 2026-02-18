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

    // LOFF után tail beérkezéshez (idle-wait)
    private DateTime lastChunkUtc = DateTime.MinValue;

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    // OPC mindig 10 számjegy
    private static readonly Regex OpcRegex = new Regex(@"^\d{10}$", RegexOptions.Compiled);

    // DM nálad jellemzően 24 számjegy (de hagyok tartományt)
    private static readonly Regex DmRegex = new Regex(@"^\d{16,32}$", RegexOptions.Compiled);

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

                lock (sync)
                {
                    lastChunkUtc = DateTime.UtcNow;
                }

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

    /// <summary>
    /// Nálad a Keyence sokszor EGY sorban (batch) küldi a több kódot,
    /// és CR (0D) csak a végén van. Ezért itt csak bufferelünk.
    /// </summary>
    private void ProcessIncomingChunk(string chunk)
    {
        lock (sync)
        {
            incomingBuffer.Append(chunk);

            // opcionális: ha captureActive, lehetne itt is parse-olni CR-es sorokra,
            // de mivel batch jön, stabilabb finalizenál parse-olni.
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
                    lastChunkUtc = DateTime.UtcNow;
                }
            }

            clientStream.Write(payload, 0, payload.Length);
            Logger.Info(string.Format("Sent:{0}[CR]", message));

            SafeUiMessage(string.Format("{0} Sent:{1}[CR]",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), message),
                System.Drawing.Color.Blue);

            if (message.Equals("LOFF", StringComparison.OrdinalIgnoreCase))
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    WaitForReaderIdleThenFinalize(idleMs: 400, maxWaitMs: 5000, pollMs: 100);
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

    private void WaitForReaderIdleThenFinalize(int idleMs, int maxWaitMs, int pollMs)
    {
        DateTime start = DateTime.UtcNow;

        while (true)
        {
            Thread.Sleep(pollMs);

            DateTime last;
            lock (sync) last = lastChunkUtc;

            if (last != DateTime.MinValue &&
                (DateTime.UtcNow - last).TotalMilliseconds >= idleMs)
            {
                break;
            }

            if ((DateTime.UtcNow - start).TotalMilliseconds >= maxWaitMs)
            {
                break;
            }
        }

        FinalizeCaptureAndExport();
    }

    private void FinalizeCaptureAndExport()
    {
        List<ReaderScanRecord> snapshot;
        string rawBuffer;

        lock (sync)
        {
            captureActive = false;

            // currentBatch most nem élőben töltődik, de hagyom kompatibilitás miatt
            snapshot = new List<ReaderScanRecord>(currentBatch);

            rawBuffer = incomingBuffer.ToString();
            lastRawMessage = rawBuffer ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(rawBuffer))
            {
                var fromBuffer = ParseReaderResponse(rawBuffer);
                if (fromBuffer.Count > 0)
                    snapshot.AddRange(fromBuffer);
            }

            // Dedupe DM alapján (Code = DM)
            snapshot = DeduplicateByCode(snapshot);
            qty = snapshot.Count;

            incomingBuffer.Clear();
            currentBatch.Clear();
        }

        SafeUiMessage(string.Format("{0} Total read barcode QTY: {1}",
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), qty));

        if (snapshot.Count <= 0)
         //   ExportScanOutput(snapshot);
                
       
            SafeUiMessage(string.Format("{0} No barcode captured.",
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                System.Drawing.Color.DarkOrange);
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
                outList.Add(r);
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
            // data = DM (1618...), image = teljes qr_raw (opc,...,dm)
            const string sql = "INSERT INTO scans (data, qr_raw, polc, processed) VALUES (@data, @image, @shelf, 0)";
            using (var cmd = new MySqlCommand(sql, dbConnection))
            {
                var pData = cmd.Parameters.Add("@data", MySqlDbType.VarChar);
                var pImg = cmd.Parameters.Add("@image", MySqlDbType.VarChar);
                var pShelf = cmd.Parameters.Add("@shelf", MySqlDbType.VarChar);

                int inserted = 0;

                foreach (var row in rows)
                {
                    if (row == null) continue;

                    var dm = (row.Code ?? string.Empty).Trim();
                    if (dm.Length == 0) continue;

                    pData.Value = dm;
                    pImg.Value = (row.Image ?? string.Empty);
                    pShelf.Value = (designForm.CurrentShelfCode ?? string.Empty).Trim();

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

    /// <summary>
    /// Nálad a rekord formátum biztos:
    /// "OPC(10 digit),TYPE...,EXTRA,DM(16-32 digit)"
    /// Több rekord jöhet egy sorban batchként: ...,DM,OPC,...,DM,OPC,...,DM
    /// Terminator: CR (0D)
    /// </summary>
    private List<ReaderScanRecord> ParseReaderResponse(string response)
    {
        var result = new List<ReaderScanRecord>();
        if (string.IsNullOrWhiteSpace(response)) return result;

        int noiseIdx = response.IndexOf("/webscripts/", StringComparison.OrdinalIgnoreCase);
        if (noiseIdx >= 0) response = response.Substring(0, noiseIdx);

        response = response.Trim();

        // ha CR van, akkor több "line"-t kezelünk
        if (response.Contains("\r"))
        {
            string[] lines = response.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var ln in lines)
            {
                var one = ParseKeyenceBatchLine(ln.Trim());
                if (one.Count > 0) result.AddRange(one);
            }
            return result;
        }

        // batch egy sorban
        result.AddRange(ParseKeyenceBatchLine(response));
        return result;
    }

    private List<ReaderScanRecord> ParseKeyenceBatchLine(string line)
    {
        var outList = new List<ReaderScanRecord>();
        if (string.IsNullOrWhiteSpace(line)) return outList;

        // tokenizálás vessző alapján
        if (!line.Contains(",")) return outList;

        string[] raw = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

        // trim + üres kiszűrés
        List<string> tokens = new List<string>(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            string x = (raw[i] ?? "").Trim();
            if (x.Length > 0) tokens.Add(x);
        }

        if (tokens.Count < 4) return outList;

        // 4-es blokkok: [OPC][TYPE][EXTRA][DM]
        // és ismétlődik batchben
        // Biztos pont: tokens[i] OPC (10 digit), tokens[i+3] DM (16-32 digit)
        for (int i = 0; i + 3 < tokens.Count;)
        {
            string opc = tokens[i];
            if (!OpcRegex.IsMatch(opc))
            {
                // elcsúszás esetén keressünk következő OPC-t
                i++;
                continue;
            }

            string type = tokens[i + 1];
            string extra = tokens[i + 2];
            string dm = tokens[i + 3];

            if (!DmRegex.IsMatch(dm))
            {
                // ha nem DM, akkor csúszás, menjünk tovább 1 tokennel
                i++;
                continue;
            }

            // qr_raw: pontosan olyan formában, ahogy a DB-be szeretnéd
            string qrRaw = opc + "," + type + "," + extra + "," + dm;

            outList.Add(new ReaderScanRecord
            {
                Code = dm,       // data oszlop (DM)
                Image = qrRaw,   // image oszlop (teljes qr_raw)
                Timestamp = DateTime.Now
            });

            i += 4;
        }

        return outList;
    }

    // ========= EXPORT ========= egyelőre kikommentelem elég csak db save

    /*
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
    */
    private IEnumerable<string> BuildCsvLines(IEnumerable<ReaderScanRecord> rows)
    {
        List<string> lines = new List<string>();
        lines.Add("data;qr_raw");

        foreach (ReaderScanRecord row in rows)
        {
            // DM ; qr_raw
            lines.Add(EscapeCsvValue(row.Code) + ";" + EscapeCsvValue(row.Image));
        }

        return lines;
    }

    private string EscapeCsvValue(string value)
    {
        if (value == null) return string.Empty;

        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r") || value.Contains(";"))
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

            string dm = (row.Code ?? string.Empty).Trim();
            if (dm.Length == 0) continue;

            if (!first) sb.AppendLine(",");

            sb.Append("  {\"data\":\"")
              .Append(JsonEscape(dm))
              .Append("\",\"qr_raw\":\"")
              .Append(JsonEscape(row.Image ?? string.Empty))
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
        public string Code { get; set; }     // DM
        public string Image { get; set; }    // qr_raw
        public DateTime Timestamp { get; set; }
    }
}
