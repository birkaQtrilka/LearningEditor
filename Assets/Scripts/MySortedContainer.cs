using System.Collections.Generic;
using System;

class MySortedContainer<T> where T : IComparable<T>
{

    public int Count => _list.Count;

    public readonly List<T> _list = new List<T>();

    public MySortedContainer()
    {

    }

    public void Enqueue(T item)
    {
        _list.Add(item);

        int index = _list.Count - 1;

        while (index > 0 && _list[GetParent(index)].CompareTo(_list[index]) > 0)
        {
            //swap
            int parentI = GetParent(index);
            (_list[parentI], _list[index]) = (_list[index], _list[parentI]);

            index = parentI;
        }
    }

    void Heapify(int i, int listSize)
    {
        int leftChildIndex = GetLeftChild(i);
        int rightChildIndex = GetRightChild(i);
        int min = i;
        if (leftChildIndex < listSize && _list[leftChildIndex].CompareTo(_list[min]) < 0)//understand this a bit more
            min = leftChildIndex;
        if (rightChildIndex < listSize && _list[rightChildIndex].CompareTo(_list[min]) < 0)
            min = rightChildIndex;

        if (min == i) return;
        //swap
        (_list[i], _list[min]) = (_list[min], _list[i]);
        Heapify(min, listSize);
    }


    int GetParent(int i)
    {
        return (i - 1) / 2;
    }

    int GetLeftChild(int i)
    {
        return 2 * i + 1;
    }

    int GetRightChild(int i)
    {
        return 2 * i + 2;
    }

    public T Peek()
    {
        return _list[_list.Count - 1];
    }

    public T Dequeue()
    {
        int lastIndex = _list.Count - 1;
        T minElement = _list[0];
        //swap
        (_list[lastIndex], _list[0]) = (_list[0], _list[lastIndex]);

        _list.RemoveAt(lastIndex);

        int listSize = _list.Count;

        Heapify(0, listSize);

        return minElement;
    }

    public void Clear()
    {
        _list.Clear();
    }
}

