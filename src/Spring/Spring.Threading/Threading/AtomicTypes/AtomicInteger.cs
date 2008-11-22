#region License

/*
 * Copyright 2002-2008 the original author or authors.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#endregion

using System;

namespace Spring.Threading.AtomicTypes {
    /// <summary> 
    /// An <see lang="int"/> value that may be updated atomically.
    /// An <see cref="Spring.Threading.AtomicTypes.AtomicInteger"/> is used in applications such as atomically
    /// incremented counters, and cannot be used as a replacement for an
    /// <see cref="int"/>. 
    /// <p/>
    /// Based on the on the back port of JCP JSR-166.
    /// </summary>
    /// <author>Doug Lea</author>
    /// <author>Griffin Caprio (.NET)</author>
    /// <author>Andreas Doehring (.NET)</author>
    [Serializable]
    public class AtomicInteger {
        private volatile int _value;

        /// <summary> 
        /// Creates a new <see cref="Spring.Threading.AtomicTypes.AtomicInteger"/> with a value of <paramref name="initialValue"/>.
        /// </summary>
        /// <param name="initialValue">
        /// The initial value
        /// </param>
        public AtomicInteger(int initialValue) {
            _value = initialValue;
        }

        /// <summary> 
        /// Creates a new <see cref="Spring.Threading.AtomicTypes.AtomicInteger"/> with initial value 0.
        /// </summary>
        public AtomicInteger()
            : this(0) {
        }

        /// <summary> 
        /// Gets / Sets the current value.
        /// <p/>
        /// <b>Note:</b> The setting of this value occurs within a <see lang="lock"/>.
        /// </summary>
        public int Value {
            get { return _value; }
            set {
                lock(this) {
                    _value = value;
                }
            }
        }

        /// <summary>
        /// Gets the current value as int
        /// </summary>
        public int IntValue {
            get { return Value; }
        }

        /// <summary>
        /// Gets the current value as long
        /// </summary>
        public long LongValue {
            get { return Value; }
        }

        /// <summary>
        /// Gets the current value as float
        /// </summary>
        public float FloatValue {
            get { return Value; }
        }

        /// <summary>
        /// Gets the current value as double
        /// </summary>
        public double DoubleValue {
            get { return Value; }
        }

        /// <summary>
        /// Gets the current value as short
        /// <b>Note:</b> this may round the value
        /// </summary>
        public short ShortValue {
            get { return (short)Value; }
        }

        /// <summary>
        /// Gets the current value as byte
        /// <b>Note:</b> this may round the value
        /// </summary>
        public byte ByteValue {
            get { return (byte)Value; }
        }

        /// <summary> 
        /// Atomically increments by one the current value.
        /// </summary>
        /// <returns> 
        /// The previous value
        /// </returns>
        public int GetAndIncrement() {
            lock(this) {
                return _value++;
            }

        }

        /// <summary> 
        /// Atomically decrements by one the current value.
        /// </summary>
        /// <returns> 
        /// The previous value
        /// </returns>
        public int GetAndDecrement() {
            lock(this) {
                return _value--;
            }
        }

        /// <summary> 
        /// Eventually sets to the given value.
        /// </summary>
        /// <param name="newValue">
        /// The new value
        /// </param>
        /// TODO: This method doesn't differ from the set() method, which was converted to a property.  For now
        /// the property will be called for this method.
        [Obsolete("This method will be removed.  Please use AtomicInteger.Value property instead.")]
        public void LazySet(int newValue) {
            Value = newValue;
        }

        /// <summary> 
        /// Atomically sets value to <paramref name="newValue"/> and returns the old value.
        /// </summary>
        /// <param name="newValue">
        /// The new value
        /// </param>
        /// <returns> 
        /// The previous value
        /// </returns>
        public int GetAndSet(int newValue) {
            lock(this) {
                int oldValue = _value;
                _value = newValue;
                return oldValue;
            }
        }

        /// <summary> 
        /// Atomically sets the value to <paramref name="newValue"/>
        /// if the current value == <paramref name="expectedValue"/>
        /// </summary>
        /// <param name="expectedValue">
        /// The expected value
        /// </param>
        /// <param name="newValue">
        /// The new value
        /// </param>
        /// <returns> <see lang="true"/> if successful. <see lang="false"/> return indicates that
        /// the actual value was not equal to the expected value.
        /// </returns>
        public bool CompareAndSet(int expectedValue, int newValue) {
            lock(this) {
                if(_value == expectedValue) {
                    _value = newValue;
                    return true;
                }
                return false;
            }
        }

        /// <summary> 
        /// Atomically sets the value to <paramref name="newValue"/>
        /// if the current value == <paramref name="expectedValue"/>
        /// </summary>
        /// <param name="expectedValue">
        /// The expected value
        /// </param>
        /// <param name="newValue">
        /// The new value
        /// </param>
        /// <returns> <see lang="true"/> if successful. <see lang="false"/> return indicates that
        /// the actual value was not equal to the expected value.
        /// </returns>
        public virtual bool WeakCompareAndSet(int expectedValue, int newValue) {
            lock(this) {
                if(_value == expectedValue) {
                    _value = newValue;
                    return true;
                }
                return false;
            }
        }

        /// <summary> 
        /// Atomically adds <paramref name="deltaValue"/> to the current value.
        /// </summary>
        /// <param name="deltaValue">
        /// The value to add
        /// </param>
        /// <returns> 
        /// The previous value
        /// </returns>
        public int GetAndAdd(int deltaValue) {
            lock(this) {
                int oldValue = _value;
                _value += deltaValue;
                return oldValue;
            }
        }

        /// <summary> 
        /// Atomically adds <paramref name="deltaValue"/> to the current value.
        /// </summary>
        /// <param name="deltaValue">
        /// The value to add
        /// </param>
        /// <returns> 
        /// The updated value
        /// </returns>
        public int AddAndGet(int deltaValue) {
            lock(this) {
                return _value += deltaValue;
            }
        }

        /// <summary> 
        /// Atomically increments the current value by one.
        /// </summary>
        /// <returns> 
        /// The updated value
        /// </returns>
        public int IncrementAndGet() {
            lock(this) {
                return ++_value;
            }
        }

        /// <summary> 
        /// Atomically decrements by one the current value.
        /// </summary>
        /// <returns> 
        /// The updated value
        /// </returns>
        public int DecrementAndGet() {
            lock(this) {
                return --_value;
            }
        }

        /// <summary> 
        /// Returns the String representation of the current value.
        /// </summary>
        /// <returns> 
        /// The String representation of the current value.
        /// </returns>
        public override string ToString() {
            return Value.ToString();
        }
    }
}