using System.Collections.Generic;

namespace Bpf.Diagnostics
{
    /// <summary>
    /// Fixed-size ring buffer of recent <see cref="BpfLogRecord"/> instances.
    /// It is the data source of the development diagnostics panel and the
    /// recent-log export, and it is only created for development builds.
    /// </summary>
    public sealed class BpfLogMemoryBuffer
    {
        private readonly BpfLogRecord[] records;
        private readonly object gate = new object();
        private int nextIndex;
        private int count;

        public BpfLogMemoryBuffer(int capacity)
        {
            records = new BpfLogRecord[capacity < 1 ? 1 : capacity];
        }

        public int Capacity
        {
            get { return records.Length; }
        }

        /// <summary>Records currently retained.</summary>
        public int Count
        {
            get
            {
                lock (gate)
                {
                    return count;
                }
            }
        }

        /// <summary>Adds a record, overwriting the oldest entry when full.</summary>
        public void Add(BpfLogRecord record)
        {
            if (record == null)
            {
                return;
            }

            lock (gate)
            {
                records[nextIndex] = record;
                nextIndex = (nextIndex + 1) % records.Length;
                if (count < records.Length)
                {
                    count++;
                }
            }
        }

        /// <summary>Drops every retained record.</summary>
        public void Clear()
        {
            lock (gate)
            {
                for (int i = 0; i < records.Length; i++)
                {
                    records[i] = null;
                }

                nextIndex = 0;
                count = 0;
            }
        }

        /// <summary>Oldest to newest copy of the retained records.</summary>
        public BpfLogRecord[] ToArray()
        {
            lock (gate)
            {
                BpfLogRecord[] result = new BpfLogRecord[count];
                int start = count == records.Length ? nextIndex : 0;
                for (int i = 0; i < count; i++)
                {
                    result[i] = records[(start + i) % records.Length];
                }

                return result;
            }
        }

        /// <summary>
        /// Newest-last list of records matching the filter, capped at
        /// <paramref name="max"/> entries (the newest ones win).
        /// </summary>
        public List<BpfLogRecord> Snapshot(BpfLogLevel minimumLevel, string category, string keyword, int max)
        {
            BpfLogRecord[] all = ToArray();
            List<BpfLogRecord> matches = new List<BpfLogRecord>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                BpfLogRecord record = all[i];
                if (record != null && record.Matches(minimumLevel, category, keyword))
                {
                    matches.Add(record);
                }
            }

            if (max > 0 && matches.Count > max)
            {
                matches.RemoveRange(0, matches.Count - max);
            }

            return matches;
        }
    }
}
