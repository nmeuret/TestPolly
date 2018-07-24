using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Polly.Wrap;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        private const double DURATION_OF_BREAK = 30;
        static CircuitBreakerPolicy _circuitBreakerPolicy;
        static DateTime _circuitBreakTime;

        static void Main(string[] args)
        {
            // DEV
            _circuitBreakerPolicy = Policy
              .Handle<HttpRequestException>()
              .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromSeconds(DURATION_OF_BREAK),
                onBreak: (ex, breakDelay) =>
                {
                    Console.WriteLine("Circuit Breaker : onBreak");
                    _circuitBreakTime = DateTime.Now;
                },
                onReset: () => 
                {
                    Console.WriteLine("Circuit Breaker : onReset");
                },
                onHalfOpen: () =>
                {
                    Console.WriteLine("Circuit Breaker : onHalfOpen");
                });

            while (true)
            {
                try
                {
                    RunAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Main Exception: {ex.Message}");
                    Console.WriteLine("Main Continue !");
                }

                Thread.Sleep(1000);
            }
        }

        static double GetCircuitBreakerRemainingTime()
        {
            TimeSpan span = DateTime.Now - _circuitBreakTime;
            return Math.Truncate (DURATION_OF_BREAK - span.TotalSeconds);
        }

        static async Task RunAsync()
        {
            Console.WriteLine("Select Polly test : ");
            Console.WriteLine("1. Test Retries");
            Console.WriteLine("2. Test Timeout");
            Console.WriteLine("3. Test Circuit Breaker");
            Console.WriteLine("4. Isolate Circuit Breaker");
            string userChoice = Console.ReadLine();
            switch (userChoice)
            {
                case "1":
                    await TestRetries();
                    break;
                case "2":
                    await TestTimeout();
                    break;
                case "3":
                    await TestCircuitBreaker();
                    break;
                case "4":
                    _circuitBreakerPolicy.Isolate();
                    break;
            }
        }

        static async Task TestRetries()
        {
            var maxRetryAttempts = 2;
            var pauseBetweenFailures = TimeSpan.FromSeconds(2);

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                // .HandleResult<HttpResponseMessage>( r => r.StatusCode == HttpStatusCode.InternalServerError)
                .WaitAndRetryAsync(maxRetryAttempts, i => pauseBetweenFailures, (ex, timeSpan) =>
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Retrying in {timeSpan.Seconds} seconds");
                });

            while (true)
            {
                await retryPolicy.ExecuteAsync(async () =>
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync("http://localhost:9000/api/test/retries/");

                    response.EnsureSuccessStatusCode();

                    Console.WriteLine(response.StatusCode);

                });

                Thread.Sleep(1000);
            }
        }

        static async Task TestTimeout()
        {
            var httpClient = new HttpClient();
            CancellationTokenSource userCancellationSource = new CancellationTokenSource();
            Policy timeoutPolicy = Policy.TimeoutAsync(10, TimeoutStrategy.Optimistic);

            Console.WriteLine("Waiting anwser for 10 sec ...");
            HttpResponseMessage httpResponse = await timeoutPolicy
                .ExecuteAsync(
                    async ct => await httpClient.GetAsync("http://localhost:9000/api/test/timeout/", ct),
                    userCancellationSource.Token
                    );
        }

        static async Task TestCircuitBreaker()
        {
            while (true)
            {
                Thread.Sleep(1000);

                string testUrl = "";
                switch (_circuitBreakerPolicy.CircuitState)
                {
                    case CircuitState.Closed:
                        // Circuit fermé, on envoie direct une URL d'échec
                        testUrl = "http://localhost:9000/api/test/failure/";
                        Console.WriteLine("CircuitState = Closed, try 'failure' request ...");
                        break;
                    case CircuitState.HalfOpen:
                        // Circuit semi-ouvert, on envoie une url qui fonctionne pour le refermer
                        testUrl = "http://localhost:9000/api/test/retries/";
                        Console.WriteLine("CircuitState = HalfOpen, try 'retries' request ...");
                        break;
                    case CircuitState.Isolated:
                        // Circuit isolé, on le referme et on continue
                        _circuitBreakerPolicy.Reset();
                        testUrl = "http://localhost:9000/api/test/retries/";
                        Console.WriteLine("CircuitState = Isolated, try 'retries' request ...");
                        break;
                    case CircuitState.Open:
                        // Circuit ouvert : même pas la peine d'essayer
                        Console.WriteLine("CircuitState = Open, waiting for Closed state ...");
                        Console.WriteLine("(Remaining time = " + GetCircuitBreakerRemainingTime() + " sec.)");
                        return;
                }

                Policy retryPolicy = Policy.Handle<HttpRequestException>().RetryAsync(3 , (ex, timeSpan) =>
                {
                    Console.WriteLine($"Retry Policy Exception: {ex.Message}");
                });

                await retryPolicy.ExecuteAsync(() => _circuitBreakerPolicy.ExecuteAsync(async () =>
                {
                    var httpClient = new HttpClient();
                    var response = await httpClient.GetAsync(testUrl);

                    response.EnsureSuccessStatusCode();

                    Console.WriteLine(response.StatusCode);
                }));

            }
        }
    }
}
