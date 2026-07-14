using System;
using System.Collections.Generic;

public class PriorityQueue<T> {
    public struct HeapData {
        public int priority;
        public long sequence;
        public T value;

        public HeapData(int priority, long sequence, T value) {
            this.priority = priority;
            this.sequence = sequence;
            this.value = value;
        }
    }
    public enum HeapType {
        max,
        min
    }

    public List<HeapData> heap = new List<HeapData>();
    public Dictionary<T, int> data = new Dictionary<T, int>();
    private int compare = 0;
    private int tailIndex = 0;
    private long sequenceCounter = 0;

    public PriorityQueue(HeapType type) {
        heap.Add(new HeapData(0, sequenceCounter, default(T)));
        sequenceCounter++;

        if (type == HeapType.min) this.compare = -1;
        else if (type == HeapType.max) this.compare = 1;
    }

    public void Clear() {
        heap.Clear();
        heap.Add(new HeapData(0, 0, default(T)));
        tailIndex = 0;
        data.Clear();
    }

    public void Enqueue(int priority, T value) {
        heap.Add(new HeapData(priority, sequenceCounter, value));
        sequenceCounter++;
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

        data.Remove(heap[tailIndex].value);
        heap.RemoveAt(tailIndex);
        tailIndex--;

        ShiftDown(index);
        ShiftUp(index);
    }

    public void Update(T currentValue, int changePriority, T changeValue) {
        long currentSequnce;
        if (!data.TryGetValue(currentValue, out int index)) {
            return;
        }

        if (!currentValue.Equals(changeValue) && data.ContainsKey(changeValue)) {
            return;
        }

        if (heap[index].priority == changePriority) {
            currentSequnce = sequenceCounter;
            sequenceCounter++;
        }
        else {
            currentSequnce = heap[index].sequence;
        }

        heap[index] = new HeapData(changePriority, currentSequnce, changeValue);

        data.Remove(currentValue);
        data[changeValue] = index;

        ShiftDown(index);
        ShiftUp(index);
    }

    public int GetFirstPriority() {
        return heap[1].priority;
    }

    public int Count {
        get { return tailIndex; }
    }

    /// <summary>
    /// [확장 추가] 대기열을 파괴하지 않고 내부 요소를 시간 오름차순으로 정렬하여 반환합니다.
    /// </summary>
    public List<KeyValuePair<int, T>> GetAllElements() {
        List<KeyValuePair<int, T>> elements = new List<KeyValuePair<int, T>>();
        for (int i = 1; i <= tailIndex; i++) {
            if (heap[i].value != null) {
                elements.Add(new KeyValuePair<int, T>(heap[i].priority, heap[i].value));
            }
        }
        elements.Sort((x, y) => x.Key.CompareTo(y.Key));
        return elements;
    }

    private void ShiftUp(int index) {
        int parent = index / 2;
        while (parent != 0) {
            int prioritycmp = heap[index].priority.CompareTo(heap[parent].priority);
            if (prioritycmp == -compare) break;
            if (prioritycmp == 0) {
                if (heap[index].sequence > heap[parent].sequence) break;
            }

            Swap(index, parent);
            index = parent;
            parent = index / 2;
        }
    }

    private void ShiftDown(int index) {
        int currentIndex = index;

        while (currentIndex * 2 <= tailIndex) {
            int leftChildIndex = currentIndex * 2;
            int rightChildIndex = currentIndex * 2 + 1;
            int compareIndex = leftChildIndex;

            if (rightChildIndex <= tailIndex) {
                if (heap[rightChildIndex].priority.CompareTo(heap[leftChildIndex].priority) == compare) {
                    compareIndex = rightChildIndex;
                }
                else if (heap[rightChildIndex].priority.CompareTo(heap[leftChildIndex].priority) == 0 &&
                    heap[rightChildIndex].sequence < heap[leftChildIndex].sequence) {
                    compareIndex = rightChildIndex;
                }
            }

            int prioritycmp = heap[compareIndex].priority.CompareTo(heap[currentIndex].priority);
            if (prioritycmp == -compare) break;
            if (prioritycmp == 0) {
                if (heap[compareIndex].sequence > heap[currentIndex].sequence) break;
            }

            Swap(compareIndex, currentIndex);
            currentIndex = compareIndex;
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