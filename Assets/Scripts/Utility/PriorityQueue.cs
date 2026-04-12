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
    public enum HeapType { 
        max,
        min
    }
    // heap structure for priority queue
    public List<HeapData> heap = new List<HeapData>();

    // dictionary for removing data <data, int>
    public Dictionary<T, int> data = new Dictionary<T, int>();
    
    // -1 for min-heap, 1 for max-hip
    private int compare = 0;

    // Last index
    private int tailIndex = 0;

    public PriorityQueue(HeapType type) {
        heap.Add(new HeapData(0, default(T)));

        if (type == HeapType.min) this.compare = -1;
        else if (type == HeapType.max) this.compare = 1;
    }

    public void Clear() {
        heap.Clear();
        heap.Add(new HeapData(0, default(T)));
        tailIndex = 0;
        data.Clear();
    }

    public void Enqueue(int priority, T value) {
        heap.Add(new HeapData(priority, value));
        tailIndex++;
        data.Add(heap[tailIndex].value, tailIndex);

        ShiftUp(tailIndex);
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
        if (!data.TryGetValue(value, out int index)) {
            return;
        }

        if (index == tailIndex) {
            data.Remove(value);
            heap.RemoveAt(tailIndex);
            tailIndex--;
            return;
        }

        Swap(index, tailIndex);

        data.Remove(value);
        heap.RemoveAt(tailIndex);
        tailIndex--;

        // If removed node is an ancesstor of tail node then shift down
        ShiftDown(index);

        // If removed node isn't an ancesstor of tail node tail node can be greater(less) than child node
        ShiftUp(index);

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
