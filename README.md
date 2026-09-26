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

Sample run (.NET 10, Apple Silicon, workstation GC). Each record has an `int`, `string`, `decimal`, `double` and `bool`:

| Storage | Items | Build ms | Enumerate ms | GC pause during build ms | GCs gen0/1/2 | Managed heap | Native memory | Full GC with data live ms |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| List<T> (on heap) | 100,000 | 13 | 1 | 9 | 2/1/0 | 10.8 MB | 0.0 MB | 3.4 |
| OffHeapIEnumerable | 100,000 | 11 | 12 | 0 | 3/0/0 | 0.0 MB | 4.0 MB | 0.1 |
| List<T> (on heap) | 1,000,000 | 227 | 16 | 129 | 25/10/3 | 107.1 MB | 0.0 MB | 31.2 |
| OffHeapIEnumerable | 1,000,000 | 115 | 154 | 8 | 39/0/0 | 0.0 MB | 40.0 MB | 0.1 |
| List<T> (on heap) | 5,000,000 | 1,447 | 54 | 818 | 121/46/7 | 559.8 MB | 0.0 MB | 129.9 |
| OffHeapIEnumerable | 5,000,000 | 524 | 729 | 25 | 197/0/0 | 0.0 MB | 204.0 MB | 0.1 |

Off-heap storage uses about 2.7x less memory, builds faster, and adds nothing to GC work while it is alive; a full GC stays at ~0.1 ms instead of growing with the data set. The trade-off is enumeration: each pass deserializes a fresh object per item, so it is roughly 10x slower than walking a `List<T>`.