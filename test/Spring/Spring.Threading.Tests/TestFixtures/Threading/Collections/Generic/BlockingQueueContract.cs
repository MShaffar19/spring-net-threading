﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.CommonFixtures;
using NUnit.CommonFixtures.Collections;
using NUnit.CommonFixtures.Threading;
using NUnit.Framework;
using Spring.Collections.Generic;
using Spring.TestFixtures.Collections.Generic;
using Spring.Threading.AtomicTypes;
using Spring.Threading.Collections.Generic;

namespace Spring.TestFixtures.Threading.Collections.Generic
{
    /// <summary>
    /// Basic functionality test cases for implementation of <see cref="IBlockingQueue{T}"/>.
    /// </summary>
    /// <author>Kenneth Xu</author>
    public abstract class BlockingQueueContract<T> : QueueContract<T>
    {
        protected bool IsFair
        {
            get { return Options.Has(CollectionContractOptions.Fair); }
            set { Options = Options.Set(CollectionContractOptions.Fair, value); }
        }

        protected TestThreadManager ThreadManager {get; private set;}

        /// <summary>
        /// Only evaluates option <see cref="CollectionContractOptions.Unique"/>,
        /// <see cref="CollectionContractOptions.ReadOnly"/>,
        /// <see cref="CollectionContractOptions.Fifo"/> and
        /// <see cref="CollectionContractOptions.NoNull"/>.
        /// </summary>
        /// <param name="options"></param>
        protected BlockingQueueContract(CollectionContractOptions options) : base(options)
        {
        }

        protected sealed override IQueue<T> NewQueue()
        {
            return NewBlockingQueue();
        }
        protected sealed override IQueue<T> NewQueueFilledWithSample()
        {
            return NewBlockingQueueFilledWithSample();
        }

        protected virtual IBlockingQueue<T> NewBlockingQueueFilledWithSample()
        {
            return (IBlockingQueue<T>) base.NewQueueFilledWithSample();
        }

        protected abstract IBlockingQueue<T> NewBlockingQueue();

        [SetUp] public void SetUpThreadManager()
        {
            ThreadManager = new TestThreadManager();
        }

        [TearDown] public void TearDownThreadManager()
        {
            ThreadManager.TearDown(true);
        }

        public override void AddHandlesNullAsExpexcted()
        {
            var q = NewBlockingQueue();
            T value = default(T);
            if (!typeof(T).IsValueType && NoNull)
            {
                var e = Assert.Throws<ArgumentNullException>(
                    () => q.Add(value));
                Assert.That(e.ParamName, Is.EqualTo("element"));
            }
            else
            {
                // The thread is required to work with 0 capacity queue.
                ThreadManager.StartAndAssertRegistered("T1", () => PollOneFromQueue(q, value));
                q.Add(value);
                ThreadManager.JoinAndVerify();
            }
        }

        [Test] public virtual void PutHandlesNullAsExpected() {
            T value = default(T);
            var q = NewBlockingQueue();
            if (!typeof(T).IsValueType && NoNull)
            {
                var e = Assert.Throws<ArgumentNullException>(
                    () => q.Put(value));
                Assert.That(e.ParamName, Is.EqualTo("element"));
            }
            else
            {
                // The thread is required to work with 0 capacity queue.
                ThreadManager.StartAndAssertRegistered("T1", () => PollOneFromQueue(q, value));
                q.Put(value);
                ThreadManager.JoinAndVerify();
            }
        }

        private static void PollOneFromQueue(IBlockingQueue<T> q, T expectedValue)
        {
            T result;
            Assert.IsTrue(q.Poll(Delays.Small, out result));
            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test] public virtual void PutAddsElementsToQueue() {
            var q = NewBlockingQueue();
            for (int i = 0; i < SampleSize; ++i) {
                q.Put(Samples[i]);
                Assert.IsTrue(q.Contains(Samples[i]));
            }
            AssertRemainingCapacity(q, 0);
        }

        [Test] public virtual void PutBlocksInterruptiblyWhenFull()
        {
            Options.SkipWhenNot(CollectionContractOptions.Bounded);
            var q = NewBlockingQueueFilledWithSample();
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1", () => Assert.Throws<ThreadInterruptedException>(
                                () => q.Put(TestData<T>.MakeData(SampleSize))));
            Thread.Sleep(Delays.Short);
            t.Interrupt();
            ThreadManager.JoinAndVerify();
        }

        [Test] public virtual void PutBlocksWaitingForTakeWhenFull()
        {
            Options.SkipWhenNot(CollectionContractOptions.Bounded);
            var q = NewBlockingQueueFilledWithSample();
            AtomicBoolean added = new AtomicBoolean(false);
            ThreadManager.StartAndAssertRegistered(
                "T1", () => { q.Put(TestData<T>.One); added.Value = true; });
            Thread.Sleep(Delays.Short);
            Assert.IsFalse(added);
            q.Take();
            ThreadManager.JoinAndVerify();
            Assert.IsTrue(added);
        }

        [Test] public virtual void FairQueueUnblocksOfferingThreadsInFifoOrder()
        {
            Options.SkipWhenNot(CollectionContractOptions.Bounded);
            Options.SkipWhenNot(CollectionContractOptions.Fair);
            const int size = 3;
            var q = NewBlockingQueueFilledWithSample();
            var exitValue = new AtomicInteger();
            var order = new AtomicInteger(1);
            for (int i = 1; i <= size; i++)
            {
                var index = i;
                ThreadManager.StartAndAssertRegistered(
                    "T" + index,
                    () =>
                        {
                            while (order.Value < index) Thread.Sleep(1);
                            Thread.Sleep(Delays.Short);
                            order.IncrementValueAndReturn();
                            Assert.IsTrue(q.Offer(TestData<T>.MakeData(index), Delays.Long));
                            exitValue.Value = index;
                        });
            }
            while (order.Value <= size) Thread.Sleep(1);
            Thread.Sleep(Delays.Short);
            for (int i = 1; i <= size; i++)
            {
                T result;
                Assert.IsTrue(q.Poll(Delays.Short, out result));
                for (int j = 0; j < 100 && exitValue.Value != i; j++) Thread.Sleep(1);
                Assert.That(exitValue.Value, Is.EqualTo(i));
            }
            ThreadManager.JoinAndVerify();
        }

        [Test] public virtual void FairQueueUnblocksTakingThreadsInFifoOrder()
        {
            Options.SkipWhenNot(CollectionContractOptions.Fair);
            const int size = 3;
            var q = NewBlockingQueue();
            var exitValue = new AtomicInteger();
            var order = new AtomicInteger(1);
            for (int i = 1; i <= size; i++)
            {
                var index = i;
                ThreadManager.StartAndAssertRegistered(
                    "T" + index,
                    () =>
                        {
                            while (order.Value < index) Thread.Sleep(1);
                            Thread.Sleep(Delays.Short);
                            order.IncrementValueAndReturn();
                            T result;
                            Assert.IsTrue(q.Poll(Delays.Long, out result));
                            exitValue.Value = index;
                        });
            }
            while (order.Value <= size) Thread.Sleep(1);
            Thread.Sleep(Delays.Short);
            for (int i = 1; i <= size; i++)
            {
                Assert.IsTrue(q.Offer(TestData<T>.MakeData(i), Delays.Short));
                for (int j = 0; j < 100 && exitValue.Value != i; j++) Thread.Sleep(1);
                Assert.That(exitValue.Value, Is.EqualTo(i));
            }
            ThreadManager.JoinAndVerify();
        }

        [Test] public virtual void TimedOfferWaitsInterruptablyAndTimesOutIfFullAndSucceedAfterTaken()
        {
            Options.SkipWhenNot(CollectionContractOptions.Bounded);
            var values = TestData<T>.MakeTestArray(SampleSize + 3);
            var q = NewBlockingQueueFilledWithSample();
            var timedout = new AtomicBoolean(false);
            ThreadManager.StartAndAssertRegistered(
                "T2", () => Assert.IsTrue(q.Offer(values[SampleSize], Delays.Long)));
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1",
                delegate
                    {
                        Assert.IsFalse(q.Offer(TestData<T>.M1, Delays.Short));
                        timedout.Value = true;
                        Assert.Throws<ThreadInterruptedException>(
                            () => q.Offer(TestData<T>.M2, Delays.Long));
                    });

            for (int i = 5; i > 0 && !timedout; i--) Thread.Sleep(Delays.Short);
            Assert.That(timedout.Value, Is.True, "Offer should timeout by now.");
            ThreadManager.StartAndAssertRegistered(
                "T3", () => Assert.IsTrue(q.Offer(values[SampleSize + 1], Delays.Long)));
            Thread.Sleep(Delays.Short);
            ThreadManager.StartAndAssertRegistered(
                "T4", () => Assert.IsTrue(q.Offer(values[SampleSize + 2], Delays.Long)));
            t.Interrupt();
            Thread.Sleep(Delays.Short);
            for (int i = 0; i < SampleSize + 3; i++)
            {
                T result;
                Assert.IsTrue(q.Poll(Delays.Short, out result));
                if (IsFifo && i<SampleSize) Assert.That(result, Is.EqualTo(values[i]));
                else CollectionAssert.Contains(values, result);
            }
            ThreadManager.JoinAndVerify();
        }

        [Test] public void TimedOfferAcceptsLongWait()
        {
            var q = NewBlockingQueueFilledWithSample();
            ThreadManager.StartAndAssertRegistered(
                "T1", () => Assert.IsTrue(q.Offer(TestData<T>.One, TimeSpan.MaxValue)));
            Thread.Sleep(Delays.Short);
            q.Take();
            ThreadManager.JoinAndVerify();
        }

        [Test] public virtual void TakeRetrievesElementsInExpectedOrder()
        {
            var q = NewBlockingQueueFilledWithSample();
            var itemsTook = new List<T>();
            for (int i = 0; i < SampleSize; ++i)
            {
                if(IsFifo)
                    Assert.AreEqual(Samples[i], q.Take());
                else
                    itemsTook.Add(q.Take());
            }
            if(!IsFifo) CollectionAssert.AreEquivalent(Samples, itemsTook);
        }

        [Test] public virtual void TakeBlocksInterruptiblyWhenEmpty()
        {
            var q = NewBlockingQueue();
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1", () => Assert.Throws<ThreadInterruptedException>(() => q.Take()));
            Thread.Sleep(Delays.Short);
            t.Interrupt();
            ThreadManager.JoinAndVerify(t);
        }

        [Test] public virtual void TakeRemovesExistingElementsUntilEmptyThenBlocksInterruptibly()
        {
            var isEmpty = new AtomicBoolean(false);
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1",
                delegate
                    {
                        var q = NewBlockingQueueFilledWithSample();
                        for (int i = q.Count - 1; i >= 0; i--) q.Take();
                        isEmpty.Value = true;
                        Assert.Throws<ThreadInterruptedException>(() => q.Take());
                    });
            for (int i = 5 - 1; i >= 0 && !isEmpty; i--) Thread.Sleep(Delays.Short);
            t.Interrupt();
            ThreadManager.JoinAndVerify(t);
        }

        [Test] public virtual void TimedPollWithZeroTimeoutSucceedsWhenNonEmptyElseTimesOut()
        {
            var q = NewBlockingQueueFilledWithSample();
            // run it in a separate thread so test won't hang due to bad queue implementation.
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1",
                delegate
                    {
                        T value;
                        for (int i = 0; i < SampleSize; ++i)
                        {
                            Assert.IsTrue(q.Poll(TimeSpan.Zero, out value));
                            AssertRetrievedResult(value, i);
                        }
                        Assert.IsFalse(q.Poll(TimeSpan.Zero, out value));
                    });
            ThreadManager.JoinAndVerify(t);
        }

        [Test] public virtual void TimedPollWithNonZeroTimeoutSucceedsWhenNonEmptyElseTimesOut() {
            var q = NewBlockingQueueFilledWithSample();
            // run it in a separate thread so test won't hang due to bad queue implementation.
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1",
                delegate
                    {
                        T value;
                        for (int i = 0; i < SampleSize; ++i)
                        {
                            Assert.IsTrue(q.Poll(Delays.Short, out value));
                            AssertRetrievedResult(value, i);
                        }
                        Assert.IsFalse(q.Poll(Delays.Short, out value));
                    });
            ThreadManager.JoinAndVerify(t);
        }

        [Test] public virtual void TimedPollIsInterruptable() {
            var q = NewBlockingQueue();
            var isEmpty = new AtomicBoolean(false);
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1",
                delegate
                    {
                        T value;
                        while (q.Count > 0) q.Take();
                        isEmpty.Value = true;
                        Assert.Throws<ThreadInterruptedException>(
                            () => q.Poll(Delays.Medium, out value));
                    });
            for (int i = 5 - 1; i >= 0 && !isEmpty; i--) Thread.Sleep(Delays.Short);
            t.Interrupt();
            ThreadManager.JoinAndVerify(t);
        }

        [Test] public virtual void TimedPollFailsBeforeDelayedOfferSucceedsAfterOfferChokesOnInterruption() {
            var q = NewBlockingQueue();
            T value;
            var timedout = new AtomicBoolean(false);
            Thread t = ThreadManager.StartAndAssertRegistered(
                "T1",
                delegate {
                             Assert.IsFalse(q.Poll(Delays.Short, out value));
                             timedout.Value = true;
                             Assert.IsTrue(q.Poll(Delays.Long, out value));
                             Assert.Throws<ThreadInterruptedException>(()=>q.Poll(Delays.Long, out value));
                });
            for (int i = 5 - 1; i >= 0 && !timedout; i--) Thread.Sleep(Delays.Short);
            Assert.IsTrue(q.Offer(TestData<T>.One, Delays.Short));
            Thread.Sleep(Delays.Short);
            t.Interrupt();
            ThreadManager.JoinAndVerify(t);
        }

        [Test] public void TimedPollAllowsLongWait()
        {
            var q = NewBlockingQueue();
            T result; while (q.Poll(out result)){}

            ThreadManager.StartAndAssertRegistered(
                "T1", () => Assert.IsTrue(q.Poll(TimeSpan.MaxValue, out result)));
            Thread.Sleep(Delays.Short);
            q.Put(TestData<T>.One);
            ThreadManager.JoinAndVerify();
        }

        [Test] public virtual void DrainToChokesOnNullArgument() {
            var q = NewBlockingQueueFilledWithSample();
            var e = Assert.Throws<ArgumentNullException>(()=>q.DrainTo(null));
            Assert.That(e.ParamName, Is.EqualTo("collection"));
            var e2 = Assert.Throws<ArgumentNullException>(()=>q.DrainTo(null, 0));
            Assert.That(e2.ParamName, Is.EqualTo("collection"));
        }

        [Test] public virtual void DrainToChokesWhenDrainToSelf() {
            var q = NewBlockingQueueFilledWithSample();
            var e = Assert.Throws<ArgumentException>(() => q.DrainTo(q));
            Assert.That(e.ParamName, Is.EqualTo("collection"));
            var e2 = Assert.Throws<ArgumentException>(()=>q.DrainTo(q, 0));
            Assert.That(e2.ParamName, Is.EqualTo("collection"));
        }

        [Test] public virtual void DrainToEmptiesQueueIntoAnotherCollection() {
            var expected = PollAll(NewBlockingQueueFilledWithSample()).ToArray();
            var q = NewBlockingQueueFilledWithSample();
            List<T> l = new List<T>();
            q.DrainTo(l);
            Assert.AreEqual(q.Count, 0);
            Assert.AreEqual(l.Count, SampleSize);
            for (int i = 0; i < SampleSize; ++i)
                Assert.AreEqual(l[i], expected[i]);
            int count = Math.Min(2, SampleSize);
            expected = new T[count];
            for (int i = 0; i < count; i++)
            {
                expected[i] = Samples[i];
                q.Add(expected[i]);
            }
            Assert.AreEqual(count, q.Count);
            for (int i = 0; i < count; i++) Assert.IsTrue(q.Contains(Samples[i]));
            l.Clear();
            q.DrainTo(l);
            Assert.AreEqual(0, q.Count);
            Assert.AreEqual(count, l.Count);
            for (int i = 0; i < count; ++i)
                Assert.IsTrue(expected.Contains(l[i]));
        }

        [Test] public virtual void DrainToEmptiesFullQueueAndUnblocksWaitingPut() {
            var expected = PollAll(NewBlockingQueueFilledWithSample()).ToArray();
            var q = NewBlockingQueueFilledWithSample();
            T toPut = TestData<T>.MakeData(SampleSize + 1);
            Thread t = ThreadManager.StartAndAssertRegistered("T1", () => q.Put(toPut));
            List<T> l = new List<T>();
            q.DrainTo(l);
            Assert.IsTrue(l.Count >= SampleSize);
            l.Remove(toPut);
            for (int i = 0; i < SampleSize; ++i)
                Assert.AreEqual(l[i], expected[i]);
            ThreadManager.JoinAndVerify(t);
            Assert.IsTrue(q.Count + l.Count >= SampleSize);
        }

        [Test] public virtual void LimitedDrainToEmptiesFirstNElementsIntoCollection() {
            var expected = PollAll(NewBlockingQueueFilledWithSample()).ToArray();
            var q = NewBlockingQueue();
            for (int i = 0; i < SampleSize + 2; ++i)
            {
                for(int j = 0; j < SampleSize; j++)
                    Assert.IsTrue(q.Offer(Samples[j]));
                List<T> l = new List<T>();
                q.DrainTo(l, i);
                int k = (i < SampleSize)? i : SampleSize;
                Assert.AreEqual(l.Count, k);
                Assert.AreEqual(q.Count, SampleSize-k);
                for (int j = 0; j < k; ++j)
                    Assert.AreEqual(l[j], expected[j]);
                T v;
                while (q.Poll(out v)) {}
            }
        }

        [Test] public virtual void SelectiveDrainToMovesSelectedElementsIntoCollection()
        {
            var expected = PollAll(NewBlockingQueueFilledWithSample()).Where(e=>e.GetHashCode()%2==0).ToArray();
            var q = NewBlockingQueueFilledWithSample();
            List<T> l = new List<T>();
            q.DrainTo(l, e=>e.GetHashCode()%2==0);
            Assert.That(l.Count, Is.EqualTo(expected.Length));
            Assert.That(q.Count, Is.LessThanOrEqualTo(SampleSize - expected.Length));
            Assert.AreEqual(SampleSize, q.Count + l.Count);
            for (int i = 0; i < l.Count; i++)
                Assert.AreEqual(l[i], expected[i]);
        }

        [Test] public virtual void OfferTransfersElementsAcrossThreads()
        {
            Options.SkipWhenNot(CollectionContractOptions.Bounded);
            var q = NewBlockingQueueFilledWithSample();
            ThreadManager.StartAndAssertRegistered(
                "T",
                delegate
                    {
                        Assert.IsFalse(q.Offer(TestData<T>.Three));
                        Assert.IsTrue(q.Offer(TestData<T>.Three, Delays.Long));
                        Assert.AreEqual(0, q.RemainingCapacity);
                    },
                delegate
                    {
                        Thread.Sleep(Delays.Short);
                        q.Take();
                    });

            ThreadManager.JoinAndVerify(Delays.Medium);
        }

        [Test] public virtual void PollRetrievesElementsAcrossThreads()
        {
            var q = NewBlockingQueue();
            ThreadManager.StartAndAssertRegistered(
                "T",
                delegate
                    {
                        T value;
                        Assert.IsFalse(q.Poll(out value));
                        Assert.IsTrue(q.Poll(Delays.Long, out value));
                        Assert.IsTrue(q.Count == 0); //empty
                    },
                delegate
                    {
                        Thread.Sleep(Delays.Short);
                        q.Put(TestData<T>.One);
                    });

            ThreadManager.JoinAndVerify(Delays.Medium);
        }

    }
}