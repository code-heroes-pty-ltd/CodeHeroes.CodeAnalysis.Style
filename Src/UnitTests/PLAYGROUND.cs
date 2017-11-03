//namespace System.Reactive.Linq
//{
//    using System;
//    using Genesis.Ensure;

//    public static class WhereExtensions
//    {
//        public static IObservable<T> WhereNotNull<T>(this IObservable<T> @this)
//            where T : class
//        {
//            Ensure.ArgumentNotNull(@this, nameof(@this));

//            return @this
//                .Where(_ => _ != null);
//        }

//        public static IObservable<bool> WhereTrue(this IObservable<bool> @this)
//        {
//            Ensure.ArgumentNotNull(@this, nameof(@this));

//            return @this
//                .Where(item => item);
//        }

//        public static IObservable<bool> WhereFalse(this IObservable<bool> @this)
//        {
//            Ensure.ArgumentNotNull(@this, nameof(@this));

//            return @this
//                .Where(item => !item);
//        }
//    }
//}