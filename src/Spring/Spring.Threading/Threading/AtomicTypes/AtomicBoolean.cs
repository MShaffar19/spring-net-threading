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
    /// A <see lang="bool"/> value that may be updated atomically. An <see cref="Spring.Threading.AtomicTypes.AtomicBoolean"/> 
    /// is used for instances of atomically updated flags, and cannot be used as a replacement for a <see lang="bool"/> value.
    /// <p/>
    /// Based on the on the back port of JCP JSR-166.
    /// </summary>
    /// <author>Doug Lea</author>
    /// <author>Griffin Caprio (.NET)</author>
    /// <author>Andreas Doehring (.NET)</author>
    [Serializable]
    public class AtomicBoolean {
        /// <summary>
        /// Holds a <see lang="int"/> representation of the flag value.
        /// </summary>
        private volatile int _value;

        /// <summary> 
        /// Creates a new <see cref="Spring.Threading.AtomicTypes.AtomicBoolean"/> with the given initial value.
        /// </summary>
        /// <param name="initialValue">
        /// The initial value
        /// </param>
        public AtomicBoolean(bool initialValue) {
            _value = initialValue ? 1 : 0;
        }

        /// <summary> 
        /// Creates a new <see cref="Spring.Threading.AtomicTypes.AtomicBoolean"/> with initial value of <see lang="false"/>.
        /// </summary>
        public AtomicBoolean()
            : this(false) {
        }

        /// <summary> 
        /// Gets / Sets the current value.
        /// <p/>
        /// <b>Note:</b> The setting of this value occurs within a <see lang="lock"/>.
        /// </summary>
        public bool Value {
            get { return _value != 0; }
            set {
                lock(this) {
                    _value = value ? 1 : 0;
                }
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
        /// The new value to use of the current value equals the expected value.
        /// </param>
        /// <returns> 
        /// <see lang="true"/> if the current value equaled the expected value, <see lang="false"/> otherwise.
        /// </returns>
        public bool CompareAndSet(bool expectedValue, bool newValue) {
            lock(this) {
                if(expectedValue == (_value != 0)) {
                    _value = newValue ? 1 : 0;
                    return true;
                }
                return false;
            }
        }

        /// <summary> 
        /// Atomically sets the value to <paramref name="newValue"/>
        /// if the current value == <paramref name="expectedValue"/>
        /// May fail spuriously.
        /// </summary>
        /// <param name="expectedValue">
        /// The expected value
        /// </param>
        /// <param name="newValue">
        /// The new value to use of the current value equals the expected value.
        /// </param>
        /// <returns>
        /// <see lang="true"/> if the current value equaled the expected value, <see lang="false"/> otherwise.
        /// </returns>
        public virtual bool WeakCompareAndSet(bool expectedValue, bool newValue) {
            lock(this) {
                if(expectedValue == (_value != 0)) {
                    _value = newValue ? 1 : 0;
                    return true;
                }
                return false;
            }
        }
        
        /// <summary> 
		/// Eventually sets to the given value.
		/// </summary>
		/// <param name="newValue">
		/// the new value
		/// </param>
		/// TODO: This method doesn't differ from the set() method, which was converted to a property.  For now
		/// the property will be called for this method.
        [Obsolete("This method will be removed.  Please use AtomicBoolean.BooleanValue property instead.")]
        public void LazySet(bool newValue) {
            Value = newValue;
        }

        /// <summary> 
        /// Atomically sets the current value to <parmref name="newValue"/> and returns the previous value.
        /// </summary>
        /// <param name="newValue">
        /// The new value for the instance.
        /// </param>
        /// <returns> 
        /// the previous value of the instance.
        /// </returns>
        public bool GetAndSet(bool newValue) {
            lock(this) {
                int oldValue = _value;
                _value = newValue ? 1 : 0;
                return oldValue != 0;
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