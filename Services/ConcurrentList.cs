using System.Collections;

namespace OverflowBackend.Services
{
    public class ConcurrentList<T>
    {
        private readonly List<T> _list = new List<T>();
        private readonly object _lock = new object(); // Synchronization object

        // Add an item to the list in a thread-safe manner
        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        // Remove an item from the list in a thread-safe manner
        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _list.Remove(item);
            }
        }

        // Get an item by index in a thread-safe manner
        public T Get(int index)
        {
            lock (_lock)
            {
                if (index >= 0 && index < _list.Count)
                {
                    return _list[index];
                }
                throw new ArgumentOutOfRangeException(nameof(index), "Index is out of range");
            }
        }

        // Get the count of the list in a thread-safe manner
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _list.Count;
                }
            }
        }

        // Enumerate through the list in a thread-safe manner
        public IEnumerable<T> GetItems()
        {
            lock (_lock)
            {
                // Returning a copy of the list to avoid modification during iteration
                return new List<T>(_list);
            }
        }

        // Where method to filter items based on a predicate
        public ConcurrentList<T> Where(Func<T, bool> predicate)
        {
            var result = new ConcurrentList<T>();

            lock (_lock)
            {
                foreach (var item in _list)
                {
                    if (predicate(item))
                    {
                        result.Add(item);
                    }
                }
            }

            return result;
        }

        // FirstOrDefault method to find the first item matching a predicate or return default value
        public T FirstOrDefault(Func<T, bool> predicate)
        {
            lock (_lock)
            {
                foreach (var item in _list)
                {
                    if (predicate(item))
                    {
                        return item;
                    }
                }
            }
            return default;
        }

        public bool Any(Func<T, bool> predicate = null)
        {
            lock (_lock)
            {
                if (predicate == null)
                {
                    // If no predicate is provided, check if the list contains any elements
                    return _list.Count > 0;
                }

                // Check if any item matches the predicate
                foreach (var item in _list)
                {
                    if (predicate(item))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
