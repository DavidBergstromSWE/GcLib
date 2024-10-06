using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace GcLib.Utilities.Collections;

/// <summary>
/// Generic circular buffer for .NET (based on <see href="http://circularbuffer.codeplex.com"/>)
/// </summary>
/// <typeparam name="T">Type.</typeparam>
public class CircularBuffer<T> : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable
{
    // backing-fields
    private int capacity;

    /// <summary>
    /// Position of last added element.
    /// </summary>
    private int head;

    /// <summary>
    /// Position of first added element.
    /// </summary>
    private int tail;

    /// <summary>
    /// Array containing elements.
    /// </summary>
    private T[] buffer;

    [NonSerialized()]
    private object syncRoot;

    /// <summary>
    /// Instantiates a new empty circular buffer, with specified capacity and buffer overflow setting.
    /// </summary>
    /// <param name="capacity">Capacity (number of buffers) of circular buffer.</param>
    /// <param name="allowOverflow">True if overwriting of buffers is allowed (default), false if not.</param>
    public CircularBuffer(int capacity, bool allowOverflow = true)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be larger than zero!");

        this.capacity = capacity;
        Size = 0;
        head = 0;
        tail = 0;
        buffer = new T[capacity];
        AllowOverflow = allowOverflow;
    }

    /// <summary>
    /// Indicates if overflow (overwriting of old buffers) is allowed.
    /// </summary>
    public bool AllowOverflow { get; set; }

    /// <summary>
    /// Capacity (number of buffers) of circular buffer.
    /// </summary>
    public int Capacity
    {
        get => capacity;
        set
        {
            if (value == capacity)
                return;

            ArgumentOutOfRangeException.ThrowIfLessThan(value, Size);

            var dst = new T[value];
            if (Size > 0)
                CopyTo(dst);
            buffer = dst;

            capacity = value;
        }
    }

    /// <summary>
    /// Current number of buffers in circular buffer.
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    /// Gets a value that indicates whether the circular buffer is empty.
    /// </summary>
    public bool IsEmpty => Size == 0;

    /// <summary>
    /// Checks if circular buffer contains a specific item.
    /// </summary>
    /// <param name="item">Item.</param>
    /// <returns>True if circular buffer contains item, false if not.</returns>
    public bool Contains(T item)
    {
        int bufferIndex = head;
        EqualityComparer<T> comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < Size; i++, bufferIndex++)
        {
            if (bufferIndex == capacity)
                bufferIndex = 0;

            if (item == null && buffer[bufferIndex] == null)
                return true;
            else if ((buffer[bufferIndex] != null) &&
                comparer.Equals(buffer[bufferIndex], item))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Clears circular buffer.
    /// </summary>
    public void Clear()
    {
        Size = 0;
        head = 0;
        tail = 0;
    }

    /// <summary>
    /// Queue array of items.
    /// </summary>
    /// <param name="src">Array of items.</param>
    /// <returns>Number of items successfully queued.</returns>
    public int Put(T[] src)
    {
        return Put(src, 0, src.Length);
    }

    /// <summary>
    /// Queue array of items, using an offset from the beginning of the queue.
    /// </summary>
    /// <param name="src">Array of items.</param>
    /// <param name="offset">Offset.</param>
    /// <param name="count">Number of items.</param>
    /// <returns>Number of items successfully queued.</returns>
    public int Put(T[] src, int offset, int count)
    {
        if (!AllowOverflow && count > capacity - Size)
            throw new InvalidOperationException();

        int srcIndex = offset;
        for (int i = 0; i < count; i++, tail++, srcIndex++)
        {
            if (tail == capacity)
                tail = 0;
            buffer[tail] = src[srcIndex];
        }
        Size = Math.Min(Size + count, capacity);
        return count;
    }

    /// <summary>
    /// Queue single item.
    /// </summary>
    /// <param name="item">Item to be queued.</param>
    public void Put(T item)
    {
        if (!AllowOverflow && Size == capacity)
            throw new InvalidOperationException();

        buffer[tail] = item;
        if (++tail == capacity)
            tail = 0;
        if (Size < Capacity)
            Size++;
    }

    /// <summary>
    /// Skip items in queue.
    /// </summary>
    /// <param name="count">Number of items to skip.</param>
    public void Skip(int count)
    {
        head += count;
        if (head >= capacity)
            head -= capacity;
    }

    /// <summary>
    /// Dequeue array of items.
    /// </summary>
    /// <param name="count">Number of items to dequeue.</param>
    /// <returns>Array of items.</returns>
    public T[] Get(int count)
    {
        var dst = new T[count];
        Get(dst);
        return dst;
    }

    /// <summary>
    /// Dequeue array of items into pre-allocated memory.
    /// </summary>
    /// <param name="dst">(Pre-allocated) array of items.</param>
    /// <returns>Number of items successfully dequeued.</returns>
    public int Get(T[] dst)
    {
        return Get(dst, 0, dst.Length);
    }

    /// <summary>
    /// Dequeue array of items into pre-allocated memory, using an offset from the beginning of the queue.
    /// </summary>
    /// <param name="dst">(Pre-allocated) array of items.</param>
    /// <param name="offset">Offset.</param>
    /// <param name="count">Number of items.</param>
    /// <returns>Number of items successfully dequeued.</returns>
    public int Get(T[] dst, int offset, int count)
    {
        int realCount = Math.Min(count, Size);
        int dstIndex = offset;
        for (int i = 0; i < realCount; i++, head++, dstIndex++)
        {
            if (head == capacity)
                head = 0;
            dst[dstIndex] = buffer[head];
        }
        Size -= realCount;
        return realCount;
    }

    /// <summary>
    /// Dequeue single item.
    /// </summary>
    /// <returns>Item.</returns>
    public T Get()
    {
        if (Size == 0)
            return default;

        T item = buffer[head];
        if (++head == capacity)
            head = 0;
        Size--;
        return item;
    }

    /// <summary>
    /// Copies array of items to beginning of queue.
    /// </summary>
    /// <param name="array">Array of items.</param>
    public void CopyTo(T[] array)
    {
        CopyTo(array, 0);
    }

    /// <summary>
    /// Copies array of items to queue at specified starting index.
    /// </summary>
    /// <param name="array">Array of items.</param>
    /// <param name="arrayIndex">Starting index.</param>
    public void CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(array, arrayIndex, Size);
    }

    /// <summary>
    /// Copies specified number of items from array to queue at specified starting index.
    /// </summary>
    /// <param name="array">Array of items.</param>
    /// <param name="arrayIndex">Starting index.</param>
    /// <param name="count">Number of items.</param>
    public void CopyTo(T[] array, int arrayIndex, int count)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, Size);

        int bufferIndex = head;
        for (int i = 0; i < count; i++, bufferIndex++, arrayIndex++)
        {
            if (bufferIndex == capacity)
                bufferIndex = 0;
            array[arrayIndex] = buffer[bufferIndex];
        }
    }

    /// <summary>
    /// Retrieves enumerator.
    /// </summary>
    /// <returns>Enumerator.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        int bufferIndex = head;
        for (int i = 0; i < Size; i++, bufferIndex++)
        {
            if (bufferIndex == capacity)
                bufferIndex = 0;

            yield return buffer[bufferIndex];
        }
    }

    /// <summary>
    /// Retrieves all buffers currently in queue.
    /// </summary>
    /// <returns></returns>
    public T[] GetBuffer()
    {
        return buffer;
    }

    /// <summary>
    /// Returns buffers in queue as an array.
    /// </summary>
    /// <returns></returns>
    public T[] ToArray()
    {
        var dst = new T[Size];
        CopyTo(dst);
        return dst;
    }

    #region ICollection<T> Members

    int ICollection<T>.Count
    {
        get { return Size; }
    }

    bool ICollection<T>.IsReadOnly
    {
        get { return false; }
    }

    void ICollection<T>.Add(T item)
    {
        Put(item);
    }

    bool ICollection<T>.Remove(T item)
    {
        if (Size == 0)
            return false;

        Get();
        return true;
    }

    #endregion

    #region IEnumerable<T> Members

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region ICollection Members

    int ICollection.Count
    {
        get { return Size; }
    }

    bool ICollection.IsSynchronized
    {
        get { return false; }
    }

    object ICollection.SyncRoot
    {
        get
        {
            if (syncRoot == null)
                Interlocked.CompareExchange(ref syncRoot, new object(), null);
            return syncRoot;
        }
    }

    void ICollection.CopyTo(Array array, int arrayIndex)
    {
        CopyTo((T[])array, arrayIndex);
    }

    #endregion

    #region IEnumerable Members

    IEnumerator IEnumerable.GetEnumerator()
    {
        return (IEnumerator)GetEnumerator();
    }

    #endregion
}