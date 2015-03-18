﻿// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
// https://github.com/rasmus/EventFlow
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using EventFlow.EventStores;

namespace EventFlow.Configuration
{
    public class EventFlowOptions
    {
        private readonly ConcurrentBag<Registration> _registrations = new ConcurrentBag<Registration>();

        public EventFlowOptions UseEventStore(Func<IResolver, IEventStore> eventStoreResolver)
        {
            AddRegistration(new Registration<IEventStore>(eventStoreResolver));
            return this;
        }

        public EventFlowOptions AddRegistration(Registration registration)
        {
            _registrations.Add(registration);
            return this;
        }

        internal IEnumerable<Registration> GetRegistrations()
        {
            return _registrations;
        }

        public IResolver CreateResolver(bool validateRegistrations = false)
        {
            var container = AutofacInitialization.Configure(this);

            if (validateRegistrations)
            {
                var services = container
                    .ComponentRegistry
                    .Registrations
                    .SelectMany(x => x.Services)
                    .OfType<TypedService>()
                    .Where(x => !x.ServiceType.Name.StartsWith("Autofac"))
                    .ToList();
                var exceptions = new List<Exception>();
                foreach (var typedService in services)
                {
                    try
                    {
                        container.Resolve(typedService.ServiceType);
                    }
                    catch (DependencyResolutionException ex)
                    {
                        exceptions.Add(ex);
                    }
                }
                if (exceptions.Any())
                {
                    var message = string.Join(", ", exceptions.Select(e => e.Message));
                    throw new AggregateException(message, exceptions);
                }
            }

            return new AutofacResolver(container);
        }
    }
}
