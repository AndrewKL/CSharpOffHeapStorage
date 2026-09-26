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
* Data lives in `MemoryTributary`, a stream backed by a list of 64 KB `byte[]` blocks. The blocks are still managed memory, but each one stays under the Large Object Heap threshold and the GC sees a few thousand byte arrays instead of millions of objects.