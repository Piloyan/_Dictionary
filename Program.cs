using System;
using System.Collections.Generic;
using System.Text;

namespace _Dictionary
{
    public static class _HashHelpers
    {
        public const int MaxPrimeArrayLength = 0x7FEFFFFD;
        public static readonly int[] primes = {
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369};
        public static bool IsPrime(int candidate)
        {
            if ((candidate & 1) != 0)
            {
                int limit = (int)Math.Sqrt(candidate);
                for (int divisor = 3; divisor <= limit; divisor += 2)
                {
                    if ((candidate % divisor) == 0)
                        return false;
                }
                return true;
            }
            return (candidate == 2);
        }
        public static int GetPrime(int min)
        {
            if (min < 0)
                throw new ArgumentException();
            for (int i = 0; i < primes.Length; i++)
            {
                int prime = primes[i];
                if (prime >= min) return prime;
            }
            for (int i = (min | 1); i < Int32.MaxValue; i += 2)
            {
                if (IsPrime(i) && ((i - 1) % 101 != 0))
                    return i;
            }
            return min;
        }
        public static int ExpandPrime(int oldSize)
        {
            int newSize = 2 * oldSize;
            if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
            {
                return MaxPrimeArrayLength;
            }
            return GetPrime(newSize);
        }
    }
    public class _Dictionary
    {
        private struct Entry
        {
            public int hashCode;
            public int next;
            public string key;
            public string value;
        }
        private int[] buckets;
        private Entry[] entries;
        private int count;
        private int version;
        private int freeList;
        private int freeCount;
        private KeyCollection keys;
        private ValueCollection values;
        private IEqualityComparer<string> comparer;
        public _Dictionary() : this(0, null) { }
        public _Dictionary(int capacity) : this(capacity, null) { }
        public _Dictionary(IEqualityComparer<string> comparer) : this(0, comparer) { }
        public _Dictionary(int capacity, IEqualityComparer<string> comparer)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException();
            if (capacity > 0) Initialize(capacity);
            this.comparer = comparer ?? EqualityComparer<string>.Default;
        }
        public _Dictionary(IDictionary<string, string> dictionary) : this(dictionary, null) { }
        public _Dictionary(IDictionary<string, string> dictionary, IEqualityComparer<string> comparer) :
        this(dictionary != null ? dictionary.Count : 0, comparer)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException();
            }
            foreach (KeyValuePair<string, string> pair in dictionary)
            {
                Add(pair.Key, pair.Value);
            }
        }
        public void Clear()
        {
            if (count > 0)
            {
                for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
                Array.Clear(entries, 0, count);
                freeList = -1;
                count = 0;
                freeCount = 0;
                version++;
            }
        }
        public IEqualityComparer<string> Comparer
        {
            get
            {
                return comparer;
            }
        }
        public bool ContainsKey(string key)
        {
            return FindEntry(key) >= 0;
        }
        public bool ContainsValue(string value)
        {
            if (value == null)
            {
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && entries[i].value == null) return true;
                }
            }
            else
            {
                EqualityComparer<string> c = EqualityComparer<string>.Default;
                for (int i = 0; i < count; i++)
                {
                    if (entries[i].hashCode >= 0 && c.Equals(entries[i].value, value)) return true;
                }
            }
            return false;
        }
        public int Count
        {
            get { return count - freeCount; }
        }
        public KeyCollection Keys
        {
            get
            {
                if (keys == null) keys = new KeyCollection(this);
                return keys;
            }
        }
        public ValueCollection Values
        {
            get
            {
                if (values == null) values = new ValueCollection(this);
                return values;
            }
        }
        public void _CopyTo(KeyValuePair[] array, int index)
        {
            if (array == null)
            {
                throw new ArgumentNullException();
            }

            if (index < 0 || index > array.Length)
            {
                throw new ArgumentOutOfRangeException();
            }
            if (array.Length - index < Count)
            {
                throw new ArgumentException();
            }
            int count = this.count;
            Entry[] entries = this.entries;
            for (int i = 0; i < count; i++)
            {
                if (entries[i].hashCode >= 0)
                {
                    array[index++] = new KeyValuePair(entries[i].key, entries[i].value);
                }
            }
        }
        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                int bucket = hashCode % buckets.Length;
                int last = -1;
                for (int i = buckets[bucket]; i >= 0; last = i, i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                    {
                        if (last < 0)
                        {
                            buckets[bucket] = entries[i].next;
                        }
                        else
                        {
                            entries[last].next = entries[i].next;
                        }
                        entries[i].hashCode = -1;
                        entries[i].next = freeList;
                        entries[i].key = default(string);
                        entries[i].value = default(string);
                        freeList = i;
                        freeCount++;
                        version++;
                        return true;
                    }
                }
            }
            return false;
        }
        public string this[string key]
        {
            get
            {
                int i = FindEntry(key);
                if (i >= 0) return entries[i].value;
                throw new KeyNotFoundException();
            }
            set
            {
                Insert(key, value, false);
            }
        }
        public void Add(string key, string value)
        {
            Insert(key, value, true);
        }
        private void Resize()
        {
            Resize(_HashHelpers.ExpandPrime(count), false);
        }
        private void Resize(int newSize, bool forceNewHashCodes)
        {
            int[] newBuckets = new int[newSize];
            for (int i = 0; i < newBuckets.Length; i++) newBuckets[i] = -1;
            Entry[] newEntries = new Entry[newSize];
            Array.Copy(entries, 0, newEntries, 0, count);
            if (forceNewHashCodes)
            {
                for (int i = 0; i < count; i++)
                {
                    if (newEntries[i].hashCode != -1)
                    {
                        newEntries[i].hashCode = (comparer.GetHashCode(newEntries[i].key) & 0x7FFFFFFF);
                    }
                }
            }
            for (int i = 0; i < count; i++)
            {
                if (newEntries[i].hashCode >= 0)
                {
                    int bucket = newEntries[i].hashCode % newSize;
                    newEntries[i].next = newBuckets[bucket];
                    newBuckets[bucket] = i;
                }
            }
            buckets = newBuckets;
            entries = newEntries;
        }
        private void Insert(string key, string value, bool add)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            if (buckets == null) Initialize(0);
            int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
            int targetBucket = hashCode % buckets.Length;
            int collisionCount = 0;
            for (int i = buckets[targetBucket]; i >= 0; i = entries[i].next)
            {
                if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key))
                {
                    if (add)
                    {
                        throw new ArgumentException();
                    }
                    entries[i].value = value;
                    version++;
                    return;
                }
                collisionCount++;
            }
            int index;
            if (freeCount > 0)
            {
                index = freeList;
                freeList = entries[index].next;
                freeCount--;
            }
            else
            {
                if (count == entries.Length)
                {
                    Resize();
                    targetBucket = hashCode % buckets.Length;
                }
                index = count;
                count++;
            }
            entries[index].hashCode = hashCode;
            entries[index].next = buckets[targetBucket];
            entries[index].key = key;
            entries[index].value = value;
            buckets[targetBucket] = index;
            version++;
        }
        private void Initialize(int capacity)
        {
            int size = _HashHelpers.GetPrime(capacity);
            buckets = new int[size];
            for (int i = 0; i < buckets.Length; i++) buckets[i] = -1;
            entries = new Entry[size];
            freeList = -1;
        }
        private int FindEntry(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException();
            }
            if (buckets != null)
            {
                int hashCode = comparer.GetHashCode(key) & 0x7FFFFFFF;
                for (int i = buckets[hashCode % buckets.Length]; i >= 0; i = entries[i].next)
                {
                    if (entries[i].hashCode == hashCode && comparer.Equals(entries[i].key, key)) return i;
                }
            }
            return -1;
        }
        public class ValueCollection
        {
            private _Dictionary dictionary;
            public ValueCollection(_Dictionary dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }
                this.dictionary = dictionary;
            }
            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }
            public struct Enumerator : IEnumerator<string>
            {
                private _Dictionary dictionary;
                private int index;
                private int version;
                private string currentValue;
                public Enumerator(_Dictionary dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentValue = default(string);
                }
                public void Dispose()
                {
                }
                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }

                    while ((uint)index < (uint)dictionary.count)
                    {
                        if (dictionary.entries[index].hashCode >= 0)
                        {
                            currentValue = dictionary.entries[index].value;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = dictionary.count + 1;
                    currentValue = default(string);
                    return false;
                }
                public string Current
                {
                    get
                    {
                        return currentValue;
                    }
                }
                Object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.count + 1))
                        {
                            throw new InvalidOperationException();
                        }
                        return currentValue;
                    }
                }
                void System.Collections.IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }
                    index = 0;
                    currentValue = default(string);
                }
            }
        }
        public class KeyCollection
        {
            private _Dictionary dictionary;
            public KeyCollection(_Dictionary dictionary)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException();
                }
                this.dictionary = dictionary;
            }
            public Enumerator GetEnumerator()
            {
                return new Enumerator(dictionary);
            }
            public struct Enumerator : IEnumerator<string>
            {
                private _Dictionary dictionary;
                private int index;
                private int version;
                private string currentKey;
                public Enumerator(_Dictionary dictionary)
                {
                    this.dictionary = dictionary;
                    version = dictionary.version;
                    index = 0;
                    currentKey = default(string);
                }
                public void Dispose()
                {
                }
                public bool MoveNext()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }

                    while ((uint)index < (uint)dictionary.count)
                    {
                        if (dictionary.entries[index].hashCode >= 0)
                        {
                            currentKey = dictionary.entries[index].key;
                            index++;
                            return true;
                        }
                        index++;
                    }
                    index = dictionary.count + 1;
                    currentKey = default(string);
                    return false;
                }
                public string Current
                {
                    get
                    {
                        return currentKey;
                    }
                }
                Object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        if (index == 0 || (index == dictionary.count + 1))
                        {
                            throw new InvalidOperationException();
                        }
                        return currentKey;
                    }
                }
                void System.Collections.IEnumerator.Reset()
                {
                    if (version != dictionary.version)
                    {
                        throw new InvalidOperationException();
                    }

                    index = 0;
                    currentKey = default(string);
                }
            }
        }
        public struct KeyValuePair
        {
            private string key;
            private string value;

            public KeyValuePair(string key, string value)
            {
                this.key = key;
                this.value = value;
            }
            public string Key
            {
                get { return key; }
            }
            public string Value
            {
                get { return value; }
            }
            public override string ToString()
            {
                StringBuilder s = new StringBuilder();
                s.Append('[');
                if (Key != null)
                {
                    s.Append(Key.ToString());
                }
                s.Append(", ");
                if (Value != null)
                {
                    s.Append(Value.ToString());
                }
                s.Append(']');
                return s.ToString();
            }
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            _Dictionary dic = new _Dictionary();
            _Dictionary dic1 = new _Dictionary(10);
            _Dictionary dic2 = new _Dictionary(EqualityComparer<string>.Default);
            _Dictionary dic4 = new _Dictionary(new Dictionary<string, string> { { "dha", "bha" }, { "dfd", "ddd" } },
            EqualityComparer<string>.Default);
            //Keys=================================
            foreach (string key in dic4.Keys)
            {
                Console.WriteLine(key);
            }
            //Value================================
            foreach (string value in dic4.Values)
            {
                Console.WriteLine(value);
            }
            //Count================================
            Console.WriteLine(dic4.Count);
            //Clear================================
            Dictionary<string, string> dic5 = new Dictionary<string, string>();
            dic5.Add("a", "b");
            dic5.Add("c", "d");
            dic5.Add("d", "e");
            dic5.Clear();
            dic5.Add("e", "f");
            dic5.Add("g", "h");
            foreach (var item in dic5)
            {
                Console.WriteLine(item);
            }
            //ContainsKey True======================
            Console.WriteLine(dic5.ContainsKey("e"));
            //ContainsKey False=====================
            Console.WriteLine(dic5.ContainsKey("a"));
            //ContainsValue True====================
            Console.WriteLine(dic5.ContainsValue("f"));
            //ContainsValue False===================
            Console.WriteLine(dic5.ContainsValue("z"));
            //Remove================================
            dic5.Remove("e");
            foreach (var item in dic5)
            {
                Console.WriteLine(item);
            }
        }
    }
}
