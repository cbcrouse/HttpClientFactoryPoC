# HttpClientFactory with HttpPolly Proof of Concept

The purpose of this POC is to demonstrate how to add resiliency to intercommunications between micro-services and also to demonstrate that the use of other tools such as RestEase or Refit can be used seamlessly in tandem with [HttpClientFactory](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/implement-resilient-applications/use-httpclientfactory-to-implement-resilient-http-requests) and [HttpPolly](https://github.com/App-vNext/Polly).

---

## Documentation References

| Title                                     |   Link     |
| :--------------------------------------   |:---------- |
| Microsoft Docs                            | <https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-2.2#use-polly-based-handlers>
| Refit Repository                          | <https://github.com/reactiveui/refit>
| Polly (Dynamic Policy Reconfiguration)    | <https://github.com/App-vNext/Polly/wiki/Dynamic-reconfiguration-during-running>
| Polly (Bulkhead)                          | <https://github.com/App-vNext/Polly/wiki/Bulkhead>
| Polly (Policy Wrap)                       | <https://github.com/App-vNext/Polly/wiki/PolicyWrap>
| Delegating Handler Order                  | <https://www.stevejgordon.co.uk/httpclientfactory-aspnetcore-outgoing-request-middleware-pipeline-delegatinghandlers>
| Simulate Response Codes                   | <https://httpstat.us/>

---

## Starting the POC

This is a simple console application that can be started up without any extra effort. When it starts, the console will open and a lot of output will flood. Look for the "warning" type messages that are written in all caps for test begin sections.

---

## Things I Learned

* It's best to just add new policies to the registry.
* Working with policy handlers is a lot like working with middleware - first in, last out.
* Certain policies are state-ful and apply to the entire client regardless of the call.
* IHttpClientFactory manages the HttpClient lifetime now and we no longer have to worry about having a single HttpClient, but you can set a custom lifetime on your client.
* IHttpClientFactory supports typed clients which both RestEase and Refit can be used to provide.
* There's a site out there to return simulated response codes
* Microsoft's HttpClientFactory is extremely flexible and works great with HttpPolly, RestEase, and Refit.
* Refit has a ton of features and you can just read about them [here](https://github.com/reactiveui/refit) - because that's not what this POC is for... ;)

---

## Things I'm Still Not Sure About

* Are MessageHandlers and PolicyHandlers treated the same when it comes to order of execution?
* How can you change the policy configuration during runtime when using the policy registry?

---

## Things You Probably Didn't Notice

* There's a period missing from a sentence above.
