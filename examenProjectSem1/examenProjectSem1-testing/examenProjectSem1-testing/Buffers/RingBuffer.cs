namespace examenProjectSem1_testing.Buffers;

public class RingBuffer<T>(int size)
{
    private T[] Buffer = new T[size];
    private int WriteIndex = 0;
    private int ReadIndex = 0;
    private int _count = 0;
    private readonly int Size = size;

    public bool IsFull => _count == Size;
    public bool IsEmpty => _count == 0;
    public int Count => _count;

    public void Add(T item)
    {
        Buffer[WriteIndex] = item;
        WriteIndex = (WriteIndex + 1) % Size;
        if (_count == Size)
        {
            ReadIndex = (ReadIndex + 1) % Size; // Overwrite the oldest data
        }
        else
        {
            _count++;
        }
    }

    public T Read()
    {
        if (_count == 0)
        {
            throw new InvalidOperationException("Buffer is empty");
        }

        T item = Buffer[ReadIndex];
        ReadIndex = (ReadIndex + 1) % Size;
        _count--;
        return item;
    }
}
