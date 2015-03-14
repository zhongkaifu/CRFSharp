using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;

#if NO_SUPPORT_PARALLEL_LIB
#else
using System.Collections.Concurrent;

namespace CRFSharp
{
    //
    // An orderable dynamic partitioner for lists
    //
    public class OrderableListPartitioner<TSource> : OrderablePartitioner<TSource>
    {
        private readonly IList<TSource> m_input;

        public OrderableListPartitioner(IList<TSource> input)
            : base(true, false, true)
        {
            m_input = input;
        }

        // Must override to return true.
        public override bool SupportsDynamicPartitions
        {
            get
            {
                return true;
            }
        }

        public override IList<IEnumerator<KeyValuePair<long, TSource>>>
            GetOrderablePartitions(int partitionCount)
        {
            var dynamicPartitions = GetOrderableDynamicPartitions();
            var partitions =
                new IEnumerator<KeyValuePair<long, TSource>>[partitionCount];

            for (int i = 0; i < partitionCount; i++)
            {
                partitions[i] = dynamicPartitions.GetEnumerator();
            }
            return partitions;
        }

        public override IEnumerable<KeyValuePair<long, TSource>>
            GetOrderableDynamicPartitions()
        {
            return new ListDynamicPartitions(m_input);
        }

        private class ListDynamicPartitions
            : IEnumerable<KeyValuePair<long, TSource>>
        {
            private IList<TSource> m_input;
            private int m_pos = 0;

            internal ListDynamicPartitions(IList<TSource> input)
            {
                m_input = input;
            }

            public IEnumerator<KeyValuePair<long, TSource>> GetEnumerator()
            {
                while (true)
                {
                    // Each task gets the next item in the list. The index is 
                    // incremented in a thread-safe manner to avoid races.
                    int elemIndex = Interlocked.Increment(ref m_pos) - 1;

                    if (elemIndex >= m_input.Count)
                    {
                        yield break;
                    }

                    yield return new KeyValuePair<long, TSource>(
                        elemIndex, m_input[elemIndex]);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return
                   ((IEnumerable<KeyValuePair<long, TSource>>)this)
                   .GetEnumerator();
            }
        }
    }

}
#endif