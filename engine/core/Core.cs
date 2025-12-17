/*
 * PyFlare Engine - Core System
 * Foundation: Memory management, object system, scene tree
 * Optimized for weak hardware (256-512MB RAM target)
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PyFlare.Engine.Core
{
    // ============================================================================
    // MEMORY POOL SYSTEM
    // ============================================================================
    
    /// <summary>
    /// Fixed-size memory pool to prevent fragmentation on weak hardware
    /// </summary>
    public class MemoryPool
    {
        private int blockSize;
        private Queue<byte[]> freeBlocks;
        private HashSet<IntPtr> allocatedBlocks;
        private int totalAllocated;

        public MemoryPool(int blockSize, int initialBlocks = 32)
        {
            this.blockSize = blockSize;
            this.freeBlocks = new Queue<byte[]>(initialBlocks);
            this.allocatedBlocks = new HashSet<IntPtr>();
            this.totalAllocated = 0;

            GrowPool(initialBlocks);
        }

        private void GrowPool(int count)
        {
            for (int i = 0; i < count; i++)
            {
                byte[] block = new byte[blockSize];
                freeBlocks.Enqueue(block);
            }
        }

        public byte[] Allocate()
        {
            if (freeBlocks.Count == 0)
            {
                int growSize = Math.Max(8, allocatedBlocks.Count / 2);
                GrowPool(growSize);
            }

            byte[] block = freeBlocks.Dequeue();
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(block, 0);
            allocatedBlocks.Add(ptr);
            totalAllocated += blockSize;
            return block;
        }

        public void Free(byte[] block)
        {
            IntPtr ptr = Marshal.UnsafeAddrOfPinnedArrayElement(block, 0);
            if (allocatedBlocks.Contains(ptr))
            {
                allocatedBlocks.Remove(ptr);
                totalAllocated -= blockSize;
                Array.Clear(block, 0, block.Length);
                freeBlocks.Enqueue(block);
            }
        }

        public int GetTotalAllocated() => totalAllocated;
        public int GetFreeBlocks() => freeBlocks.Count;
        public int GetAllocatedBlocks() => allocatedBlocks.Count;
    }

    /// <summary>
    /// Central memory manager with multiple pools for different allocation sizes
    /// </summary>
    public class MemoryManager
    {
        private static readonly int[] POOL_SIZES = { 64, 256, 1024, 4096, 16384 };
        private Dictionary<int, MemoryPool> pools;
        private int totalAllocations;
        private long peakMemory;

        private static MemoryManager instance;
        public static MemoryManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new MemoryManager();
                return instance;
            }
        }

        private MemoryManager()
        {
            pools = new Dictionary<int, MemoryPool>();
            foreach (int size in POOL_SIZES)
            {
                pools[size] = new MemoryPool(size);
            }
            totalAllocations = 0;
            peakMemory = 0;
        }

        public byte[] Allocate(int size)
        {
            int poolSize = -1;
            foreach (int ps in POOL_SIZES)
            {
                if (ps >= size)
                {
                    poolSize = ps;
                    break;
                }
            }

            if (poolSize == -1)
            {
                Console.WriteLine($"Warning: Large allocation ({size} bytes) bypassing pools");
                return new byte[size];
            }

            totalAllocations++;
            UpdatePeakMemory();
            return pools[poolSize].Allocate();
        }

        public void Free(byte[] block, int poolSize)
        {
            if (pools.ContainsKey(poolSize))
            {
                pools[poolSize].Free(block);
                totalAllocations--;
            }
        }

        private void UpdatePeakMemory()
        {
            long current = GetTotalMemoryUsed();
            if (current > peakMemory)
                peakMemory = current;
        }

        public long GetTotalMemoryUsed()
        {
            long total = 0;
            foreach (var pool in pools.Values)
            {
                total += pool.GetTotalAllocated();
            }
            return total;
        }

        public void PrintMemoryReport()
        {
            Console.WriteLine("\n=== PyFlare Memory Report ===");
            Console.WriteLine($"Total Allocations: {totalAllocations}");
            Console.WriteLine($"Memory Used: {GetTotalMemoryUsed() / 1024.0:F2} KB");
            Console.WriteLine($"Peak Memory: {peakMemory / 1024.0:F2} KB");
            Console.WriteLine("\nPool Statistics:");

            foreach (var kvp in pools)
            {
                int size = kvp.Key;
                var pool = kvp.Value;
                int allocated = pool.GetAllocatedBlocks();
                int free = pool.GetFreeBlocks();
                float efficiency = (allocated + free) > 0 
                    ? (allocated / (float)(allocated + free)) * 100 
                    : 0;

                Console.WriteLine($"  {size,5} byte pool: {allocated,3} used, {free,3} free, {efficiency:F1}% efficiency");
            }
            Console.WriteLine("==============================\n");
        }
    }

    // ============================================================================
    // OBJECT SYSTEM
    // ============================================================================

    /// <summary>
    /// Base class for all PyFlare engine objects
    /// Provides reference counting, signals, and metadata
    /// </summary>
    public class PyFlareObject
    {
        private static int nextObjectId = 1;
        private static Dictionary<string, Type> classRegistry = new Dictionary<string, Type>();
        private static Dictionary<int, WeakReference> objectDatabase = new Dictionary<int, WeakReference>();

        protected int objectId;
        protected int refCount;
        protected Dictionary<string, List<Action<object[]>>> signals;
        protected Dictionary<string, object> metadata;
        protected object attachedScript;

        public PyFlareObject()
        {
            objectId = nextObjectId++;
            refCount = 1;
            signals = new Dictionary<string, List<Action<object[]>>>();
            metadata = new Dictionary<string, object>();
            attachedScript = null;

            objectDatabase[objectId] = new WeakReference(this);
        }

        // Class registry
        public static void RegisterClass(Type type)
        {
            classRegistry[type.Name] = type;
        }

        public static Type GetClass(string className)
        {
            return classRegistry.ContainsKey(className) ? classRegistry[className] : null;
        }

        // Reference counting
        public void Reference()
        {
            refCount++;
        }

        public void Unreference()
        {
            refCount--;
            if (refCount <= 0)
                Cleanup();
        }

        public int GetReferenceCount() => refCount;

        protected virtual void Cleanup()
        {
            signals.Clear();
            metadata.Clear();
            if (objectDatabase.ContainsKey(objectId))
                objectDatabase.Remove(objectId);
        }

        // Signal system
        public void AddSignal(string signalName)
        {
            if (!signals.ContainsKey(signalName))
                signals[signalName] = new List<Action<object[]>>();
        }

        public void Connect(string signalName, Action<object[]> callback)
        {
            if (!signals.ContainsKey(signalName))
                AddSignal(signalName);

            if (!signals[signalName].Contains(callback))
                signals[signalName].Add(callback);
        }

        public void Disconnect(string signalName, Action<object[]> callback)
        {
            if (signals.ContainsKey(signalName))
                signals[signalName].Remove(callback);
        }

        public void EmitSignal(string signalName, params object[] args)
        {
            if (signals.ContainsKey(signalName))
            {
                foreach (var callback in signals[signalName])
                {
                    try
                    {
                        callback(args);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"Error in signal '{signalName}': {e.Message}");
                    }
                }
            }
        }

        public List<string> GetSignalList()
        {
            return new List<string>(signals.Keys);
        }

        // Metadata
        public void SetMeta(string key, object value)
        {
            metadata[key] = value;
        }

        public object GetMeta(string key, object defaultValue = null)
        {
            return metadata.ContainsKey(key) ? metadata[key] : defaultValue;
        }

        public bool HasMeta(string key) => metadata.ContainsKey(key);

        // Properties
        public int GetObjectId() => objectId;
        public string GetClassName() => GetType().Name;

        // Static utilities
        public static int GetObjectCount() => objectDatabase.Count;
        public static List<PyFlareObject> GetAllObjects()
        {
            List<PyFlareObject> result = new List<PyFlareObject>();
            foreach (var wr in objectDatabase.Values)
            {
                if (wr.IsAlive)
                    result.Add(wr.Target as PyFlareObject);
            }
            return result;
        }
    }

    // ============================================================================
    // RESOURCE SYSTEM
    // ============================================================================

    /// <summary>
    /// Base class for all loadable resources (textures, sounds, meshes, etc.)
    /// </summary>
    public class Resource : PyFlareObject
    {
        protected string resourcePath;
        protected bool isLoaded;
        protected long memoryUsage;

        public Resource()
        {
            resourcePath = "";
            isLoaded = false;
            memoryUsage = 0;
        }

        public virtual void Load(string path)
        {
            resourcePath = path;
            isLoaded = true;
        }

        public virtual void Unload()
        {
            isLoaded = false;
            memoryUsage = 0;
        }

        public string GetPath() => resourcePath;
        public bool IsLoaded() => isLoaded;
        public long GetMemoryUsage() => memoryUsage;

        protected override void Cleanup()
        {
            if (isLoaded)
                Unload();
            base.Cleanup();
        }
    }

    /// <summary>
    /// Resource loader with caching and lazy loading
    /// </summary>
    public class ResourceLoader
    {
        private static Dictionary<string, WeakReference> resourceCache = new Dictionary<string, WeakReference>();

        public static T Load<T>(string path) where T : Resource, new()
        {
            // Check cache first
            if (resourceCache.ContainsKey(path))
            {
                WeakReference wr = resourceCache[path];
                if (wr.IsAlive)
                {
                    T cached = wr.Target as T;
                    if (cached != null)
                    {
                        cached.Reference();
                        return cached;
                    }
                }
            }

            // Load new resource
            T resource = new T();
            resource.Load(path);
            resourceCache[path] = new WeakReference(resource);
            return resource;
        }

        public static void ClearCache()
        {
            resourceCache.Clear();
        }

        public static int GetCacheSize() => resourceCache.Count;
    }

    // ============================================================================
    // CORE ENGINE CLASS
    // ============================================================================

    /// <summary>
    /// Main engine core - manages initialization, main loop, and shutdown
    /// </summary>
    public class Engine
    {
        private static Engine instance;
        public static Engine Instance
        {
            get
            {
                if (instance == null)
                    instance = new Engine();
                return instance;
            }
        }

        private bool isRunning;
        private double targetFPS;
        private double deltaTime;
        private long frameCount;

        private Engine()
        {
            isRunning = false;
            targetFPS = 60.0;
            deltaTime = 0.0;
            frameCount = 0;
        }

        public void Initialize()
        {
            Console.WriteLine("PyFlare Engine Initializing...");
            
            // Initialize memory manager
            var memMgr = MemoryManager.Instance;
            
            // Register core classes
            PyFlareObject.RegisterClass(typeof(PyFlareObject));
            PyFlareObject.RegisterClass(typeof(Resource));
            
            Console.WriteLine("PyFlare Engine Initialized");
            isRunning = true;
        }

        public void Shutdown()
        {
            Console.WriteLine("PyFlare Engine Shutting Down...");
            
            isRunning = false;
            
            // Clear resource cache
            ResourceLoader.ClearCache();
            
            // Print final memory report
            MemoryManager.Instance.PrintMemoryReport();
            
            Console.WriteLine("PyFlare Engine Shutdown Complete");
        }

        public void Update(double dt)
        {
            deltaTime = dt;
            frameCount++;
        }

        public bool IsRunning() => isRunning;
        public double GetDeltaTime() => deltaTime;
        public long GetFrameCount() => frameCount;
        public double GetTargetFPS() => targetFPS;
        public void SetTargetFPS(double fps) => targetFPS = fps;
    }
}
