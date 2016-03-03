#define DEBUG
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace prime_threads_test
{
    class Program
    {
        static ulong estimatedNthPrime;
        static int threadsComplete = 0;
        static List<ulong> primes = new List<ulong>();
        static List<ulong> primesCache = new List<ulong>();

        static void Main()
        {
            Console.Title = "Prime Threads 2";

            while (true)
            {
                primes.Clear();
                primesCache.Clear();
                threadsComplete = 0;

                Console.Write("Number of primes to find: ");

                string input = Console.ReadLine();

                if (input.ToLower() == "quit" || input.ToLower() == "exit")
                    break;

                int numOfPrimes;
                try
                {
                    numOfPrimes = int.Parse(input);
                    if (numOfPrimes < 1)
                        throw new Exception();
                }
                catch
                {
                    Console.WriteLine("wat");
                    continue;
                }

                if (numOfPrimes <= 7022)
                {
                    findPrimesSimple(numOfPrimes);
                    continue;
                }

                //Console.Write("Number of threads? (1, 2, 4, 8): ");
                //input = Console.ReadLine();
                //int numOfCores;
                //while (!int.TryParse(input, out numOfCores) || (numOfCores != 1 && numOfCores != 2 && numOfCores != 4 && numOfCores != 8))
                //{
                //    Console.Write("Number of threads? (1, 2, 4, 8): ");
                //    input = Console.ReadLine();
                //}

                primes.Capacity = (int)(numOfPrimes * 1.1f);

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                estimatedNthPrime = (ulong)(numOfPrimes * Math.Log(numOfPrimes) + numOfPrimes * (Math.Log(Math.Log(numOfPrimes)) - 0.9385));

                var tasks = new List<Task>();

                float intervalPercent = .01f;
                float lowerBoundPercent = 0f;
                float upperBoundPercent = lowerBoundPercent + intervalPercent;
                ulong lowerBoundValue = (ulong)(estimatedNthPrime * lowerBoundPercent);
                ulong upperBoundValue = (ulong)(estimatedNthPrime * upperBoundPercent);
                tasks.Add(Task.Run(() => findPrimesWithCache(upperBoundValue, "")));
                for (; upperBoundValue <= estimatedNthPrime; )
                {
                    intervalPercent -= intervalPercent * .001f;
                    lowerBoundPercent = upperBoundPercent;
                    upperBoundPercent += intervalPercent;

                    lowerBoundValue = (ulong)(estimatedNthPrime * lowerBoundPercent);
                    upperBoundValue = (ulong)(estimatedNthPrime * upperBoundPercent);

                    //var lowerBound = (ulong)(estimatedNthPrime * ((float)i / NUM_OF_THREADS));
                    //var upperBound = (ulong)(estimatedNthPrime * ((float)(i + 1) / NUM_OF_THREADS));
                    //var threadName = i.ToString();

                    tasks.Add(Task.Run(() => findPrimes(lowerBoundValue, upperBoundValue, "")));
                }

                Task.WaitAll(tasks.ToArray());

                primes.Sort();

                var t = new CustomTimeSpan(stopWatch.ElapsedMilliseconds);

                Console.WriteLine("\nPrime number " + numOfPrimes + " is " + primes.ElementAt(numOfPrimes - 1));
                Console.WriteLine("Total number of primes found: " + primes.Count + ". (" + (primes.Count - numOfPrimes) + " extra)");
                Console.WriteLine("Time elapsed: " + t + " \n");
            }
        }

        static void findPrimesSimple(int n)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (n == 0)
                return;

            primes.Add(2);
            primes.Add(3);

            for (ulong d = 5; primes.Count < n; d += 2)
            {
                if (IsPrime(d, primes))
                {
                    primes.Add(d);
                }
            }

            CustomTimeSpan time = new CustomTimeSpan(stopwatch.ElapsedMilliseconds);
            Console.WriteLine("\nPrime number " + n + " is " + primes.ElementAt(n - 1));
            Console.WriteLine("Total number of primes found: " + primes.Count + ". (" + (primes.Count - n) + " extra)");
            Console.WriteLine("Time elapsed: " + time + "\n");
        }

        static void findPrimesWithCache(ulong end, string threadName)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //List<ulong> localPrimes = new List<ulong>();
            //localPrimes.Add(2);
            primesCache.Add(2);
            primesCache.Add(3);

            for (ulong n = 5; n < end; n += 2)
            {
                if (IsPrime(n, primesCache))
                {
                    primesCache.Add(n);
                }
            }

            lock (primes)
            {
                foreach (ulong prime in primesCache)
                    primes.Add(prime);
            }

            ++threadsComplete;

#if DEBUG
            CustomTimeSpan time = new CustomTimeSpan(stopwatch.ElapsedMilliseconds);
            Console.WriteLine("thread " + threadName + " for 3 - " + end + " completed in " + time);
#endif

            //Thread.CurrentThread.Abort();
        }

        static void findPrimes(ulong start, ulong end, string threadName)
        {
            Thread.Sleep(5);
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (start % 2 == 0)
                ++start;

            LinkedList<ulong> localPrimes = new LinkedList<ulong>();

            for (ulong n = start; n < end; n += 2)
            {
                if (IsPrime(n, primesCache))
                {
                    localPrimes.AddLast(n);
                }
            }

            lock (primes)
            {
                foreach (ulong prime in localPrimes)
                    primes.Add(prime);
            }

            ++threadsComplete;

#if DEBUG
            CustomTimeSpan time = new CustomTimeSpan(stopwatch.ElapsedMilliseconds);
            Console.WriteLine("thread " + threadName + " for " + start + " - " + end + " completed in " + time);
#endif

            //Thread.CurrentThread.Abort();
        }

        static bool IsPrime(ulong n)
        {
            if (n != 2 && n % 2 == 0)
                return false;

            ulong sqrtn = (ulong)Math.Sqrt(n);

            for (ulong i = 3; i <= sqrtn; i += 2)
                if (n % i == 0)
                    return false;

            return true;
        }

        static bool IsPrime(ulong n, List<ulong> primes)
        {
            ulong sqrtn = (ulong)Math.Sqrt(n);

            for (var i = 1; primes[i] <= sqrtn; ++i)
            {
                if (n % primes[i] == 0)
                    return false;
            }

            return true;
        }
    }

    class CustomTimeSpan
    {
        private long hours, minutes, milliseconds;
        private double seconds;

        public CustomTimeSpan()
        {
            hours = minutes = 0;
            seconds = 0.0;
        }
        public CustomTimeSpan(long ms)
        {
            hours = ms / (1000 * 60 * 60);
            ms = ms % (1000 * 60 * 60);
            minutes = ms / (1000 * 60);
            ms = ms % (1000 * 60);
            seconds = ms / 1000.0;
            milliseconds = ms;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (hours > 0)
                result.Append(hours + (hours > 1 ? " hours, " : " hour, "));
            if (minutes > 0)
                result.Append(minutes + (minutes > 1 ? " minutes, " : " minute, "));
            if (seconds >= 1)
                result.Append(seconds + " seconds.");
            else
                result.Append(milliseconds + " milliseconds.");
            return result.ToString();
        }
    }
}