# README #

This is a library to test to concept of off heap storage.  calling ToList() on large IEnumerable makes the garbage collector and the allocater unhappy.  This library pushes this data into an unmanaged memory stream. 

### How do I get set up? ###

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (on macOS: `brew install dotnet`).

```
dotnet build
dotnet test
```

### Notes ###

* Supported property types: `int`, `bool`, `decimal`, `float`, `double`, `string` (including `null`). Other properties are skipped.
* `T` must be a class with a public parameterless constructor; only public read/write instance properties are stored.
* Data lives in `MemoryTributary`, a stream backed by a list of 1 MB blocks allocated with `NativeMemory`, outside the GC heap. The garbage collector never scans or moves them.
* `OffHeapIEnumerable<T>` is `IDisposable`: dispose it (e.g. `using var storage = ...`) to free the native memory. A finalizer frees it as a fallback, but the GC doesn't know how much native memory is held, so don't rely on that.

### Benchmark ###

```
dotnet run -c Release --project OffHeapStorage.Benchmark -- 100000 1000000 5000000
```

There are two off-heap variants with the same API and byte format:

* `OffHeapIEnumerable<T>` uses `Serializer<T>`, which reads and writes each property through a boxed `object` getter/setter delegate.
* `TypedOffHeapIEnumerable<T>` uses `TypedSerializer<T>`, which compiles one reader and one writer per type with expression trees, so nothing is boxed.

Sample run (.NET 10, Apple Silicon, workstation GC). Each record has an `int`, `string`, `decimal`, `double` and `bool`:

| Storage | Items | Build ms | Enumerate ms | Allocated while enumerating | GC pause during build ms | GCs gen0/1/2 | Managed heap | Native memory | Full GC with data live ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| List<T> (on heap) | 100,000 | 11 | 1 | 0.0 MB | 7 | 2/1/0 | 10.8 MB | 0.0 MB | 3.4 |
| OffHeapIEnumerable | 100,000 | 11 | 10 | 19.8 MB | 1 | 3/0/0 | 0.0 MB | 4.0 MB | 0.1 |
| TypedOffHeapIEnumerable | 100,000 | 8 | 8 | 9.8 MB | 0 | 2/0/0 | 0.0 MB | 4.0 MB | 0.1 |
| List<T> (on heap) | 1,000,000 | 230 | 19 | 0.0 MB | 139 | 25/10/3 | 107.1 MB | 0.0 MB | 37.4 |
| OffHeapIEnumerable | 1,000,000 | 123 | 135 | 198.3 MB | 10 | 39/0/0 | 0.0 MB | 40.0 MB | 0.1 |
| TypedOffHeapIEnumerable | 1,000,000 | 109 | 133 | 99.1 MB | 9 | 22/0/0 | 0.0 MB | 40.0 MB | 1.1 |
| List<T> (on heap) | 5,000,000 | 1,503 | 84 | 0.0 MB | 916 | 120/46/6 | 559.8 MB | 0.0 MB | 123.6 |
| OffHeapIEnumerable | 5,000,000 | 546 | 749 | 991.7 MB | 29 | 197/0/0 | 0.0 MB | 204.0 MB | 0.1 |
| TypedOffHeapIEnumerable | 5,000,000 | 476 | 392 | 495.8 MB | 23 | 114/0/0 | 0.0 MB | 204.0 MB | 0.2 |

Off-heap storage uses about 2.7x less memory, builds faster, and adds nothing to GC work while it is alive; a full GC stays at ~0.1 ms instead of growing with the data set. The trade-off is enumeration: each pass deserializes a fresh object per item. The typed serializer halves the garbage produced while enumerating (only the record and its string remain) and is 1.3-1.9x faster than the boxed one across runs, but still several times slower than walking a `List<T>`; the remaining cost is mostly per-read `Stream` overhead.
