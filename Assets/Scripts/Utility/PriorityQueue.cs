using System;
using System.Collections.Generic;

public class PriorityQueue<T> {
    public struct HeapData {
        public int priority;
        public T value;

        public HeapData(int priority, T value) {
            this.priority = priority;
            this.value = value;
        }
    }
    // heap structure for priority queue
    public List<HeapData> heap = new List<HeapData>();

    // dictionary for removing data <data, int>
    public Dictionary<T, int> data = new Dictionary<T, int>();
    
    // -1 for min-heap, 1 for max-hip
    private int compare = 0;

    // Last index
    private int tailIndex = 0;

    // For first index in heap
    public const int NegativeInfinity = int.MinValue / 2;

    public PriorityQueue(char a) {
        heap.Add(new HeapData(NegativeInfinity, default(T)));

        if (a == 'l') this.compare = -1;
        else if (a == 'g') this.compare = 1;
    }

    public void Clear() {
        heap.Clear();
        heap.Add(new HeapData(NegativeInfinity, default(T)));
        data.Clear();
    }

    public void Enqueue(int priority, T value) {
        heap.Add(new HeapData(priority, value));
        tailIndex++;
        data.Add(heap[tailIndex].value, tailIndex);

        int currentIndex = tailIndex;
        while (currentIndex > 1) {
            int parentIndex = currentIndex / 2;
            if (heap[currentIndex].priority.CompareTo(heap[parentIndex].priority) == compare) {
                Swap(currentIndex, parentIndex);
                currentIndex = parentIndex;
            }
            else break;
        }
    }

    public T Dequeue() {
        if (tailIndex < 1) return default(T);

        T result = heap[1].value;

        Swap(1, tailIndex);

        data.Remove(heap[tailIndex].value);
        heap.RemoveAt(tailIndex);
        tailIndex--;

        if (tailIndex > 0) {
            ShiftDown(1);
        }

        return result;
    }

    public void Remove(T value) {
        if (!data.ContainsKey(value)) {
            return;
        }
        if (data[value] == tailIndex) {
            data.Remove(heap[tailIndex].value);
            heap.RemoveAt(tailIndex);
            tailIndex--;
            return;
        }

        Swap(data[value], tailIndex);

        data.Remove(heap[tailIndex].value);
        heap.RemoveAt(tailIndex);
        tailIndex--;

        // If removed node is an ancesstor of tail node then shift down
        ShiftDown(data[value]);

        // If removed node isn't an ancesstor of tail node tail node can be greater(less) than child node
        ShiftUp(data[value]);

    }

    // DONT USE THIS WHEN YOU HAVE MUTIPLE SAME DATA IN PQ
    public void Update(T currentValue, int changePriority, T changeValue) {
        if (!data.TryGetValue(currentValue, out int index)) {
            return;
        }

        if (!currentValue.Equals(changeValue) && data.ContainsKey(changeValue)) {
            return;
        }

        heap[index] = new HeapData(changePriority, changeValue);

        data.Remove(currentValue);
        data[changeValue] = index;

        ShiftDown(index);
        ShiftUp(index);
        return;   
    }

    private void ShiftUp(int index) {
        int parent = index / 2;
        while (parent != 0) {
            if (heap[index].priority.CompareTo(heap[parent].priority) == compare) {
                Swap(index, parent);
                index = parent;
                parent = index / 2;
            }
            else {
                break;
            }
        } 
    }

    private void ShiftDown(int index) {
        int currentIndex = index;

        while (currentIndex * 2 <= tailIndex) {
            int leftChildIndex = currentIndex * 2;
            int rightChildIndex = currentIndex * 2 + 1;
            int compareIndex = leftChildIndex;

            if (rightChildIndex <= tailIndex &&
                heap[rightChildIndex].priority.CompareTo(heap[leftChildIndex].priority) == compare) {
                compareIndex = rightChildIndex;
            }

            if (heap[compareIndex].priority.CompareTo(heap[currentIndex].priority) == compare) {
                Swap(currentIndex, compareIndex);
                currentIndex = compareIndex;
                leftChildIndex = currentIndex * 2;
                rightChildIndex = currentIndex * 2 + 1;
            }
            else {
                break;
            }
        }
    }

    private void Swap(int A, int B) {
        HeapData temp = heap[A];

        heap[A] = heap[B];
        heap[B] = temp;

        data[heap[A].value] = A;
        data[heap[B].value] = B;
    }
}
