using System;
using System.Diagnostics;

namespace Amib.Threading.Internal
{
    internal enum STPPerformanceCounterType
    {
        // Fields
        ActiveThreads                = 0,
        InUseThreads                = 1,
        OverheadThreads                = 2,
        OverheadThreadsPercent        = 3,
        OverheadThreadsPercentBase    = 4,

        WorkItems                    = 5,
        WorkItemsInQueue            = 6,
        WorkItemsProcessed            = 7,

        WorkItemsQueuedPerSecond    = 8,
        WorkItemsProcessedPerSecond    = 9,

        AvgWorkItemWaitTime            = 10,
        AvgWorkItemWaitTimeBase        = 11,

        AvgWorkItemProcessTime        = 12,
        AvgWorkItemProcessTimeBase    = 13,

        WorkItemsGroups                = 14,

        LastCounter                    = 14,
    }
 

    /// <summary>
    /// Summary description for STPPerformanceCounter.
    /// </summary>
    internal class STPPerformanceCounter
    {
        // Fields
        private PerformanceCounterType _pcType;
        protected string _counterHelp;
        protected string _counterName;

        // Methods
        public STPPerformanceCounter(
            string counterName, 
            string counterHelp, 
            PerformanceCounterType pcType)
        {
            this._counterName = counterName;
            this._counterHelp = counterHelp;
            this._pcType = pcType;
        }

        public void AddCounterToCollection(CounterCreationDataCollection counterData)
        {
            CounterCreationData counterCreationData = new CounterCreationData(
                _counterName, 
                _counterHelp, 
                _pcType);

            counterData.Add(counterCreationData);
        }
 
        // Properties
        public string Name
        {
            get
            {
                return _counterName;
            }
        }
    }

    internal class STPPerformanceCounters
    {
        // Fields
        internal STPPerformanceCounter[] _stpPerformanceCounters;
        private static STPPerformanceCounters _instance;
        internal const string _stpCategoryHelp = "SmartThreadPool performance counters";
        internal const string _stpCategoryName = "SmartThreadPool";

        // Methods
        static STPPerformanceCounters()
        {
            _instance = new STPPerformanceCounters();
        }
 
        private STPPerformanceCounters()
        {
            STPPerformanceCounter[] stpPerformanceCounters = new STPPerformanceCounter[] 
                { 
                    new STPPerformanceCounter("Active threads", "The current number of available in the thread pool.", PerformanceCounterType.NumberOfItems32), 
                    new STPPerformanceCounter("In use threads", "The current number of threads that execute a work item.", PerformanceCounterType.NumberOfItems32), 
                    new STPPerformanceCounter("Overhead threads", "The current number of threads that are active, but are not in use.", PerformanceCounterType.NumberOfItems32), 
                    new STPPerformanceCounter("% overhead threads", "The current number of threads that are active, but are not in use in percents.", PerformanceCounterType.RawFraction), 
                    new STPPerformanceCounter("% overhead threads base", "The current number of threads that are active, but are not in use in percents.", PerformanceCounterType.RawBase), 

                    new STPPerformanceCounter("Work Items", "The number of work items in the Smart Thread Pool. Both queued and processed.", PerformanceCounterType.NumberOfItems32), 
                    new STPPerformanceCounter("Work Items in queue", "The current number of work items in the queue", PerformanceCounterType.NumberOfItems32), 
                    new STPPerformanceCounter("Work Items processed", "The number of work items already processed", PerformanceCounterType.NumberOfItems32), 

                    new STPPerformanceCounter("Work Items queued/sec", "The number of work items queued per second", PerformanceCounterType.RateOfCountsPerSecond32), 
                    new STPPerformanceCounter("Work Items processed/sec", "The number of work items processed per second", PerformanceCounterType.RateOfCountsPerSecond32), 

                    new STPPerformanceCounter("Avg. Work Item wait time/sec", "The average time a work item supends in the queue waiting for its turn to execute.", PerformanceCounterType.AverageCount64), 
                    new STPPerformanceCounter("Avg. Work Item wait time base", "The average time a work item supends in the queue waiting for its turn to execute.", PerformanceCounterType.AverageBase), 

                    new STPPerformanceCounter("Avg. Work Item process time/sec", "The average time it takes to process a work item.", PerformanceCounterType.AverageCount64), 
                    new STPPerformanceCounter("Avg. Work Item process time base", "The average time it takes to process a work item.", PerformanceCounterType.AverageBase), 

                    new STPPerformanceCounter("Work Items Groups", "The current number of work item groups associated with the Smart Thread Pool.", PerformanceCounterType.NumberOfItems32), 
                };

            _stpPerformanceCounters = stpPerformanceCounters;
            SetupCategory();
        }
 
        private void SetupCategory()
        {
            if (!PerformanceCounterCategory.Exists(_stpCategoryName))
            {
                CounterCreationDataCollection counters = new CounterCreationDataCollection();

                for (int i = 0; i < _stpPerformanceCounters.Length; i++)
                {
                    _stpPerformanceCounters[i].AddCounterToCollection(counters);
                }


                // *********** Remark for .NET 2.0 ***********
                // If you are here, it means you got the warning that this overload
                // of the method is deprecated in .NET 2.0. To use the correct 
                // method overload, uncomment the third argument of
                // the method.
                #pragma warning disable 0618
                PerformanceCounterCategory.Create(
                    _stpCategoryName, 
                    _stpCategoryHelp, 
                    //PerformanceCounterCategoryType.MultiInstance,
                    counters);
                #pragma warning restore 0618
            }
        }
 
        // Properties
        public static STPPerformanceCounters Instance
        {
            get
            {
                return _instance;
            }
        }
     }

    internal class STPInstancePerformanceCounter : IDisposable
    {
        // Fields
        private PerformanceCounter _pcs;

        // Methods
        protected STPInstancePerformanceCounter()
        {
        }

        public STPInstancePerformanceCounter(
            string instance, 
            STPPerformanceCounterType spcType)
        {
            STPPerformanceCounters counters = STPPerformanceCounters.Instance;
            _pcs = new PerformanceCounter(
                STPPerformanceCounters._stpCategoryName, 
                counters._stpPerformanceCounters[(int) spcType].Name, 
                instance, 
                false);
            _pcs.RawValue = _pcs.RawValue;
        }

        ~STPInstancePerformanceCounter()
        {
            Close();
        }

        public void Close()
        {
            if (_pcs != null)
            {
                _pcs.RemoveInstance();
                _pcs.Close();
                _pcs = null;
            }
        }
 
        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
 
        public virtual void Increment()
        {
            _pcs.Increment();
        }
 
        public virtual void IncrementBy(long val)
        {
            _pcs.IncrementBy(val);
        }

        public virtual void Set(long val)
        {
            _pcs.RawValue = val;
        }
    }

    internal class STPInstanceNullPerformanceCounter : STPInstancePerformanceCounter
    {
        // Methods
        public STPInstanceNullPerformanceCounter() {}
        public override void Increment() {}
        public override void IncrementBy(long value) {}
        public override void Set(long val) {}
    }
        
    internal interface ISTPInstancePerformanceCounters : IDisposable
    {
        void Close();
        void SampleThreads(long activeThreads, long inUseThreads);
        void SampleWorkItems(long workItemsQueued, long workItemsProcessed);
        void SampleWorkItemsWaitTime(TimeSpan workItemWaitTime);
        void SampleWorkItemsProcessTime(TimeSpan workItemProcessTime);
    }


    internal class STPInstancePerformanceCounters : ISTPInstancePerformanceCounters, IDisposable
    {
        // Fields
        private STPInstancePerformanceCounter[] _pcs;
        private static STPInstancePerformanceCounter _stpInstanceNullPerformanceCounter;

        // Methods
        static STPInstancePerformanceCounters()
        {
            _stpInstanceNullPerformanceCounter = new STPInstanceNullPerformanceCounter();
        }
 
        public STPInstancePerformanceCounters(string instance)
        {
            _pcs = new STPInstancePerformanceCounter[(int)STPPerformanceCounterType.LastCounter];
            // STPPerformanceCounters counters = STPPerformanceCounters.Instance;
            for (int i = 0; i < _pcs.Length; i++)
            {
                if (instance != null)
                {
                    _pcs[i] = new STPInstancePerformanceCounter(
                        instance, 
                        (STPPerformanceCounterType) i);
                }
                else
                {
                    _pcs[i] = _stpInstanceNullPerformanceCounter;
                }
            }
        }
 

        public void Close()
        {
            if (null != _pcs)
            {
                for (int i = 0; i < _pcs.Length; i++)
                {
                    if (null != _pcs[i])
                    {
                        _pcs[i].Close();
                    }
                }
                _pcs = null;
            }
        }

        ~STPInstancePerformanceCounters()
        {
            Close();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }
 
        private STPInstancePerformanceCounter GetCounter(STPPerformanceCounterType spcType)
        {
            return _pcs[(int) spcType];
        }

        public void SampleThreads(long activeThreads, long inUseThreads)
        {
            GetCounter(STPPerformanceCounterType.ActiveThreads).Set(activeThreads);
            GetCounter(STPPerformanceCounterType.InUseThreads).Set(inUseThreads);
            GetCounter(STPPerformanceCounterType.OverheadThreads).Set(activeThreads-inUseThreads);

            GetCounter(STPPerformanceCounterType.OverheadThreadsPercentBase).Set(activeThreads-inUseThreads);
            GetCounter(STPPerformanceCounterType.OverheadThreadsPercent).Set(inUseThreads);
        }

        public void SampleWorkItems(long workItemsQueued, long workItemsProcessed)
        {
            GetCounter(STPPerformanceCounterType.WorkItems).Set(workItemsQueued+workItemsProcessed);
            GetCounter(STPPerformanceCounterType.WorkItemsInQueue).Set(workItemsQueued);
            GetCounter(STPPerformanceCounterType.WorkItemsProcessed).Set(workItemsProcessed);

            GetCounter(STPPerformanceCounterType.WorkItemsQueuedPerSecond).Set(workItemsQueued);
            GetCounter(STPPerformanceCounterType.WorkItemsProcessedPerSecond).Set(workItemsProcessed);
        }

        public void SampleWorkItemsWaitTime(TimeSpan workItemWaitTime)
        {
            GetCounter(STPPerformanceCounterType.AvgWorkItemWaitTime).IncrementBy((long)workItemWaitTime.TotalMilliseconds);
            GetCounter(STPPerformanceCounterType.AvgWorkItemWaitTimeBase).Increment();
        }

        public void SampleWorkItemsProcessTime(TimeSpan workItemProcessTime)
        {
            GetCounter(STPPerformanceCounterType.AvgWorkItemProcessTime).IncrementBy((long)workItemProcessTime.TotalMilliseconds);
            GetCounter(STPPerformanceCounterType.AvgWorkItemProcessTimeBase).Increment();
        }
    }

    internal class NullSTPInstancePerformanceCounters : ISTPInstancePerformanceCounters, IDisposable
    {
        static NullSTPInstancePerformanceCounters()
        {
        }

        private static NullSTPInstancePerformanceCounters _instance = new NullSTPInstancePerformanceCounters(null);

        public static NullSTPInstancePerformanceCounters Instance
        {
            get { return _instance; }
        }

        public NullSTPInstancePerformanceCounters(string instance) {}
         public void Close() {}
        public void Dispose() {}
 
        public void SampleThreads(long activeThreads, long inUseThreads) {}
        public void SampleWorkItems(long workItemsQueued, long workItemsProcessed) {}
        public void SampleWorkItemsWaitTime(TimeSpan workItemWaitTime) {}
        public void SampleWorkItemsProcessTime(TimeSpan workItemProcessTime) {}
    }

}
