using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class MemoryCache
{
    delegate void ZeroMemoryDelegate(IntPtr ptr, int numBytes);

    // temporary buffers used to convert raw data <--> managed structure
    // so instead of allocating nX bytes every time, just reuse the same buffer :D
    static readonly Dictionary<int, IntPtr> s_Cache = new Dictionary<int, IntPtr>();

    static int s_CacheHits = 0; // number of times the cache was utilized
    static int s_CacheMisses = 0; // number of times new memory was allocated
    static int s_CacheBytesTotal = 0; // how much memory has been allocated
    static int s_CacheBytesSaved = 0; // how much memory has been reused

    static ZeroMemoryDelegate s_ZeroMemoryFunc = null;
    
    public static int Hits => s_CacheHits;
    public static int Misses => s_CacheMisses;

    public static int BytesTotal => s_CacheBytesTotal;
    public static int BytesSaved => s_CacheBytesSaved;

    static int GetKey(int size, short bucket = 0)
    {
        var key = size;

        if (bucket > 0)
        {
            key *= ~(bucket + (size * 12345));

            // ensure it's negative
            if (key > 0)
                key = -key;
        }

        return key;
    }
    
    // WARNING: SLOW!!! (not actually tested :P)
    public static bool IsCached(IntPtr ptr)
    {
        return s_Cache.ContainsValue(ptr);
    }

    public static bool IsCached(int size, short bucket = 0)
    {
        return s_Cache.ContainsKey(size);
    }

    public static void Dump(StringBuilder sb, bool fullDump = false)
    {
        sb.AppendLine("Memory cache information:");
        
        sb.AppendLine($" {s_CacheHits} hits, {s_CacheMisses} misses");
        sb.AppendLine($" {s_CacheBytesTotal} bytes total");
        sb.AppendLine($" {s_CacheBytesSaved} bytes saved");
        
        var count = s_Cache.Count;

        if (count > 0)
        {
            sb.AppendLine($" ({count} handles)");

            if (fullDump)
            {
                foreach (var kv in s_Cache)
                {
                    if (kv.Key > 0)
                    {
                        sb.AppendLine($" -> {kv.Value:X8} : {kv.Key} bytes");
                    }
                    else
                    {
                        sb.AppendLine($" -> {kv.Value:X8} : <reserved>");
                    }
                }
            }
        }
        else
        {
            sb.AppendLine(" (NO HANDLES)");
        }
    }

    static void ZeroMemory(IntPtr ptr, int size)
    {
        if (s_ZeroMemoryFunc == null)
            s_ZeroMemoryFunc = NativeMethods.RtlZeroMemory;

        s_ZeroMemoryFunc(ptr, size);
    }

    static IntPtr AllocPtr(int size)
    {
        try
        {
            // TODO: free up unmanaged memory as needed
            return Marshal.AllocHGlobal(size);
        }
        catch (OutOfMemoryException e)
        {
            throw new OutOfMemoryException($"Unable to allocate {size} bytes for the memory cache; out of memory!", e);
        }
    }
    
    // retrieves a cached block of memory
    public static IntPtr Alloc(int size, short bucket = 0, bool clean = true)
    {
        if (size < 0)
            throw new ArgumentOutOfRangeException(nameof(size), "Size cannot be negative.");

        IntPtr result = IntPtr.Zero;

        var key = GetKey(size, bucket);

        if (s_Cache.TryGetValue(key, out result))
        {
            if (clean)
            {
                // make sure it's squeaky clean ;)
                ZeroMemory(result, size);
            }

            s_CacheHits++;
            s_CacheBytesSaved += size;
        }
        else
        {
            // allocate some fresh memory
            result = AllocPtr(size);

            // cache me outside, how bout dat
            s_Cache.Add(key, result);

            s_CacheMisses++;
            s_CacheBytesTotal += size;
        }

        return result;
    }

    public static IntPtr Alloc(Type type, short bucket, out int size)
    {
        // TODO: cache type sizes
        size = Marshal.SizeOf(type);

        return Alloc(size, bucket);
    }

    public static IntPtr Alloc(Type type, out int size)
    {
        // TODO: cache type sizes
        size = Marshal.SizeOf(type);

        return Alloc(size, 0);
    }
    
    public static IntPtr Alloc<T>(short bucket = 0)
    {
        var typeSize = 0;

        return Alloc<T>(bucket, out typeSize);
    }

    public static IntPtr Alloc<T>(out int typeSize)
    {
        var type = typeof(T);

        return Alloc(type, 0, out typeSize);
    }

    public static IntPtr Alloc<T>(short bucket, out int typeSize)
    {
        var type = typeof(T);
        
        return Alloc(type, bucket, out typeSize);
    }

    public static bool Release(int size, short bucket = 0)
    {
        var key = GetKey(size, bucket);

        if (s_Cache.ContainsKey(key))
        {
            Marshal.FreeHGlobal(s_Cache[key]);

            s_Cache.Remove(key);
            return true;
        }

        return false;
    }
}