using System;

public static class EventBus<T>{
    private static event Action<T> OnEvent;

    public static void Subscribe(Action<T> handler) {
        OnEvent += handler;
    }

    public static void Unsubscribe(Action<T> handler) {
        OnEvent -= handler;
    }

    public static void Publish(T eventData) {
        OnEvent?.Invoke(eventData);
    }
}
