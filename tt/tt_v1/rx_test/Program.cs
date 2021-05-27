using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;

namespace rx_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var one = Observable.Range(1, 1000).Publish();
            var two = Observable.Range(1001, 10000).Publish();
            var three = one.Merge(two).Publish().RefCount();
            var obs = Observable.Interval(TimeSpan.FromMilliseconds(200)).Publish();
            // obs.Subscribe(d => Console.WriteLine(d));
            
            Thread.Sleep(2000);

            var sequence = Observable.Create<long>((observer) =>
            {
                // obs.Connect();
                three.
                obs.Subscribe(observer.OnNext, observer.OnError);
                
                return Disposable.Empty;
            });

            sequence.Subscribe(Console.WriteLine);
            Console.ReadLine();

        }
    }
}