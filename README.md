# TestPolly

Simple C# Client/Server test project to play with POLLY, a ".NET resilience and transient-fault-handling library that allows developers to express policies such as Retry, Circuit Breaker, Timeout, Bulkhead Isolation, and Fallback in a fluent and thread-safe manner".

The solution contains 2 projects :

- Server : WebApi Console application with timeout and failures,
- Client : WebApi client calling server using Polly to test failures handling.

Visit the official Polly page for more info :

http://www.thepollyproject.org/
