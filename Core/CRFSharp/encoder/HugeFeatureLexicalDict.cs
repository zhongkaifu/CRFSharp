using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using AdvUtils;

#if NO_SUPPORT_PARALLEL_LIB
#else
using System.Threading.Tasks;
using System.Collections.Concurrent;
#endif

namespace CRFSharp
{
    //定义内存的信息结构
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
        public MEMORYSTATUSEX()
        {
            this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    public sealed class FeatureFreq : IComparable<FeatureFreq>
    {
        public string strFeature;
        public long value;

        public int CompareTo(FeatureFreq fi)
        {
            return StringComparer.Ordinal.Compare(strFeature, fi.strFeature);
        }
    }

    public sealed class HugeFeatureLexicalDict : IFeatureLexicalDict
    {
        VarBigArray<FeatureFreq> arrayFeatureFreq;
        long arrayFeatureFreqSize;
        uint SHRINK_AVALI_MEM_LOAD;
        AdvUtils.Security.Cryptography.MD5 md5;

#if NO_SUPPORT_PARALLEL_LIB
#else
        ParallelOptions parallelOption;
#endif

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        public HugeFeatureLexicalDict(int thread_num, uint shrinkMemLoad)
        {
            SHRINK_AVALI_MEM_LOAD = shrinkMemLoad;
            arrayFeatureFreq = new VarBigArray<FeatureFreq>(1024 * 1024);
            arrayFeatureFreqSize = 0;
            md5 = new AdvUtils.Security.Cryptography.MD5();

#if NO_SUPPORT_PARALLEL_LIB
#else
            parallelOption = new ParallelOptions();
            parallelOption.MaxDegreeOfParallelism = thread_num;
#endif
        }

        public void Clear()
        {
            arrayFeatureFreq.Clear();
            arrayFeatureFreq = null;
        }


        public VarBigArray<FeatureFreq> featureFreq
        {
            get
            {
                return arrayFeatureFreq;
            }
        }

        public long Size
        {
            get
            {
                return arrayFeatureFreqSize;
            }
        }

#if NO_SUPPORT_PARALLEL_LIB
#else
        private long ParallelMerge(long startIndex, long endIndex, int freq)
        {
            long sizePerThread = (endIndex - startIndex + 1) / parallelOption.MaxDegreeOfParallelism;
            //Fistly, merge items in each block by parallel
            Parallel.For(0, parallelOption.MaxDegreeOfParallelism, parallelOption, i =>
            {
                Merge(startIndex + i * sizePerThread, startIndex + (i + 1) * sizePerThread - 1, 0);
            });

            //Secondly, merge all items
            return Merge(startIndex, endIndex, freq);
        }
#endif

        private void ForceCollectMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        //Merge same items in sorted list
        private long Merge(long startIndex, long endIndex, int freq)
        {
            long newEndIndex = startIndex;

            //Try to find first not null item
            while ((arrayFeatureFreq[startIndex] == null) &&
                startIndex <= endIndex)
            {
                startIndex++;
            }
            arrayFeatureFreq[newEndIndex] = arrayFeatureFreq[startIndex];
            for (long i = startIndex + 1; i <= endIndex; i++)
            {
                if (arrayFeatureFreq[i] == null)
                {
                    continue;
                }

                if (arrayFeatureFreq[newEndIndex].strFeature == arrayFeatureFreq[i].strFeature)
                {
                    //two same items, sum their value up
                    arrayFeatureFreq[newEndIndex].value += arrayFeatureFreq[i].value;
                    arrayFeatureFreq[i] = null;
                }
                else
                {
                    //two different items
                    if (arrayFeatureFreq[newEndIndex].value >= freq)
                    {
                        newEndIndex++;
                    }

                    arrayFeatureFreq[newEndIndex] = arrayFeatureFreq[i];
                    if (newEndIndex < i)
                    {
                        arrayFeatureFreq[i] = null;
                    }
                }
            }

            return newEndIndex;
        }

        //Generate feature string and its id list
        public void GenerateLexicalIdList(out IList<string> keyList, out IList<int> valList)
        {
            FixedBigArray<string> fixArrayKey = new FixedBigArray<string>(Size, 0);
            keyList = fixArrayKey;

            FixedBigArray<int> fixArrayValue = new FixedBigArray<int>(Size, 0);
            valList = fixArrayValue;

#if NO_SUPPORT_PARALLEL_LIB
            for (long i = 0;i < arrayFeatureFreqSize;i++)
#else
            Parallel.For(0, arrayFeatureFreqSize, parallelOption, i =>
#endif
            {
                fixArrayKey[i] = arrayFeatureFreq[i].strFeature;
                fixArrayValue[i] = (int)(arrayFeatureFreq[i].value);
            }
#if NO_SUPPORT_PARALLEL_LIB
#else
            );
#endif
        }

        Object thisLock = new object();
        //Generate feature id by NGram rules
        public long RegenerateFeatureId(BTreeDictionary<long, long> old2new, long ysize)
        {
            AdvUtils.Security.Cryptography.MD5 md5 = new AdvUtils.Security.Cryptography.MD5();
            long maxid_ = 0;

#if NO_SUPPORT_PARALLEL_LIB
            for (long i = 0;i < arrayFeatureFreqSize;i++)
#else
            Parallel.For(0, arrayFeatureFreqSize, parallelOption, i =>
#endif
            {
                //Generate new feature id
                long addValue = (arrayFeatureFreq[i].strFeature[0] == 'U' ? ysize : ysize * ysize);
                long oldValue = maxid_;
                while (System.Threading.Interlocked.CompareExchange(ref maxid_, oldValue + addValue, oldValue) != oldValue)
                {
                    oldValue = maxid_;
                }

                //Create existed and new feature ids mapping
                lock (thisLock)
                {
                    old2new.Add(
                        GetId(arrayFeatureFreq[i].strFeature),
                        oldValue);
                }

                arrayFeatureFreq[i].value = oldValue;
            }
#if NO_SUPPORT_PARALLEL_LIB
#else
            );
#endif
            return maxid_;
        }

        //Shrink entire list
        public void Shrink(int freq)
        {
            long newEndIndex = Shrink(0, arrayFeatureFreqSize - 1, freq);
            arrayFeatureFreqSize = newEndIndex + 1;
        }

        //Shrink item list
        private long Shrink(long startIndex, long endIndex, int freq)
        {
            Console.Write("Sorting...");
#if NO_SUPPORT_PARALLEL_LIB
            arrayFeatureFreq.Sort(startIndex, endIndex - startIndex + 1);
#else
            arrayFeatureFreq.Sort(startIndex, endIndex - startIndex + 1, parallelOption.MaxDegreeOfParallelism);
#endif

            Console.Write("Merging...");

#if NO_SUPPORT_PARALLEL_LIB
            long newEndIndex = Merge(startIndex, endIndex, freq);
#else
            long newEndIndex = ParallelMerge(startIndex, endIndex, freq);
#endif
            sortedEndIndex = newEndIndex;

            Console.WriteLine("Done!");

            ForceCollectMemory();

            return newEndIndex;
        }

        //Get feature string id
        private long GetId(string strFeature)
        {
            byte[] rawbytes = Encoding.UTF8.GetBytes(strFeature);
            
            lock (thisLock)
            {
                return md5.Compute64BitHash(rawbytes);
            }
        }

        private long sortedEndIndex = 0;
        private int ShrinkingLock = 0;
        private int AddLock = 0;
        //Add the feature string into list and get feature string id
        public long GetOrAddId(string strFeature)
        {
            while (ShrinkingLock == 1) { Thread.Sleep(5000); }

            //add item-adding lock
            Interlocked.Increment(ref AddLock);

            FeatureFreq newFFItem = new FeatureFreq();
            newFFItem.strFeature = strFeature;
            newFFItem.value = 1;
            if (sortedEndIndex > 0)
            {
                FeatureFreq ff = arrayFeatureFreq.BinarySearch(0, sortedEndIndex, newFFItem);
                if (ff != null)
                {
                    Interlocked.Increment(ref ff.value);
                    //free item-adding lock
                    Interlocked.Decrement(ref AddLock);
                    return GetId(strFeature);
                }
            }

            long oldValue = Interlocked.Increment(ref arrayFeatureFreqSize) - 1;
            arrayFeatureFreq[oldValue] = newFFItem;

            //free item-adding lock
            Interlocked.Decrement(ref AddLock);

            //Check whether shrink process should be started
            uint memoryLoad = 0;
            if (oldValue % 10000000 == 0)
            {
                MEMORYSTATUSEX msex = new MEMORYSTATUSEX();
                GlobalMemoryStatusEx(msex);
                memoryLoad = msex.dwMemoryLoad;
            }

            if (memoryLoad >= SHRINK_AVALI_MEM_LOAD)
            {
                if (Interlocked.CompareExchange(ref ShrinkingLock, 1, 0) == 0)
                {
                    //Double check whether shrink should be started
                    MEMORYSTATUSEX msex = new MEMORYSTATUSEX();
                    GlobalMemoryStatusEx(msex);
                    if (msex.dwMemoryLoad >= SHRINK_AVALI_MEM_LOAD)
                    {
                        while (AddLock != 0) { Thread.Sleep(1000); }

                        DateTime startDT = DateTime.Now;
                        Console.WriteLine("Begin to shrink [Feature Size: {0}]...", arrayFeatureFreqSize);
                        long newArrayFeatureFreqSize = Shrink(0, arrayFeatureFreqSize - 1, 0) + 1;

                        GlobalMemoryStatusEx(msex);
                        if (msex.dwMemoryLoad >= SHRINK_AVALI_MEM_LOAD - 1)
                        {
                            //Still have enough available memory, raise shrink threshold
                            SHRINK_AVALI_MEM_LOAD = msex.dwMemoryLoad + 1;
                            if (SHRINK_AVALI_MEM_LOAD >= 100)
                            {
                                //if use more than 100% memory, the performance will extremely reduce
                                SHRINK_AVALI_MEM_LOAD = 100;
                            }
                        }

                        arrayFeatureFreqSize = newArrayFeatureFreqSize;
                        TimeSpan ts = DateTime.Now - startDT;
                        Console.WriteLine("Shrink has been done!");
                        Console.WriteLine("[Feature Size:{0}, TimeSpan:{1}, Next Shrink Rate:{2}%]", arrayFeatureFreqSize, ts, SHRINK_AVALI_MEM_LOAD);
                    }

                    Interlocked.Decrement(ref ShrinkingLock);
                }
            }
            return GetId(strFeature);
        }
    }
}
