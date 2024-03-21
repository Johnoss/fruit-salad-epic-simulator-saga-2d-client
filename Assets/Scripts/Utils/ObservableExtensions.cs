using System;
using UniRx;

namespace Utils
{
    public static class ObservableExtensions
    {
        public static IDisposable SubscribeBlind<T>(this IObservable<T> observable, Action action)
        {
            return observable.Subscribe(_ => action());
        }
        
        public static IObservable<bool> WhereTrue(this IObservable<bool> observable)
        {
            return observable.Where(value => value);
        }
        
        public static IObservable<bool> WhereFalse(this IObservable<bool> observable)
        {
            return observable.Where(value => !value);
        }
    }
}