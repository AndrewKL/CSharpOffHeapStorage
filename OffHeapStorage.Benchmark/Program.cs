using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime;

namespace OffHeapStorage.Benchmark
{
    public class Record
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public double Score { get; set; }
        public bool Active { get; set; }
    }

    public class Result
    {
        public string Storage;
        public int Count;
        public double BuildMs;
        public double EnumerateMs;
        public long EnumerateAllocatedBytes;
        public double BuildGcPauseMs;
        public int Gen0, Gen1, Gen2;
        public long ManagedBytes;
        public long NativeBytes;
        public double FullGcMs;
    }

    public static class Program
    {
        const int FullGcSamples = 5;

        public static void Main(string[] args)
        {
            var sizes = args.Length > 0
                ? args.Select(int.Parse).ToArray()
                : new[] { 100_000, 1_000_000, 5_000_000 };

            Console.WriteLine($".NET {Environment.Version}, {(GCSettings.IsServerGC ? "server" : "workstation")} GC, " +
                              $"{(Debugger.IsAttached ? "debugger attached" : "no debugger")}");

            // Warm up both paths until tiered JIT has produced optimized code
            for (int i = 0; i < 5; i++)
            {
                Run("warmup", 100_000, items => items.ToList(), s => 0);
                Run("warmup", 100_000, items => new OffHeapIEnumerable<Record>(items), s => s.AllocatedBytes, dispose: true);
                Run("warmup", 100_000, items => new TypedOffHeapIEnumerable<Record>(items), s => s.AllocatedBytes, dispose: true);
            }

            var results = new List<Result>();
            foreach (var size in sizes)
            {
                results.Add(Run("List<T> (on heap)", size, items => items.ToList(), s => 0));
                results.Add(Run("OffHeapIEnumerable", size, items => new OffHeapIEnumerable<Record>(items), s => s.AllocatedBytes, dispose: true));
                results.Add(Run("TypedOffHeapIEnumerable", size, items => new TypedOffHeapIEnumerable<Record>(items), s => s.AllocatedBytes, dispose: true));
            }

            Console.WriteLine();
            Console.WriteLine("| Storage | Items | Build ms | Enumerate ms | Allocated while enumerating | GC pause during build ms | GCs gen0/1/2 | Managed heap | Native memory | Full GC with data live ms |");
            Console.WriteLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (var r in results)
            {
                Console.WriteLine($"| {r.Storage} | {r.Count:N0} | {r.BuildMs:N0} | {r.EnumerateMs:N0} | {Mb(r.EnumerateAllocatedBytes)} | {r.BuildGcPauseMs:N0} | " +
                                  $"{r.Gen0}/{r.Gen1}/{r.Gen2} | {Mb(r.ManagedBytes)} | {Mb(r.NativeBytes)} | {r.FullGcMs:N1} |");
            }
        }

        static IEnumerable<Record> Generate(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return new Record
                {
                    Id = i,
                    Name = "Item " + i,
                    Price = i * 0.01m,
                    Score = i / 3.0,
                    Active = (i & 1) == 0
                };
            }
        }

        static Result Run<TStorage>(string name, int count, Func<IEnumerable<Record>, TStorage> build,
            Func<TStorage, long> nativeBytes, bool dispose = false)
            where TStorage : IEnumerable<Record>
        {
            long baseline = LiveManagedBytes();
            int gen0 = GC.CollectionCount(0), gen1 = GC.CollectionCount(1), gen2 = GC.CollectionCount(2);
            var pause = GC.GetTotalPauseDuration();

            var sw = Stopwatch.StartNew();
            var storage = build(Generate(count));
            sw.Stop();
            var result = new Result
            {
                Storage = name,
                Count = count,
                BuildMs = sw.Elapsed.TotalMilliseconds,
                BuildGcPauseMs = (GC.GetTotalPauseDuration() - pause).TotalMilliseconds,
                Gen0 = GC.CollectionCount(0) - gen0,
                Gen1 = GC.CollectionCount(1) - gen1,
                Gen2 = GC.CollectionCount(2) - gen2,
            };

            long allocated = GC.GetAllocatedBytesForCurrentThread();
            sw.Restart();
            long checksum = 0;
            foreach (var r in storage)
                checksum += r.Id + r.Name.Length;
            sw.Stop();
            result.EnumerateMs = sw.Elapsed.TotalMilliseconds;
            result.EnumerateAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocated;

            result.ManagedBytes = LiveManagedBytes() - baseline;
            result.NativeBytes = nativeBytes(storage);

            // How long a blocking full GC takes while the data set is still reachable
            var samples = new double[FullGcSamples];
            for (int i = 0; i < samples.Length; i++)
            {
                sw.Restart();
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
                sw.Stop();
                samples[i] = sw.Elapsed.TotalMilliseconds;
            }
            Array.Sort(samples);
            result.FullGcMs = samples[samples.Length / 2];

            GC.KeepAlive(storage);
            if (dispose)
                ((IDisposable)storage).Dispose();
            if (checksum == 42)
                Console.WriteLine();   // keep the enumeration from being optimized away

            if (name != "warmup")
                Console.WriteLine($"  done: {name}, {count:N0} items");
            return result;
        }

        /// <summary>
        /// Bytes of reachable objects on the managed heap, measured after a compacting full collection
        /// </summary>
        static long LiveManagedBytes()
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            var info = GC.GetGCMemoryInfo();
            return info.HeapSizeBytes - info.FragmentedBytes;
        }

        static string Mb(long bytes)
        {
            return $"{bytes / (1024.0 * 1024.0):N1} MB";
        }
    }
}
