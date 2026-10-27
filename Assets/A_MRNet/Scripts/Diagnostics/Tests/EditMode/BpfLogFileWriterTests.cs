using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Bpf.Diagnostics.Tests
{
    /// <summary>
    /// EditMode coverage for the JSONL session file sink: batched writes,
    /// rotation at the size limit and retention of the newest files.
    /// </summary>
    [TestFixture]
    public sealed class BpfLogFileWriterTests
    {
        private string tempDirectory;

        [SetUp]
        public void SetUp()
        {
            tempDirectory = Path.Combine(Path.GetTempPath(), "bpf-log-writer-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, true);
            }
        }

        [Test]
        public void RecordsAreBufferedUntilFlush()
        {
            using (BpfLogFileWriter writer = new BpfLogFileWriter(tempDirectory, "buffer", 1024 * 1024, 5))
            {
                writer.Enqueue(Record("test.buffered"));

                Assert.That(File.Exists(writer.CurrentPath), Is.False);
                Assert.That(writer.PendingCount, Is.EqualTo(1));

                writer.Flush();

                Assert.That(File.Exists(writer.CurrentPath), Is.True);
                Assert.That(ReadAllTextShared(writer.CurrentPath), Does.Contain("test.buffered"));
                Assert.That(writer.PendingCount, Is.EqualTo(0));
            }
        }

        [Test]
        public void SessionFileNameContainsTheSessionId()
        {
            using (BpfLogFileWriter writer = new BpfLogFileWriter(tempDirectory, "session-42", 1024 * 1024, 5))
            {
                Assert.That(Path.GetFileName(writer.CurrentPath), Is.EqualTo("bpf-session-42.jsonl"));
            }
        }

        [Test]
        public void RotationKeepsTheNewestFilesOnly()
        {
            string directory = Path.Combine(tempDirectory, "rotate");
            using (BpfLogFileWriter writer = new BpfLogFileWriter(directory, "rotate", 2000, 3))
            {
                for (int i = 0; i < 300; i++)
                {
                    writer.Enqueue(Record("test.rotate." + i));
                    writer.Drain();
                }

                writer.Flush();
            }

            string[] files = Directory.GetFiles(directory, "bpf-*.jsonl");
            Assert.That(files.Length, Is.EqualTo(3));

            int[] parts = files.Select(PartNumber).OrderBy(number => number).ToArray();
            Assert.That(parts[2], Is.GreaterThan(3), "rotation should have produced more than three part files");
            Assert.That(parts, Is.EqualTo(new[] { parts[2] - 2, parts[2] - 1, parts[2] }));

            foreach (string file in files)
            {
                string[] lines = File.ReadAllLines(file);
                Assert.That(lines.Length, Is.GreaterThan(0));
                foreach (string line in lines)
                {
                    Assert.That(line, Does.StartWith("{"));
                    Assert.That(line, Does.EndWith("}"));
                    Assert.That(line, Does.Contain("\"event\":\"test.rotate."));
                }
            }
        }

        [Test]
        public void RotationResetsTheByteCounter()
        {
            string directory = Path.Combine(tempDirectory, "counter");
            using (BpfLogFileWriter writer = new BpfLogFileWriter(directory, "counter", 2000, 10))
            {
                for (int i = 0; i < 40; i++)
                {
                    writer.Enqueue(Record("test.counter." + i));
                    writer.Drain();
                }
            }

            Assert.That(Directory.GetFiles(directory, "bpf-*.jsonl").Length, Is.GreaterThan(1));
        }

        private static string ReadAllTextShared(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private static BpfLogRecord Record(string eventName)
        {
            BpfLogRecord record = new BpfLogRecord(BpfLogLevel.Info, BpfLogCategory.TexCore, eventName, "payload for " + eventName);
            record.SessionId = "test-session";
            return record;
        }

        private static int PartNumber(string path)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            int marker = name.IndexOf(".p", StringComparison.Ordinal);
            if (marker < 0)
            {
                return 1;
            }

            return int.Parse(name.Substring(marker + 2), CultureInfo.InvariantCulture);
        }
    }
}
