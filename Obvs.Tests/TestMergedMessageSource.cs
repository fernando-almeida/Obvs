﻿using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using FakeItEasy;

using Obvs.Types;

using Xunit;

namespace Obvs.Tests {

    public class TestMergedMessageSource {
        [Fact]
        public void ShouldOnlySubscribeToUnderlyingSourcesOnce() {
            IMessageSource<IEvent> source1 = A.Fake<IMessageSource<IEvent>>();
            IMessageSource<IMessage> source2 = A.Fake<IMessageSource<IMessage>>();
            IObservable<IEvent> observable1 = A.Fake<IObservable<IEvent>>();
            IObservable<IMessage> observable2 = A.Fake<IObservable<IMessage>>();
            IObserver<IMessage> observer = A.Fake<IObserver<IMessage>>();
            var scheduler = A.Fake<IScheduler>();
            
            A.CallTo(() => source1.GetMessages(A<IScheduler>._)).Returns(observable1);
            A.CallTo(() => source2.GetMessages(A<IScheduler>._)).Returns(observable2);

            MergedMessageSource<IMessage> mergedMessageSource = new MergedMessageSource<IMessage>(new [] { source1, source2 });

            var mergedMessagesObservable = mergedMessageSource.GetMessages(scheduler);
            IDisposable sub1 = mergedMessagesObservable.OfType<IEvent>().Subscribe(observer);
            IDisposable sub2 = mergedMessagesObservable.OfType<IMessage>().Subscribe(observer);
            
            A.CallTo(() => source1.GetMessages(A<IScheduler>._))
                .MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => observable1.Subscribe(A<IObserver<IMessage>>._))
                .MustHaveHappened(Repeated.Exactly.Once);

            A.CallTo(() => source2.GetMessages(A<IScheduler>._))
                .MustHaveHappened(Repeated.Exactly.Once);
            A.CallTo(() => observable2.Subscribe(A<IObserver<IMessage>>._))
                .MustHaveHappened(Repeated.Exactly.Once);
            
            sub1.Dispose();
            sub2.Dispose();
        }

        [Fact]
        public void ShouldReturnMessagesFromUnderlyingSources() {
            IMessageSource<IEvent> source1 = A.Fake<IMessageSource<IEvent>>();
            IMessageSource<IMessage> source2 = A.Fake<IMessageSource<IMessage>>();
            IObservable<IEvent> observable1 = A.Fake<IObservable<IEvent>>();
            IObservable<IMessage> observable2 = A.Fake<IObservable<IMessage>>();
            IObserver<IMessage> observer = A.Fake<IObserver<IMessage>>();
            IObserver<IEvent> internalObserver1 = null;
            IObserver<IMessage> internalObserver2 = null;
            var scheduler = A.Fake<IScheduler>();

        A.CallTo(() => source1.GetMessages(A<IScheduler>._)).Returns(observable1);
        A.CallTo(() => source2.GetMessages(A<IScheduler>._)).Returns(observable2);
        A.CallTo(() => observable1.Subscribe(A<IObserver<IEvent>>._))
            .Invokes(call => internalObserver1 = call.GetArgument<IObserver<IEvent>>(0));
        A.CallTo(() => observable2.Subscribe(A<IObserver<IMessage>>._))
            .Invokes(call => internalObserver2 = call.GetArgument<IObserver<IMessage>>(0));

        MergedMessageSource<IMessage> mergedMessageSource = new MergedMessageSource<IMessage>(new [] { source1, source2 });
        var mergedMessagesObservable = mergedMessageSource.GetMessages(scheduler);
        var sub1 = mergedMessagesObservable.OfType<IEvent>().Subscribe(observer);
        var sub2 = mergedMessagesObservable.OfType<IMessage>().Subscribe(observer);

        Assert.NotNull(internalObserver1);
        Assert.NotNull(internalObserver2);

        IEvent ev1 = A.Fake<IEvent>();
        IMessage msg2 = A.Fake<IMessage>();
        internalObserver1.OnNext(ev1);
        internalObserver2.OnNext(msg2);

        A.CallTo(() => observer.OnNext(ev1)).MustHaveHappened(Repeated.Exactly.Twice);
        A.CallTo(() => observer.OnNext(msg2)).MustHaveHappened(Repeated.Exactly.Once);

        sub1.Dispose();
        sub2.Dispose();
    }

    [Fact]
    public void ShouldDisposeUnderlyingSubscriptionOnlyWhenAllSubscriptionsDisposed() {
        IMessageSource<IEvent> source1 = A.Fake<IMessageSource<IEvent>>();
        IMessageSource<IMessage> source2 = A.Fake<IMessageSource<IMessage>>();
        IObservable<IEvent> observable1 = A.Fake<IObservable<IEvent>>();
        IObservable<IMessage> observable2 = A.Fake<IObservable<IMessage>>();
        IObserver<IMessage> observer = A.Fake<IObserver<IMessage>>();
        IObserver<IEvent> internalObserver1 = null;
        IObserver<IMessage> internalObserver2 = null;
        var scheduler = A.Fake<IScheduler>();

        A.CallTo(() => source1.GetMessages(A<IScheduler>._)).Returns(observable1);
        A.CallTo(() => source2.GetMessages(A<IScheduler>._)).Returns(observable2);
        A.CallTo(() => observable1.Subscribe(A<IObserver<IEvent>>._)).Invokes(call => internalObserver1 = call.GetArgument<IObserver<IEvent>>(0));
        A.CallTo(() => observable2.Subscribe(A<IObserver<IMessage>>._)).Invokes(call => internalObserver2 = call.GetArgument<IObserver<IMessage>>(0));

        MergedMessageSource<IMessage> mergedMessageSource = new MergedMessageSource<IMessage>(new [] { source1, source2 });

        var mergedMessagesObservable = mergedMessageSource.GetMessages(scheduler);
        IDisposable sub1 = mergedMessagesObservable.OfType<IEvent>().Subscribe(observer);
        IDisposable sub2 = mergedMessagesObservable.OfType<IMessage>().Subscribe(observer);

        Assert.NotNull(internalObserver1);
        Assert.NotNull(internalObserver2);

        IEvent ev1 = A.Fake<IEvent>();
        IMessage msg1 = A.Fake<IMessage>();
        IMessage msg2 = A.Fake<IMessage>();

        internalObserver1.OnNext(ev1);
        A.CallTo(() => observer.OnNext(ev1)).MustHaveHappened(Repeated.Exactly.Twice);

        // dispose of first subscription
        sub1.Dispose();

        // second subscription should still be active
        internalObserver2.OnNext(msg1);
        A.CallTo(() => observer.OnNext(msg1)).MustHaveHappened(Repeated.Exactly.Once);

        // dispose of second subscription
        sub2.Dispose();

        // no subscriptions should be active
        internalObserver2.OnNext(msg2);
        A.CallTo(() => observer.OnNext(msg2)).MustNotHaveHappened();
    }
}
}