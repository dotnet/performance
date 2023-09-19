// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is a simple simulation of a cache to test ECO.
// We are trying to create the following situations:
//
// Uneven fragmentation in gen2 to verify that ECO picks the more fragmented regions to compact.
// This is achieved by the user threads replacing cache entries at random and an 
// evict thread periodically evicts entries at bulk.
//
// References between objects to test ECO's construction phase.
// This is achieved by having each cache entry carry a string that we have to construct that
// represents the last access time.
// Since we are doing this all the time it also tests the concurrent construction.
// 
// Create pinning with the cached items sometimes to test pinning.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Text;
using System.Diagnostics;

namespace CacheSim
{
    sealed class Rand
    {
        /* Generate Random numbers
         */
        private int x = 0;

        public int getRand()
        {
            x = (314159269 * x + 278281) & 0x7FFFFFFF;
            return x;
        }

        // obtain random number in the range 0 .. r-1
        public int getRand(int r)
        {
            // require r >= 0
            int x = (int)(((long)getRand() * r) >> 31);
            return x;
        }

        // obtain random number in the range min .. max-1
        public int getRand(int min, int max)
        {
            // require r >= 0
            int x = (int)(((long)getRand() * (max - min)) >> 31);
            x += min;
            return x;
        }

        public double getFloat()
        {
            return (double)getRand() / (double)0x7FFFFFFF;
        }

    };

    struct OnDiskItem
    {
        public int iId;
        // This is just a random number we create that simulates the 
        // data this item carries.
        public long lData;
    }

    class OnDiskData
    {
        OnDiskItem[] arrItems;

        public void CreateOnDiskData(int iTotalSize)
        {
            OnDiskItem temp = new OnDiskItem();
            int iSize = Marshal.SizeOf(temp);
            int iNumOfElements = iTotalSize / iSize;
            Console.WriteLine("Creating {0} items on disk, each item {1} bytes", iNumOfElements, iSize);

            arrItems = new OnDiskItem[iNumOfElements];

            for (int i = 0; i < arrItems.Length; i++)
            {
                arrItems[i].iId = i + 100;
                arrItems[i].lData = 1024 * 1024 * 1024 + i;
            }
        }

        public int GetTotalItems()
        {
            return arrItems.Length;
        }

        public OnDiskItem GetItem(int iIndex)
        {
            return arrItems[iIndex];
        }
    }

    class CachedItem
    {
        public int iId;
        string strData;
        string strAccessTime;
        GCHandle pin;
        bool fIsPinned;

        public CachedItem(OnDiskItem diskItem)
        {
            iId = diskItem.iId;
            strData = diskItem.lData.ToString();
            fIsPinned = false;
        }

        public void RecordAccess()
        {
            strAccessTime = DateTime.Now.ToString("HH:mm:ss tt");
        }

        public void PinUserData()
        {
            if (fIsPinned == false)
            {
                pin = GCHandle.Alloc(strData, GCHandleType.Pinned);
                fIsPinned = true;
            }
        }

        public void UnpinUserData()
        {
            if (fIsPinned)
            {
                pin.Free();
                fIsPinned = false;
            }
        }

        public string GetUserData()
        {
            return strData;
        }
    }

    // When we evict an entry we randomly choose an entry to evict.
    class InMemoryCache
    {
        CachedItem[] CachedItems;
        OnDiskData DiskItems;
        int iReplacedItems;
        int iCachedCount;
        static Rand r = new Rand();

        // I am not recording the access time when prepopulating the cache.
        public void InitCache(OnDiskData diskData, int iPercent)
        {
            DiskItems = diskData;
            int iDiskItemsCount = diskData.GetTotalItems();
            int iCachedItemsCount = iDiskItemsCount * iPercent / 100;
            CachedItems = new CachedItem[iCachedItemsCount];
            iReplacedItems = 0;

            int iAdded = 0;

            while (iAdded < iCachedItemsCount)
            {
                OnDiskItem temp = diskData.GetItem(r.getRand(iDiskItemsCount));
                CachedItem item = new CachedItem(temp);
                CachedItems[iAdded] = item;
                iAdded++;
            }

            Console.WriteLine("Cached initialized: {0} entries, heap size is {1} bytes",
                iCachedItemsCount, GC.GetTotalMemory(true));

            iCachedCount = iCachedItemsCount;
        }

        void EvictEntry(int iIndex)
        {
            if (CachedItems[iIndex] != null)
            {
                CachedItems[iIndex].UnpinUserData();
                CachedItems[iIndex] = null;
                iCachedCount--;
            }
        }

        CachedItem AddEntry(int iIndex, OnDiskItem item)
        {
            CachedItem itemToCache = new CachedItem(item);
            CachedItems[iIndex] = itemToCache;
            CachedItems[iIndex].RecordAccess();
            iCachedCount++;
            return itemToCache;
        }

        // If the item is not in the cache, add it.
        // Evict an entry if the cache is full.
        public CachedItem GetItem(int iId)
        {
            CachedItem item = null;

            for (int i = 0; i < CachedItems.Length; i++)
            {
                if (CachedItems[i] != null)
                {
                    if (CachedItems[i].iId == iId)
                    {
                        CachedItems[i].RecordAccess();
                        item = CachedItems[i];
                        break;
                    }
                }
            }

            if (item == null)
            {
                int iItemToReplace = r.getRand(CachedItems.Length);
                EvictEntry(iItemToReplace);

                OnDiskItem temp = DiskItems.GetItem(iId);
                item = AddEntry(iItemToReplace, temp);
                iReplacedItems++;
            }

            return item;
        }

        public string GetUserData(int iId, bool fPin)
        {
            string strData;

            lock (this)
            {
                CachedItem item = GetItem(iId);
                strData = item.GetUserData();

                if (fPin)
                {
                    item.PinUserData();
                }
            }

            return strData;
        }

        public int GetTotalItems()
        {
            return CachedItems.Length;
        }

        public int GetReplacedItemCount()
        {
            return iReplacedItems;
        }

        public void RemoveChunk(int iPercentage)
        {
            int iCountToRemove = (int)(((long)iCachedCount * (long)iPercentage) / 100);
            Console.WriteLine("Cached {0} items, evicting {1}", iCachedCount, iCountToRemove);

            int iRemovedCount = 0;

            lock (this)
            {
                int iIndex = r.getRand(CachedItems.Length);

                while ((iIndex < CachedItems.Length) && (iRemovedCount < iCountToRemove))
                {
                    if (CachedItems[iIndex] != null)
                    {
                        CachedItems[iIndex].UnpinUserData();
                        CachedItems[iIndex] = null;
                        iRemovedCount++;
                    }

                    iIndex++;

                    if ((iRemovedCount < iCountToRemove) && (iIndex == CachedItems.Length))
                    {
                        iIndex = 0;
                    }
                }
            }
        }
    }

    class EvictThread
    {
        InMemoryCache cache;
        int iPercentage;
        int iEvictIntervalMS;
        static Rand r = new Rand();

        // percentage is the percentage of items we want to remove from the cache.
        public EvictThread(InMemoryCache c, int percent, int intervalMillSeconds)
        {
            cache = c;
            iPercentage = percent;
            iEvictIntervalMS = intervalMillSeconds;
        }

        // We remove by chunks. We can also remove randomly that many items. 
        public void Evict()
        {
            while (true)
            {
                Thread.Sleep(iEvictIntervalMS);

                cache.RemoveChunk(iPercentage);
            }
        }
    }

    class StatsThread
    {
        int iIntervalMS;
        InMemoryCache cache;
        int iReplacedLast;

        public StatsThread(int iIntervalShowStatsMS, InMemoryCache c)
        {
            iIntervalMS = iIntervalShowStatsMS;
            cache = c;
            iReplacedLast = c.GetReplacedItemCount();
        }

        public void ShowStats()
        {
            while (true)
            {
                Thread.Sleep(iIntervalMS);

                int iReplacedCurrent = cache.GetReplacedItemCount();
                Console.WriteLine("{0} items replaced after {1}ms", (iReplacedCurrent - iReplacedLast), iIntervalMS);
                Console.WriteLine("gen0: {0, 5:N0}, gen1: {1, 5:N0}; gen2: {2, 5:N0}, heap size: {3, 10:N0} bytes",
                    GC.CollectionCount(0),
                    GC.CollectionCount(1),
                    GC.CollectionCount(2),
                    GC.GetTotalMemory(false));

                iReplacedLast = iReplacedCurrent;
            }
        }
    }

    // User thread generates a random index and see if it exists in the cache, if not, add it to the cache.
    class UserThread
    {
        static Rand r = new Rand();
        Stopwatch stopwatch = new Stopwatch();
        OnDiskData DiskItems;
        InMemoryCache cache;
        int iTemporaryBytes;
        int iPinningPercent;

        public UserThread(InMemoryCache c, OnDiskData diskData, int iTempBytes, int iPPercent)
        {
            DiskItems = diskData;
            cache = c;
            iTemporaryBytes = iTempBytes;
            iPinningPercent = iPPercent;
        }

        int AllocateTempHalf()
        {
            int iAllocate;
            int iAllocateMin = 70 * 1024;
            if ((iTemporaryBytes / 2) < iAllocateMin)
            {
                iAllocateMin = 0;
            }

            iAllocate = r.getRand(iAllocateMin, iTemporaryBytes / 2);
            byte[] temp = new byte[iAllocate];
            return iAllocate;
        }

        int AllocateTempQuater()
        {
            int iAllocate = r.getRand(iTemporaryBytes / 4);
            byte[] temp = new byte[iAllocate];
            return iAllocate;
        }

        void AllocateRest(int iAlreadyAllocated)
        {
            int iRest = iTemporaryBytes - iAlreadyAllocated;
            int iLargeObjectSize = 85000;

            if (iRest > iLargeObjectSize)
            {
                iRest -= iLargeObjectSize;
                byte[] temp = new byte[iLargeObjectSize];
            }

            if (iRest > iLargeObjectSize)
            {
                int iSmallObjectSize = r.getRand(iLargeObjectSize);
                byte[] temp = new byte[iSmallObjectSize];
                iRest -= iSmallObjectSize;
            }

            byte[] tempRest = new byte[iRest];
        }

        public void DoWork()
        {
            stopwatch.Reset();
            stopwatch.Start();
            long lElapsedMS = 0;
            ulong ulRequests = 1;

            // We pin every Nth item.
            int iNth = Int32.MaxValue;

            if (iPinningPercent != 0)
            {
                iNth = 100 / iPinningPercent;
            }

            try
            {
                while (true)
                {
                    bool fPin = false;
                    int iAllocated = 0;

                    iAllocated += AllocateTempHalf();

                    int iItemToLookUp = r.getRand(DiskItems.GetTotalItems());

                    if ((ulRequests % (ulong)iNth) == 0)
                    {
                        //Console.WriteLine("pinning on request {0}!", ulRequests);
                        fPin = true;
                    }

                    string strData = cache.GetUserData(iItemToLookUp, fPin);

                    // Simulating using the data from the item.
                    StringBuilder sb = new StringBuilder(strData);
                    sb.Append(ulRequests);

                    iAllocated += AllocateTempQuater();

                    AllocateRest(iAllocated);

                    ulRequests++;

                    if (ulRequests % 100 == 0)
                    {
                        Console.WriteLine("{0} requests executed, took {1}ms",
                            ulRequests,
                            stopwatch.ElapsedMilliseconds - lElapsedMS);

                        lElapsedMS = stopwatch.ElapsedMilliseconds;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("!!!!!!" + ex.ToString());
            }

            Console.WriteLine("----------------Exiting user thread----------------");
        }
    }

    class CacheSim
    {
        static int iUserThreads = 4;
        static int iTemporaryBytes = 160 * 1024;
        static int iCachedPercent = 80;
        static int iPinningPercent = 10;
        static int iEvictPercent = 10;
        static int iEvictIntervalMS = 5000;
        static int iOnDishSize = 1024 * 1024 * 10;
        static int iIntervalShowStatsMS = 5000;

        public static void Main2(string[] args)
        {
            int iArgIndex = 0;
            while (iArgIndex < (args.Length - 1))
            {
                string strOption = args[iArgIndex].Trim();
                if (strOption.StartsWith("-cp"))
                {
                    iCachedPercent = int.Parse(args[++iArgIndex].Trim());
                }
                else if (strOption.StartsWith("-ds"))
                {
                    iOnDishSize = int.Parse(args[++iArgIndex].Trim());
                }
                else if (strOption.StartsWith("-ut"))
                {
                    iUserThreads = int.Parse(args[++iArgIndex].Trim());
                }
                else if (strOption.StartsWith("-tb"))
                {
                    iTemporaryBytes = int.Parse(args[++iArgIndex].Trim());
                }
                else if (strOption.StartsWith("-pp"))
                {
                    iPinningPercent = int.Parse(args[++iArgIndex].Trim());
                }
                else if (strOption.StartsWith("-ep"))
                {
                    iEvictPercent = int.Parse(args[++iArgIndex].Trim());
                }
                else if (strOption.StartsWith("-ei"))
                {
                    iEvictIntervalMS = int.Parse(args[++iArgIndex].Trim());
                }
                else if (strOption.StartsWith("-st"))
                {
                    iIntervalShowStatsMS = int.Parse(args[++iArgIndex].Trim());
                }

                iArgIndex++;
            }

            Console.WriteLine("---------Options for running this test---------");
            Console.WriteLine("OnDisk size is {0}, Caching {1}%", iOnDishSize, iCachedPercent);
            // Instead of actually loading data from the disk we just create
            // it in memory - this way we can eliminate the variability
            // created with the APIs that do the loading.
            OnDiskData diskData = new OnDiskData();
            diskData.CreateOnDiskData(iOnDishSize);

            // The cache is prepopulated by randomly adding some percentage
            // of the on disk data, but in the format that's actually 
            // represented in the cache (which is bigger than the on disk
            // format since usually people store in a more compact form).
            // By default we add 80% of the on disk data to cache.
            InMemoryCache cache = new InMemoryCache();
            cache.InitCache(diskData, iCachedPercent);

            Console.WriteLine("{0} user threads, each iteration allocates {1} bytes temporary memory, pinning {2}% entries",
                iUserThreads, iTemporaryBytes, iPinningPercent);

            UserThread threadUser;
            ThreadStart ts;
            Thread[] threadsUser = new Thread[iUserThreads];

            for (int i = 0; i < iUserThreads; i++)
            {
                threadUser = new UserThread(cache, diskData, iTemporaryBytes, iPinningPercent);
                ts = new ThreadStart(threadUser.DoWork);
                threadsUser[i] = new Thread(ts);
                threadsUser[i].Start();
            }

            Console.WriteLine("Eviction thread evicts {0}% every {1}ms", iEvictPercent, iEvictIntervalMS);
            EvictThread threadEvict = new EvictThread(cache, iEvictPercent, iEvictIntervalMS);
            ThreadStart tsEvict = new ThreadStart(threadEvict.Evict);
            Thread tEvict = new Thread(tsEvict);
            tEvict.Start();

            Console.WriteLine("Stats thread shows stats {0}ms", iIntervalShowStatsMS);
            StatsThread threadStats = new StatsThread(iIntervalShowStatsMS, cache);
            ThreadStart tsStats = new ThreadStart(threadStats.ShowStats);
            Thread tStats = new Thread(tsStats);
            tStats.Start();

            Console.WriteLine("-----------------------------------------------");
        }
    }
}
