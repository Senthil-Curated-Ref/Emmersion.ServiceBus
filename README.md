# Emmersion.ServiceBus

This library makes it simple to interact with Azure ServiceBus to do messaging between product contexts according to our internal conventions.

This has been [open sourced](https://github.com/emmersion/engineering-at-emmersion#open-source)
under the [MIT License](./LICENSE).

**Important:**
Starting at version 3.0, the library provides support to transition us from the older _single-topic_ strategy
(where all event types are funneled through a single topic)
to the new _multi-topic_ strategy (where each type of message has a separate topic).


## Configuration

Before you can use the library, you will need to call one or both of:
```csharp
Emmersion.ServiceBus.DependencyInjectionConfig.ConfigureSubscriberServices(services);
Emmersion.ServiceBus.DependencyInjectionConfig.ConfigurePublisherServices(services);
```

You will also need to provide an `ISubscriptionConfig` and/or `IPublisherConfig` respectively.

In both cases:
* `ConnectionString` - the connection string for a ServiceBus instance, not specific to a single topic.
    This connection string must have the "Manage" permission.
* `SingleTopicConnectionString` - the connection string for the specific topic used in the single-topic strategy
* `SingleTopicName` - the name of the specific topic used in the single-topic strategy

`IPublisherConfig` also provides:
* `Environment` - (optional) the name of the publishing environment (e.g. `production` or `dev-allan`); used for filtering

`ISubscriptionConfig` also provides:
* `SingleTopicSubscriptionName` - the subscription name for the specific topic used in the single-topic strategy
* `MaxConcurrentMessages` - the number of messages that can be processed concurrently
* `EnvironmentFilter` - (optional) if provided a non-null/empty value, any messages that do not have a matching environment will be filtered out


## Usage

### Message Publishing

To publish messages, use an injected `IMessagePublisher`. Here is an example of publishing in the new multi-topic way:

```csharp
var topic = new Topic("assessments", "user-assessment-scored", 1);
var body = new UserAssessmentScored
{
    UserAssessmentId = userAssessmentId,
    UserId = userId
};
var message = new Message<UserAssessmentScored>(topic, body);
await publisher.PublishAsync(message);
```

There is also an `PublishScheduledAsync` method which sends the message to ServiceBus immediately
but is not enqueued until the specified time.

To publish in the older, single-topic way:

```csharp
var event = new MessageEvent("user-assessment-scored", 1);
var message = new UserAssessmentScored
{
    UserAssessmentId = userAssessmentId,
    UserId = userId
};
publisher.PublishAsync(event, message);
```

To track metrics about all messages sent through the `IMessagePublisher`:

```csharp
publisher.OnMessagePublished += (object sender, MessagePublishedArgs args) =>
{
    // Do something smart with args.ElapsedMilliseconds, like record to Influx.
};
```


### Subscribing to Messages

To subscribe to messages, use an injected `IMessageSubscriber`. Here is an example of subscribing in the new multi-topic way:

```csharp
var topic = new Topic("assessments", "user-assessment-scored", 1);
var subscription = new Subscription(topic, "monolith", "reporting-listener");
await subscriber.SubscribeAsync(subscription, async (Message<UserAssessmentScored> message) =>
{
    await DoSomethingWith(message);
});
```

Note that you are given the entire message object, which contains additional information such as `MessageId` and `CorrelationId`.

Calling `Subscribe` will create the topic subscription in Azure automatically (if it didn't already exist).
If the name of the subscription contains the text `auto-delete` then it will delete itself after it is idle for 5 minutes.

The library also provides a way to subscribe to the dead letter queue (multi-topic only).

```csharp
await subscriber.SubscribeToDeadLettersAsync(subscription, async (DeadLetter deadLetter) => {
    await DoSomethingWith(deadLetter);
});
```

Dead letter messages always come back with a `string Body` since deserialization into a type
may have been the problem that caused it to become a dead letter in the first place.

You can also subscribe to messages in the older, single-topic way:

```csharp
var event = new MessageEvent("user-assessment-scored", 1);
await subscriber.SubscribeAsync(event, async (UserAssessmentScored message) =>
{
    await DoSomethingWith(message);
});
```

To track metrics about all messages received by the `IMessageSubscriber`:

```csharp
subscriber.OnMessageReceived += (object sender, MessageReceivedArgs args) =>
{
    // Do something with the args, such as recording the args.ProcessingTime to Influx.
};
```

To observe any exceptions encountered by the `IMessageSubscriber`:

```csharp
subscriber.OnException += (object sender, ExceptionArgs args) => 
{
    // Log the exception information.
};
```


### Connection Loss
Some initial testing shows that Microsoft's SDK handles reconnecting to ServiceBus in case of a network partition.
This is true for both publishing and subscribing.
The publishing `Task` will not complete until the message is delivered.


### Filtering for Development (Multi-topic only)
In order to make local development easier when sharing a topic
(due to the cost of separate ServiceBus instances),
you can set the `Environment` and `EnvironmentFilter` configuration variables
so that your message processor will ignore non-matching messages.

However, the non-matching messages will still be taken from the subscription queue.
Therefore, you should also use unique (per developer) subscription names
so that you don't consume others' messages on a shared subscription.


### Unit Testing
When unit testing your subscription handler, you may at times wish to set fields that are normally inaccessible.
In these cases, please use the `TestMessageBuilder<T>` class.
For example:

```csharp
var message = new TestMessageBuilder<UserAssessmentScored>()
    .WithPublishedAt(DateTimeOffset.UtcNow.AddMinutes(-5))
    .WithEnqueuedAt(DateTimeOffset.UtcNow.AddMilliseconds(-300))
    .WithReceivedAt(DateTimeOffset.UtcNow)
    .Build(new UserAssessmentScored
    {
       UserAssessmentId = Guid.NewGuid(),
       UserId = Guid.NewGuid()
    });
```


### Integration Testing
To run the integration tests, provide user secrets for the connection strings:

```
dotnet user-secrets set 'ServiceBus:ConnectionString' 'your-connection-string'

dotnet user-secrets set 'ServiceBus:SingleTopicConnectionString' 'your-connection-string'
```


## Changes & Upgrading Info

### v4.1
* Migrated dependency from `Microsoft.Azure.ServiceBus` to `Azure.Messaging.ServiceBus`
* Deprecated the older single-topic `Subscribe` methods (which took a `MessageEvent`)
  because the SDK change introduced a `.Wait()`.
  Please use `SubscribeAsync` instead.
* Deprecated a `Message` constructor in favor of the `TestMessageBuilder`
* `ExceptionArgs` now exposes new data from the updated SDK
  and has deprecated old data which is now unavailable.  

### v4.0
Changed namespace from `EL.` to `Emmersion.`

### v3.1
Added `async..await` support:
* You can now subscribe to messages with a message handler that returns a `Task`
* New async methods added:
    * `PublishAsync`
    * `PublishScheduledAsync`
    * `SubscribeAsync`
    * `SubscribeToDeadLettersAsync`
* Older methods were deprecated because they utilize a `.Wait()`
  which may not interact well with `async..await`:
    * `Publish`
    * `PublishScheduled`
    * `Subscribe`
    * `SubscribeToDeadLetters`

This version also cleaned up some mixing of synchronous and asynchronous code.

### v3.0
In version 3.0, the major new feature is using separate topics, one per message type.
Publishing and subscribing in the new way use the `Message<T>` and `Subscription` classes;
the older, single-topic method is still available via the `MessageEvent` class in order to migrate w/ dual publishing.
Configuration classes have also changed correspondingly.

The breaking changes are:
* The `ISubscriptionConfig` class has new required fields
* `ITopicConfig` has been replaced with `IPublisherConfig`
* `IMessageSubscriber`'s `OnUnhandledException` and `OnServiceBusException` have been collapsed into a single `OnException` event with new args
* Properties and constructors of `MessageReceivedArgs` have changed

Other changes:
* There is now a default implementation for `IMessageSerializer`
* You can now publish a scheduled message
* Ability to subscribe to dead letter queues
* Connections to ServiceBus are no longer initiated immediately at startup, but wait until you publish or subscribe.
* Subscriptions are automatically created if they do not exist
* Subscribing will throw an exception if the topic does not exist
* Ability to create auto-deleting subscriptions
* Ability to filter messages when subscribing

### v2.1
Added separate methods in `DependencyInjectionConfig` for configuring publishers and subscribers.

### v2.0
The major change in version 2.0 was using a `SubscriptionClient` instead of the `[ServiceBusTrigger]` annotation.
This allowed us to provide a configuration class instead of needing a specialized `appsettings.json` file.
