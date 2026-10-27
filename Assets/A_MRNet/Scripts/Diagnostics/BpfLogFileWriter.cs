using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// JSONL session log file sink. Records are queued from any thread and
    /// written in batches by <see cref="Drain"/>, so a log call never performs
    /// a synchronous disk write. Files rotate at a size limit and only the
    /// newest files are retained.
    /// </summary>
    public sealed class BpfLogFileWriter : IDisposable
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        private readonly ConcurrentQueue<BpfLogRecord> pending = new ConcurrentQueue<BpfLogRecord>();
        private readonly StringBuilder batch = new StringBuilder(8192);
        private readonly object writeGate = new object();
        private readonly string directory;
        private readonly string sessionId;
        private readonly long maxFileBytes;
        private readonly int maxRetainedFiles;

        private FileStream stream;
        private StreamWriter writer;
        private string currentPath;
        private long currentBytes;
        private int partIndex = 1;
        private bool disposed;

        public BpfLogFileWriter(string directory, string sessionId, long maxFileBytes, int maxRetainedFiles)
        {
            this.directory = directory;
            this.sessionId = sessionId;
            this.maxFileBytes = maxFileBytes;
            this.maxRetainedFiles = maxRetainedFiles;
        }

        /// <summary>Full path of the file currently being appended to.</summary>
        public string CurrentPath
        {
            get
            {
                if (currentPath == null)
                {
                    currentPath = BuildPath(partIndex);
                }

                return currentPath;
            }
        }

        /// <summary>Bytes written to the current part file.</summary>
        public long CurrentFileBytes
        {
            get { return currentBytes; }
        }

        /// <summary>Records waiting to be written.</summary>
        public int PendingCount
        {
            get { return pending.Count; }
        }

        /// <summary>True when a write failed and the sink stopped accepting data.</summary>
        public bool IsFaulted { get; private set; }

        /// <summary>Text of the first write failure, if any.</summary>
        public string LastError { get; private set; }

        /// <summary>Queues a record for the next drain. Never touches the disk.</summary>
        public void Enqueue(BpfLogRecord record)
        {
            if (record == null || IsFaulted || disposed)
            {
                return;
            }

            pending.Enqueue(record);
        }

        /// <summary>Writes every queued record in one batch.</summary>
        public int Drain()
        {
            if (disposed)
            {
                return 0;
            }

            lock (writeGate)
            {
                if (IsFaulted)
                {
                    return 0;
                }

                batch.Length = 0;
                BpfLogRecord record;
                int written = 0;
                while (pending.TryDequeue(out record))
                {
                    batch.Append(record.ToJsonLine());
                    batch.Append('\n');
                    written++;
                }

                if (written == 0)
                {
                    return 0;
                }

                if (!EnsureWriter())
                {
                    return 0;
                }

                try
                {
                    string payload = batch.ToString();
                    int byteCount = Utf8WithoutBom.GetByteCount(payload);
                    writer.Write(payload);
                    writer.Flush();
                    stream.Flush();
                    currentBytes += byteCount;
                    if (maxFileBytes > 0 && currentBytes >= maxFileBytes)
                    {
                        RollToNextPart();
                    }
                }
                catch (Exception exception)
                {
                    Fault(exception);
                }

                return written;
            }
        }

        /// <summary>Drains the queue and forces the current file to storage.</summary>
        public void Flush()
        {
            int written = Drain();
            if (written == 0 && !HasOpenWriter())
            {
                return;
            }

            lock (writeGate)
            {
                if (writer == null || IsFaulted)
                {
                    return;
                }

                try
                {
                    writer.Flush();
                    stream.Flush(true);
                }
                catch (Exception exception)
                {
                    Fault(exception);
                }
            }
        }

        /// <summary>Closes the current file after a final flush.</summary>
        public void Close()
        {
            Flush();
            lock (writeGate)
            {
                CloseWriter();
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Close();
        }

        private bool HasOpenWriter()
        {
            lock (writeGate)
            {
                return writer != null;
            }
        }

        private bool EnsureWriter()
        {
            if (writer != null)
            {
                return true;
            }

            try
            {
                Directory.CreateDirectory(directory);
                string path = BuildPath(partIndex);
                stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                writer = new StreamWriter(stream, Utf8WithoutBom);
                currentPath = path;
                currentBytes = stream.Length;
                PruneOldFiles(path);
                return true;
            }
            catch (Exception exception)
            {
                Fault(exception);
                return false;
            }
        }

        private void RollToNextPart()
        {
            CloseWriter();
            partIndex++;
            currentBytes = 0;
            currentPath = BuildPath(partIndex);
        }

        private void CloseWriter()
        {
            if (writer == null)
            {
                return;
            }

            try
            {
                writer.Flush();
                writer.Dispose();
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
            }
            finally
            {
                writer = null;
                stream = null;
            }
        }

        private string BuildPath(int part)
        {
            string name = part <= 1
                ? BpfLogOptions.FileNamePrefix + sessionId + BpfLogOptions.FileExtension
                : BpfLogOptions.FileNamePrefix + sessionId + ".p" + part.ToString(CultureInfo.InvariantCulture) + BpfLogOptions.FileExtension;
            return Path.Combine(directory, name);
        }

        /// <summary>Deletes the oldest session files beyond the retention limit.</summary>
        private void PruneOldFiles(string keepPath)
        {
            if (maxRetainedFiles <= 0)
            {
                return;
            }

            try
            {
                string[] files = Directory.GetFiles(
                    directory,
                    BpfLogOptions.FileNamePrefix + "*" + BpfLogOptions.FileExtension);
                if (files.Length <= maxRetainedFiles)
                {
                    return;
                }

                List<string> ordered = new List<string>(files);
                ordered.Sort((left, right) =>
                {
                    int byTime = File.GetLastWriteTimeUtc(right).CompareTo(File.GetLastWriteTimeUtc(left));
                    return byTime != 0 ? byTime : PartNumberFor(right).CompareTo(PartNumberFor(left));
                });

                for (int i = maxRetainedFiles; i < ordered.Count; i++)
                {
                    if (string.Equals(ordered[i], keepPath, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(ordered[i], currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    try
                    {
                        File.Delete(ordered[i]);
                    }
                    catch
                    {
                        // A locked or already removed file must never break logging.
                    }
                }
            }
            catch (Exception exception)
            {
                LastError = exception.Message;
            }
        }

        /// <summary>Part number encoded in a session file name; 1 when absent.</summary>
        private static int PartNumberFor(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            int marker = name.IndexOf(".p", StringComparison.Ordinal);
            if (marker < 0)
            {
                return 1;
            }

            int part;
            return int.TryParse(
                name.Substring(marker + 2),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out part)
                ? part
                : 1;
        }

        private void Fault(Exception exception)
        {
            IsFaulted = true;
            LastError = exception == null ? "unknown log write failure" : exception.Message;
            UnityEngine.Debug.LogWarning(
                "[Bpf][Warning][Diagnostics] file.write_failed: " + LastError);
        }
    }
}
