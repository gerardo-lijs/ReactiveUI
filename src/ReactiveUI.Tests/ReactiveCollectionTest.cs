﻿// Copyright (c) 2019 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Reactive.Testing;

using ReactiveUI.Legacy;
using ReactiveUI.Testing;

using Splat;

using Xunit;

#pragma warning disable CS0618 // Item has been marked as Obsolete

namespace ReactiveUI.Tests.Legacy
{
    public class FakeCollectionModel : ReactiveObject
    {
        private bool _isHidden;

        private int _someNumber;

        public bool IsHidden
        {
            get => _isHidden;
            set => this.RaiseAndSetIfChanged(ref _isHidden, value);
        }

        public int SomeNumber
        {
            get => _someNumber;
            set => this.RaiseAndSetIfChanged(ref _someNumber, value);
        }
    }

    public class FakeCollectionViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<string> _numberAsString;

        public FakeCollectionViewModel(FakeCollectionModel model)
        {
            Model = model;

            this.WhenAny(x => x.Model.SomeNumber, x => x.Value.ToString()).ToProperty(this, x => x.NumberAsString, out _numberAsString);
        }

        public FakeCollectionModel Model { get; protected set; }

        public string NumberAsString => _numberAsString.Value;
    }

    public class ReactiveCollectionTest
    {
        [Fact]
        public void ActOnEveryObjectShouldHandleAddingItems()
        {
            var testObj = new TestFixture
            {
                NullableInt = null
            };
            var fixture = new ReactiveList<TestFixture>();

            fixture.ActOnEveryObject<TestFixture, ReactiveList<TestFixture>>(addedObj => { addedObj.NullableInt = 1; }, removedObj => { removedObj.NullableInt = 0; });

            fixture.Add(testObj);

            Assert.Equal(1, testObj.NullableInt);
        }

        [Fact]
        public void ActOnEveryObjectShouldHandleAddUnderSuppressedNotifications()
        {
            var testObj = new TestFixture
            {
                NullableInt = null
            };
            var fixture = new ReactiveList<TestFixture>();

            fixture.ActOnEveryObject<TestFixture, ReactiveList<TestFixture>>(addedObj => { addedObj.NullableInt = 1; }, removedObj => { removedObj.NullableInt = 0; });

            using (fixture.SuppressChangeNotifications())
            {
                fixture.Add(testObj);
            }

            Assert.Equal(1, testObj.NullableInt);
        }

        [Fact]
        public void ActOnEveryObjectShouldHandleClear()
        {
            var testObj = new TestFixture
            {
                NullableInt = null
            };
            var fixture = new ReactiveList<TestFixture>
            {
                testObj
            };

            fixture.ActOnEveryObject<TestFixture, ReactiveList<TestFixture>>(addedObj => { addedObj.NullableInt = 1; }, removedObj => { removedObj.NullableInt = 0; });

            fixture.Clear();

            Assert.Equal(0, testObj.NullableInt);
        }

        [Fact]
        public void ActOnEveryObjectShouldHandleClearUnderSuppressedNotifications()
        {
            var testObj = new TestFixture
            {
                NullableInt = null
            };
            var fixture = new ReactiveList<TestFixture>
            {
                testObj
            };

            fixture.ActOnEveryObject<TestFixture, ReactiveList<TestFixture>>(addedObj => { addedObj.NullableInt = 1; }, removedObj => { removedObj.NullableInt = 0; });

            using (fixture.SuppressChangeNotifications())
            {
                fixture.Clear();
            }

            Assert.Equal(0, testObj.NullableInt);
        }

        // ActOnEveryObject
        [Fact]
        public void ActOnEveryObjectShouldHandlePreexistingItems()
        {
            var testObj = new TestFixture
            {
                NullableInt = null
            };
            var fixture = new ReactiveList<TestFixture>
            {
                testObj
            };

            fixture.ActOnEveryObject<TestFixture, ReactiveList<TestFixture>>(addedObj => { addedObj.NullableInt = 1; }, removedObj => { removedObj.NullableInt = 0; });

            Assert.Equal(1, testObj.NullableInt);
        }

        [Fact]
        public void ActOnEveryObjectShouldHandleRemoveUnderSuppressedNotifications()
        {
            var testObj = new TestFixture
            {
                NullableInt = null
            };
            var fixture = new ReactiveList<TestFixture>
            {
                testObj
            };

            fixture.ActOnEveryObject<TestFixture, ReactiveList<TestFixture>>(addedObj => { addedObj.NullableInt = 1; }, removedObj => { removedObj.NullableInt = 0; });

            using (fixture.SuppressChangeNotifications())
            {
                fixture.Remove(testObj);
            }

            Assert.Equal(0, testObj.NullableInt);
        }

        [Fact]
        public void ActOnEveryObjectShouldHandleRemovingItems()
        {
            var testObj = new TestFixture
            {
                NullableInt = null
            };
            var fixture = new ReactiveList<TestFixture>
            {
                testObj
            };

            fixture.ActOnEveryObject<TestFixture, ReactiveList<TestFixture>>(addedObj => { addedObj.NullableInt = 1; }, removedObj => { removedObj.NullableInt = 0; });

            fixture.Remove(testObj);

            Assert.Equal(0, testObj.NullableInt);
        }

        [Fact]
        public void AddRangeSmokeTest()
        {
            var fixture = new ReactiveList<string>();
            var output = fixture.CreateDerivedCollection(x => "Prefix" + x);

            fixture.Add("Bamf");
            Assert.Equal(1, fixture.Count);
            Assert.Equal(1, output.Count);
            Assert.Equal("Bamf", fixture[0]);
            Assert.Equal("PrefixBamf", output[0]);

            fixture.AddRange(Enumerable.Repeat("Bar", 4));
            Assert.Equal(5, fixture.Count);
            Assert.Equal(5, output.Count);
            Assert.Equal("Bamf", fixture[0]);
            Assert.Equal("PrefixBamf", output[0]);

            Assert.True(fixture.Skip(1).All(x => x == "Bar"));
            Assert.True(output.Skip(1).All(x => x == "PrefixBar"));

            // Trigger the Reset by adding a ton of items
            fixture.AddRange(Enumerable.Repeat("Bar", 35));
            Assert.Equal(40, fixture.Count);
            Assert.Equal(40, output.Count);
            Assert.Equal("Bamf", fixture[0]);
            Assert.Equal("PrefixBamf", output[0]);
        }

        [Fact]
        public void ChangeTrackingItemsShouldBeTrackedEvenWhenSuppressed()
        {
            var input = new TestFixture();
            var fixture = new ReactiveList<TestFixture>
            {
                ChangeTrackingEnabled = true
            };

            var changes = fixture.ItemChanged.CreateCollection();
            Assert.Equal(0, changes.Count);

            input.IsOnlyOneWord = "foo";
            Assert.Equal(0, changes.Count);

            using (fixture.SuppressChangeNotifications())
            {
                fixture.Add(input);

                input.IsOnlyOneWord = "bar";
                Assert.Equal(0, changes.Count);
            }

            // Even though we added it during a suppression, we should still
            // get notifications now that the suppression is over
            input.IsOnlyOneWord = "baz";
            Assert.Equal(1, changes.Count);

            fixture.RemoveAt(0);
            input.IsOnlyOneWord = "bamf";
            Assert.Equal(1, changes.Count);
        }

        [Fact]
        public void ChangeTrackingShouldApplyOnAddRangedItems()
        {
            var fixture = new ReactiveList<TestFixture>
            {
                new TestFixture()
            };
            fixture.ChangeTrackingEnabled = true;

            var reset = fixture.ShouldReset.CreateCollection();
            var itemChanged = fixture.ItemChanged.CreateCollection();
            Assert.Equal(0, reset.Count);

            fixture[0].IsNotNullString = "Foo";
            Assert.Equal(0, reset.Count);
            Assert.Equal(1, itemChanged.Count);

            fixture.AddRange(
                             Enumerable.Range(0, 15).Select(
                                                            x => new TestFixture
                                                            {
                                                                IsOnlyOneWord = x.ToString()
                                                            }));
            Assert.Equal(1, reset.Count);
            Assert.Equal(1, itemChanged.Count);

            fixture[0].IsNotNullString = "Bar";
            Assert.Equal(1, reset.Count);
            Assert.Equal(2, itemChanged.Count);

            fixture[5].IsNotNullString = "Baz";
            Assert.Equal(1, reset.Count);
            Assert.Equal(3, itemChanged.Count);
        }

        [Fact]
        public void ChangeTrackingShouldFireNotifications()
        {
            var fixture = new ReactiveList<TestFixture>
            {
                ChangeTrackingEnabled = true
            };
            var before_output = new List<Tuple<TestFixture, string>>();
            var output = new List<Tuple<TestFixture, string>>();
            var item1 = new TestFixture
            {
                IsOnlyOneWord = "Foo"
            };
            var item2 = new TestFixture
            {
                IsOnlyOneWord = "Bar"
            };

            fixture.ItemChanging.Subscribe(x => { before_output.Add(new Tuple<TestFixture, string>(x.Sender, x.PropertyName)); });

            fixture.ItemChanged.Subscribe(x => { output.Add(new Tuple<TestFixture, string>(x.Sender, x.PropertyName)); });

            fixture.Add(item1);
            fixture.Add(item2);

            item1.IsOnlyOneWord = "Baz";
            Assert.Equal(1, output.Count);
            item2.IsNotNullString = "FooBar";
            Assert.Equal(2, output.Count);

            fixture.Remove(item2);
            item2.IsNotNullString = "FooBarBaz";
            Assert.Equal(2, output.Count);

            fixture.ChangeTrackingEnabled = false;
            item1.IsNotNullString = "Bamf";
            Assert.Equal(2, output.Count);

            new[] { item1, item2 }.AssertAreEqual(output.Select(x => x.Item1));
            new[] { item1, item2 }.AssertAreEqual(before_output.Select(x => x.Item1));
            new[] { "IsOnlyOneWord", "IsNotNullString" }.AssertAreEqual(output.Select(x => x.Item2));
        }

        [Fact]
        public void ChangeTrackingShouldStopWhenAnObjectIsReplacedAndChangeNotificationIsSuppressed()
        {
            var fixture = new ReactiveList<TestFixture>
            {
                ChangeTrackingEnabled = true
            };

            var before_output = new List<Tuple<TestFixture, string>>();
            var output = new List<Tuple<TestFixture, string>>();
            var item1 = new TestFixture
            {
                IsOnlyOneWord = "Foo"
            };
            var item2 = new TestFixture
            {
                IsOnlyOneWord = "Bar"
            };

            fixture.ItemChanging.Subscribe(x => { before_output.Add(new Tuple<TestFixture, string>(x.Sender, x.PropertyName)); });

            fixture.ItemChanged.Subscribe(x => { output.Add(new Tuple<TestFixture, string>(x.Sender, x.PropertyName)); });

            fixture.Add(item1);

            item1.IsOnlyOneWord = "Baz";
            Assert.Equal(1, output.Count);
            item2.IsNotNullString = "FooBar";
            Assert.Equal(1, output.Count);

            using (IReactiveObjectExtensions.SuppressChangeNotifications(fixture))
            {
                fixture[0] = item2;
            }

            item1.IsOnlyOneWord = "FooAgain";
            Assert.Equal(1, output.Count);
            item2.IsNotNullString = "FooBarBaz";
            Assert.Equal(2, output.Count);

            new[] { item1, item2 }.AssertAreEqual(output.Select(x => x.Item1));
            new[] { item1, item2 }.AssertAreEqual(before_output.Select(x => x.Item1));
            new[] { "IsOnlyOneWord", "IsNotNullString" }.AssertAreEqual(output.Select(x => x.Item2));
        }

        [Fact]
        public void ChangeTrackingShouldWorkWhenAddingTheSameThingMoreThanOnce()
        {
            var fixture = new ReactiveList<TestFixture>
            {
                ChangeTrackingEnabled = true
            };
            var output = new List<Tuple<TestFixture, string>>();
            var item1 = new TestFixture
            {
                IsOnlyOneWord = "Foo"
            };

            fixture.ItemChanged.Subscribe(x => { output.Add(new Tuple<TestFixture, string>(x.Sender, x.PropertyName)); });

            fixture.Add(item1);
            fixture.Add(item1);
            fixture.Add(item1);

            item1.IsOnlyOneWord = "Bar";
            Assert.Equal(1, output.Count);

            fixture.RemoveAt(0);

            item1.IsOnlyOneWord = "Baz";
            Assert.Equal(2, output.Count);

            fixture.RemoveAt(0);
            fixture.RemoveAt(0);

            // We've completely removed item1, we shouldn't be seeing any
            // notifications from it
            item1.IsOnlyOneWord = "Bamf";
            Assert.Equal(2, output.Count);

            fixture.ChangeTrackingEnabled = false;
            fixture.Add(item1);
            fixture.Add(item1);
            fixture.Add(item1);
            fixture.ChangeTrackingEnabled = true;

            item1.IsOnlyOneWord = "Bonk";
            Assert.Equal(3, output.Count);
        }

        [Fact]
        public void CollectionCountChangedFiresWhenClearing()
        {
            var items = new ReactiveList<object>(new[] { new object() });
            var countChanged = false;
            items.CountChanged.Subscribe(_ => { countChanged = true; });

            items.Clear();

            Assert.True(countChanged);
        }

        [Fact]
        public void CollectionCountChangedTest()
        {
            var fixture = new ReactiveList<int>();
            var before_output = new List<int>();
            var output = new List<int>();

            fixture.CountChanging.Subscribe(before_output.Add);
            fixture.CountChanged.Subscribe(output.Add);

            fixture.Add(10);
            fixture.Add(20);
            fixture.Add(30);
            fixture.RemoveAt(1);
            fixture.Clear();

            var before_results = new[] { 0, 1, 2, 3, 2 };
            Assert.Equal(before_results.Length, before_output.Count);
            before_results.AssertAreEqual(before_output);

            var results = new[] { 1, 2, 3, 2, 0 };
            Assert.Equal(results.Length, output.Count);
            results.AssertAreEqual(output);
        }

        [Fact]
        public void CollectionsShouldntShareSubscriptions()
        {
            var fixture1 = new ReactiveList<TestFixture>
            {
                ChangeTrackingEnabled = true
            };
            var fixture2 = new ReactiveList<TestFixture>
            {
                ChangeTrackingEnabled = true
            };
            var item1 = new TestFixture
            {
                IsOnlyOneWord = "Foo"
            };
            var output1 = new List<Tuple<TestFixture, string>>();
            var output2 = new List<Tuple<TestFixture, string>>();

            fixture1.ItemChanged.Subscribe(x => { output1.Add(new Tuple<TestFixture, string>(x.Sender, x.PropertyName)); });

            fixture2.ItemChanged.Subscribe(x => { output2.Add(new Tuple<TestFixture, string>(x.Sender, x.PropertyName)); });

            fixture1.Add(item1);
            fixture1.Add(item1);
            fixture2.Add(item1);
            fixture2.Add(item1);

            item1.IsOnlyOneWord = "Bar";
            Assert.Equal(1, output1.Count);
            Assert.Equal(1, output2.Count);

            fixture2.RemoveAt(0);

            item1.IsOnlyOneWord = "Baz";
            Assert.Equal(2, output1.Count);
            Assert.Equal(2, output2.Count);
        }

        [Fact]
        public void CountPropertyIsNotAmbiguous()
        {
            IReactiveList<int> reactiveList = new ReactiveList<int>();
            Assert.Equal(0, reactiveList.Count);
            IList<int> list = reactiveList;
            Assert.Equal(0, list.Count);

            ICollection collection = new ReactiveList<int>();
            var l = (IList)collection;
            Assert.Same(collection, l);
        }

        [Fact]
        public void CreateCollectionWithoutTimer()
        {
            var input = new[] { "Foo", "Bar", "Baz", "Bamf" };
            var fixture = new TestScheduler().With(
                                                   sched =>
                                                   {
                                                       var f = input.ToObservable(sched).CreateCollection();

                                                       sched.Start();
                                                       return f;
                                                   });

            input.AssertAreEqual(fixture);
        }

        [Fact]
        public void CreateCollectionWithTimer()
        {
            var input = new[] { "Foo", "Bar", "Baz", "Bamf" };
            var sched = new TestScheduler();

            using (TestUtils.WithScheduler(sched))
            {
                IReactiveDerivedList<string> fixture = input.ToObservable(sched).CreateCollection(TimeSpan.FromSeconds(0.5));
                sched.AdvanceToMs(1005);
                fixture.AssertAreEqual(input.Take(2));

                sched.AdvanceToMs(1505);
                fixture.AssertAreEqual(input.Take(3));

                sched.AdvanceToMs(10000);
                fixture.AssertAreEqual(input);
            }
        }

        [WpfFact]
        public void DataboundReactiveListDoesNotThrowForAddRange()
        {
            var vm = new PropertyBindViewModel();
            var view = new PropertyBindView
            {
                ViewModel = vm
            };
            var fixture = new PropertyBinderImplementation();
            fixture.OneWayBind(vm, view, m => m.SomeCollectionOfStrings, v => v.FakeItemsControl.ItemsSource);

            // eliminate the ResetChangeThreshold from the equation
            vm.SomeCollectionOfStrings.ResetChangeThreshold = int.MinValue;

            // Within the reset threshold
            vm.SomeCollectionOfStrings.AddRange(Create(5));
            vm.SomeCollectionOfStrings.AddRange(Create(20));

            IEnumerable<string> Create(int numElements) => Enumerable.Range(1, numElements).Select(i => $"item_{i}");
        }

        [WpfFact]
        public void DataboundReactiveListDoesNotThrowForInsertRange()
        {
            var vm = new PropertyBindViewModel();
            var view = new PropertyBindView
            {
                ViewModel = vm
            };
            var fixture = new PropertyBinderImplementation();
            fixture.OneWayBind(vm, view, m => m.SomeCollectionOfStrings, v => v.FakeItemsControl.ItemsSource);
            vm.SomeCollectionOfStrings.ResetChangeThreshold = int.MinValue;

            foreach (var item in Create(5))
            {
                vm.SomeCollectionOfStrings.Add(item);
            }

            // within reset threshold
            vm.SomeCollectionOfStrings.InsertRange(2, Create(5));

            // outside reset threshold
            vm.SomeCollectionOfStrings.InsertRange(2, Create(20));

            IEnumerable<string> Create(int numElements) => Enumerable.Range(1, numElements).Select(i => $"item_{i}");
        }

        [WpfFact]
        public void DataboundReactiveListDoesNotThrowForRemoveRange()
        {
            var vm = new PropertyBindViewModel();
            var view = new PropertyBindView
            {
                ViewModel = vm
            };
            var fixture = new PropertyBinderImplementation();
            fixture.OneWayBind(vm, view, m => m.SomeCollectionOfStrings, v => v.FakeItemsControl.ItemsSource);
            vm.SomeCollectionOfStrings.ResetChangeThreshold = int.MinValue;

            foreach (var item in Enumerable.Range(1, 40).Select(i => $"item_{i}"))
            {
                vm.SomeCollectionOfStrings.Add(item);
            }

            // within reset threshold
            vm.SomeCollectionOfStrings.RemoveRange(2, 5);

            // outside reset threshold
            vm.SomeCollectionOfStrings.RemoveRange(2, 20);
        }

        [Fact]
        public void DerivedCollectionFilterTest()
        {
            var models = new ReactiveList<FakeCollectionModel>(
                                                               new[] { 0, 1, 2, 3, 4, }.Select(
                                                                                               x => new FakeCollectionModel
                                                                                               {
                                                                                                   SomeNumber = x
                                                                                               }))
            {
                ChangeTrackingEnabled = true
            };

            var viewModels = models.CreateDerivedCollection(x => new FakeCollectionViewModel(x), x => !x.IsHidden);
            Assert.Equal(5, viewModels.Count);

            models[0].IsHidden = true;
            Assert.Equal(4, viewModels.Count);

            models[4].IsHidden = true;
            Assert.Equal(3, viewModels.Count);

            models[0].IsHidden = false;
            Assert.Equal(4, viewModels.Count);
        }

        [Fact]
        public void DerivedCollectionMoveNotificationSmokeTest()
        {
            var initial = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var source = new ReactiveList<int>(initial);

            var derived = source.CreateDerivedCollection(x => x);
            var nestedDerived = derived.CreateDerivedCollection(x => x);
            var derivedSorted = source.CreateDerivedCollection(x => x, orderer: (x, y) => x.CompareTo(y));

            for (var i = 0; i < initial.Length; i++)
            {
                for (var j = 0; j < initial.Length; j++)
                {
                    source.Move(i, j);

                    Assert.True(derived.SequenceEqual(source));
                    Assert.True(nestedDerived.SequenceEqual(source));
                    Assert.True(derivedSorted.SequenceEqual(initial));
                }
            }
        }

        /// <summary>
        /// This test is a bit contrived and only exists to verify that a particularly gnarly bug doesn't get
        /// reintroduced because it's hard to reason about the removal logic in derived collections and it might
        /// be tempting to try and reorder the shiftIndices operation in there.
        /// </summary>
        [Fact]
        public void DerivedCollectionRemovalRegressionTest()
        {
            var input = new[] { 'A', 'B', 'C', 'D' };
            var source = new ReactiveList<char>(input);

            // A derived collection that filters away 'A' and 'B'
            var derived = source.CreateDerivedCollection(x => x, x => x >= 'C');

            var changeNotifications = new List<NotifyCollectionChangedEventArgs>();
            derived.Changed.Subscribe(changeNotifications.Add);

            Assert.Equal(0, changeNotifications.Count);
            Assert.Equal(2, derived.Count);
            Assert.True(derived.SequenceEqual(new[] { 'C', 'D' }));

            // The tricky part here is that 'B' isn't in the derived collection, only 'C' is and this test
            // will detect if the dervied collection gets tripped up and removes 'C' instead
            source.RemoveAll(new[] { 'B', 'C' });

            Assert.Equal(1, changeNotifications.Count);
            Assert.Equal(1, derived.Count);
            Assert.True(derived.SequenceEqual(new[] { 'D' }));
        }

        [Fact]
        public void DerivedCollectionShouldBeSorted()
        {
            var input = new[] { "Foo", "Bar", "Baz" };
            var fixture = new ReactiveList<string>(input);

            var output = fixture.CreateDerivedCollection(x => x, orderer: string.CompareOrdinal);

            Assert.Equal(3, output.Count);
            Assert.True(new[] { "Bar", "Baz", "Foo" }.Zip(output, (expected, actual) => expected == actual).All(x => x));

            fixture.Add("Bamf");
            Assert.Equal(4, output.Count);
            Assert.True(new[] { "Bamf", "Bar", "Baz", "Foo" }.Zip(output, (expected, actual) => expected == actual).All(x => x));

            fixture.Add("Eoo");
            Assert.Equal(5, output.Count);
            Assert.True(new[] { "Bamf", "Bar", "Baz", "Eoo", "Foo" }.Zip(output, (expected, actual) => expected == actual).All(x => x));

            fixture.Add("Roo");
            Assert.Equal(6, output.Count);
            Assert.True(new[] { "Bamf", "Bar", "Baz", "Eoo", "Foo", "Roo" }.Zip(output, (expected, actual) => expected == actual).All(x => x));

            fixture.Add("Bar");
            Assert.Equal(7, output.Count);
            Assert.True(new[] { "Bamf", "Bar", "Bar", "Baz", "Eoo", "Foo", "Roo" }.Zip(output, (expected, actual) => expected == actual).All(x => x));
        }

        [Fact]
        public void DerivedCollectionShouldHandleMovesWhenOnlyContainingOneItem()
        {
            // This test is here to verify a bug in where newPositionForItem would return an incorrect
            // index for lists only containing a single item (the item to find a new position for)
            var sanity = new List<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };
            var source = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };

            var derived = source.CreateDerivedCollection(x => x, x => x == "d");

            Assert.Equal("d", derived.Single());
            Assert.Equal("d", source[3]);

            source.Move(3, 0);

            Assert.Equal("d", source[0]);
            Assert.Equal("d", derived.Single());
        }

        [Fact]
        public void DerivedCollectionShouldNotSignalRedundantMoveSignals()
        {
            var sanity = new List<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };
            var source = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };

            var derived = source.CreateDerivedCollection(x => x, x => x == "d" || x == "e");

            var derivedNotification = new List<NotifyCollectionChangedEventArgs>();
            derived.Changed.Subscribe(derivedNotification.Add);

            Assert.Equal("d", source[3]);
            source.Move(3, 0);

            Assert.Equal(0, derivedNotification.Count);
        }

        [Fact]
        public void DerivedCollectionShouldOrderCorrectly()
        {
            var collection = new ReactiveList<int>();
            var orderedCollection = collection.CreateDerivedCollection(x => x, null, (x, y) => x.CompareTo(y));

            collection.Add(1);
            collection.Add(2);

            Assert.Equal(2, orderedCollection.Count);
            Assert.Equal(1, orderedCollection[0]);
            Assert.Equal(2, orderedCollection[1]);
        }

        [Fact]
        public void DerivedCollectionShouldStopFollowingAfterDisposal()
        {
            var collection = new ReactiveList<int>();

            var orderedCollection = collection.CreateDerivedCollection(x => x.ToString(), null, (x, y) => x.CompareTo(y));

            collection.Add(1);
            collection.Add(2);

            Assert.Equal(2, orderedCollection.Count);

            orderedCollection.Dispose();

            collection.Add(3);
            Assert.Equal(2, orderedCollection.Count);
        }

        [Fact]
        public void DerivedCollectionShouldUnderstandDummyMoveSignal()
        {
            var sanity = new List<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };
            var source = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };

            var derived = source.CreateDerivedCollection(x => x);

            var sourceNotifications = new List<NotifyCollectionChangedEventArgs>();

            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(x => source.CollectionChanged += x, x => source.CollectionChanged -= x).Subscribe(x => sourceNotifications.Add(x.EventArgs));

            var derivedNotification = new List<NotifyCollectionChangedEventArgs>();
            derived.Changed.Subscribe(derivedNotification.Add);

            source.Move(0, 0);

            Assert.Equal(1, sourceNotifications.Count);
            Assert.Equal(NotifyCollectionChangedAction.Move, sourceNotifications.First().Action);

            Assert.Equal(0, derivedNotification.Count);
        }

        [Fact]
        public void DerivedCollectionShouldUnderstandMoveEvenWhenSorted()
        {
            var sanity = new List<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };
            var source = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };

            var derived = source.CreateDerivedCollection(selector: x => x, filter: x => x != "c", orderer: (x, y) => x.CompareTo(y));

            var sourceNotifications = new List<NotifyCollectionChangedEventArgs>();

            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(x => source.CollectionChanged += x, x => source.CollectionChanged -= x).Subscribe(x => sourceNotifications.Add(x.EventArgs));

            var derivedNotifications = new List<NotifyCollectionChangedEventArgs>();
            derived.Changed.Subscribe(derivedNotifications.Add);

            Assert.Equal(5, derived.Count);
            Assert.True(derived.SequenceEqual(new[] { "a", "b" /*, "c" */, "d", "e", "f" }));

            var rnd = new Random();

            for (var i = 0; i < 50; i++)
            {
                var from = rnd.Next(0, source.Count);
                int to;

                do
                {
                    to = rnd.Next(0, source.Count);
                }
                while (to == from);

                source.Move(from, to);

                var tmp = sanity[from];
                sanity.RemoveAt(from);
                sanity.Insert(to, tmp);

                Assert.True(source.SequenceEqual(sanity));
                Assert.True(derived.SequenceEqual(new[] { "a", "b" /*, "c" */, "d", "e", "f" }));

                Assert.Equal(1, sourceNotifications.Count);
                Assert.Equal(NotifyCollectionChangedAction.Move, sourceNotifications.First().Action);

                Assert.Empty(derivedNotifications);

                sourceNotifications.Clear();
            }
        }

        [Fact]
        public void DerivedCollectionShouldUnderstandMoveSignals()
        {
            var source = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };
            var derived = source.CreateDerivedCollection(x => x);

            var sourceNotifications = new List<NotifyCollectionChangedEventArgs>();

            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(x => source.CollectionChanged += x, x => source.CollectionChanged -= x).Subscribe(x => sourceNotifications.Add(x.EventArgs));

            var derivedNotifications = new List<NotifyCollectionChangedEventArgs>();
            derived.Changed.Subscribe(derivedNotifications.Add);

            Assert.Equal(6, derived.Count);
            Assert.True(source.SequenceEqual(derived));
            Assert.Empty(derivedNotifications);

            source.Move(1, 4);

            Assert.True(source.SequenceEqual(new[] { "a", "c", "d", "e", "b", "f" }));
            Assert.True(derived.SequenceEqual(source));

            Assert.Equal(1, sourceNotifications.Count);
            Assert.Equal(NotifyCollectionChangedAction.Move, sourceNotifications.First().Action);

            Assert.Equal(1, derivedNotifications.Count);
            Assert.Equal(NotifyCollectionChangedAction.Move, derivedNotifications.First().Action);

            sourceNotifications.Clear();
            derivedNotifications.Clear();

            source.Move(4, 1);

            Assert.True(source.SequenceEqual(new[] { "a", "b", "c", "d", "e", "f" }));
            Assert.True(derived.SequenceEqual(source));

            Assert.Equal(1, sourceNotifications.Count);
            Assert.Equal(NotifyCollectionChangedAction.Move, sourceNotifications.First().Action);

            Assert.Equal(1, derivedNotifications.Count);
            Assert.Equal(NotifyCollectionChangedAction.Move, derivedNotifications.First().Action);

            source.Move(0, 5);

            Assert.True(source.SequenceEqual(new[] { "b", "c", "d", "e", "f", "a" }));
            Assert.True(derived.SequenceEqual(source));

            source.Move(5, 0);

            Assert.True(source.SequenceEqual(new[] { "a", "b", "c", "d", "e", "f", }));
            Assert.True(derived.SequenceEqual(source));
        }

        [Fact]
        public void DerivedCollectionShouldUnderstandNestedMoveSignals()
        {
            var source = new System.Collections.ObjectModel.ObservableCollection<string>
            {
                "a",
                "b",
                "c",
                "d",
                "e",
                "f"
            };
            var derived = source.CreateDerivedCollection(x => x);
            var nested = derived.CreateDerivedCollection(x => x);

            var reverseNested = nested.CreateDerivedCollection(x => x, orderer: OrderedComparer<string>.OrderByDescending(x => x).Compare);

            var sortedNested = reverseNested.CreateDerivedCollection(x => x, orderer: OrderedComparer<string>.OrderBy(x => x).Compare);

            source.Move(1, 4);

            Assert.True(source.SequenceEqual(derived));
            Assert.True(source.SequenceEqual(nested));
            Assert.True(source.OrderByDescending(x => x).SequenceEqual(reverseNested));
            Assert.True(source.OrderBy(x => x).SequenceEqual(sortedNested));
        }

        [Fact]
        public void DerivedCollectionSignalledToResetShouldFireExactlyOnce()
        {
            var input = new List<string>
            {
                "Foo"
            };
            var resetSubject = new Subject<Unit>();
            var derived = input.CreateDerivedCollection(x => x, signalReset: resetSubject);

            var changeNotifications = new List<NotifyCollectionChangedEventArgs>();
            derived.Changed.Subscribe(changeNotifications.Add);

            Assert.Equal(0, changeNotifications.Count);
            Assert.Equal(1, derived.Count);

            input.Add("Bar");

            // Shouldn't have picked anything up since the input isn't reactive
            Assert.Equal(0, changeNotifications.Count);
            Assert.Equal(1, derived.Count);

            resetSubject.OnNext(Unit.Default);

            Assert.Equal(1, changeNotifications.Count);
            Assert.Equal(2, derived.Count);
        }

        [Fact]
        public void DerivedCollectionsShouldBeFiltered()
        {
            var input = new[] { "Foo", "Bar", "Baz", "Bamf" };
            var fixture = new ReactiveList<TestFixture>(
                                                        input.Select(
                                                                     x => new TestFixture
                                                                     {
                                                                         IsOnlyOneWord = x
                                                                     }));
            var itemsAdded = new List<TestFixture>();
            var itemsRemoved = new List<TestFixture>();

            var output = fixture.CreateDerivedCollection(x => x, x => x.IsOnlyOneWord[0] == 'F', (l, r) => l.IsOnlyOneWord.CompareTo(r.IsOnlyOneWord));
            output.ItemsAdded.Subscribe(itemsAdded.Add);
            output.ItemsRemoved.Subscribe(itemsRemoved.Add);

            Assert.Equal(1, output.Count);
            Assert.Equal(0, itemsAdded.Count);
            Assert.Equal(0, itemsRemoved.Count);

            fixture.Add(
                        new TestFixture
                        {
                            IsOnlyOneWord = "Boof"
                        });
            Assert.Equal(1, output.Count);
            Assert.Equal(0, itemsAdded.Count);
            Assert.Equal(0, itemsRemoved.Count);

            fixture.Add(
                        new TestFixture
                        {
                            IsOnlyOneWord = "Far"
                        });
            Assert.Equal(2, output.Count);
            Assert.Equal(1, itemsAdded.Count);
            Assert.Equal(0, itemsRemoved.Count);

            fixture.RemoveAt(1); // Remove "Bar"
            Assert.Equal(2, output.Count);
            Assert.Equal(1, itemsAdded.Count);
            Assert.Equal(0, itemsRemoved.Count);

            fixture.RemoveAt(0); // Remove "Foo"
            Assert.Equal(1, output.Count);
            Assert.Equal(1, itemsAdded.Count);
            Assert.Equal(1, itemsRemoved.Count);
        }

        [Fact]
        public void DerivedCollectionsShouldFollowBaseCollection()
        {
            var input = new[] { "Foo", "Bar", "Baz", "Bamf" };
            var fixture = new ReactiveList<TestFixture>(
                                                        input.Select(
                                                                     x => new TestFixture
                                                                     {
                                                                         IsOnlyOneWord = x
                                                                     }));

            var output = fixture.CreateDerivedCollection(x => x.IsOnlyOneWord);

            input.AssertAreEqual(output);

            fixture.Add(
                        new TestFixture
                        {
                            IsOnlyOneWord = "Hello"
                        });
            Assert.Equal(5, output.Count);
            Assert.Equal("Hello", output[4]);

            fixture.RemoveAt(4);
            Assert.Equal(4, output.Count);

            fixture[1] = new TestFixture
            {
                IsOnlyOneWord = "Goodbye"
            };
            Assert.Equal(4, output.Count);
            Assert.Equal("Goodbye", output[1]);

            fixture.Clear();
            Assert.Equal(0, output.Count);
        }

        [Fact]
        public void DerviedCollectionShouldHandleItemsRemoved()
        {
            var input = new[] { "Foo", "Bar", "Baz", "Bamf" };
            var disposed = new List<TestFixture>();
            var fixture = new ReactiveList<TestFixture>(
                                                        input.Select(
                                                                     x => new TestFixture
                                                                     {
                                                                         IsOnlyOneWord = x
                                                                     }));

            var output = fixture.CreateDerivedCollection(x => Disposable.Create(() => disposed.Add(x)), item => item.Dispose());

            fixture.Add(
                        new TestFixture
                        {
                            IsOnlyOneWord = "Hello"
                        });
            Assert.Equal(5, output.Count);

            fixture.RemoveAt(3);
            Assert.Equal(4, output.Count);
            Assert.Equal(1, disposed.Count);
            Assert.Equal("Bamf", disposed[0].IsOnlyOneWord);

            fixture[1] = new TestFixture
            {
                IsOnlyOneWord = "Goodbye"
            };
            Assert.Equal(4, output.Count);
            Assert.Equal(2, disposed.Count);
            Assert.Equal("Bar", disposed[1].IsOnlyOneWord);

            var count = output.Count;
            output.Dispose();
            Assert.Equal(disposed.Count, 2 + count);
        }

        [Fact]
        public void GetARangeWhenWeAddAListOfItems()
        {
            var fixture = new ReactiveList<int>
            {
                1,
                2,
                3,
                4,
                5
            };
            var changed = fixture.Changed.CreateCollection();
            Assert.Equal(0, changed.Count);

            fixture.AddRange(new[] { 6, 7 });
            Assert.Equal(1, changed.Count);
            Assert.Equal(NotifyCollectionChangedAction.Add, changed[0].Action);
        }

        [Fact]
        public void GetAResetWhenWeAddALotOfItems()
        {
            var fixture = new ReactiveList<int>
            {
                1,
            };
            var reset = fixture.ShouldReset.CreateCollection();
            Assert.Equal(0, reset.Count);

            fixture.AddRange(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, });
            Assert.Equal(1, reset.Count);
        }

        [Fact]
        public void GetSingleItemNotificationWhenWeAddAListOfItemsAndRangeIsFalse()
        {
            try
            {
                RxApp.SupportsRangeNotifications = false;

                var fixture = new ReactiveList<int>
                {
                    1,
                    2,
                    3,
                    4,
                    5
                };
                var changed = fixture.Changed.CreateCollection();
                Assert.Equal(0, changed.Count);

                fixture.AddRange(new[] { 6, 7 });
                Assert.Equal(2, changed.Count);
                Assert.Equal(NotifyCollectionChangedAction.Add, changed[0].Action);
                Assert.Equal(NotifyCollectionChangedAction.Add, changed[1].Action);
            }
            finally
            {
                RxApp.SupportsRangeNotifications = true;
            }
        }

        [Fact]
        public void IListSmokeTest()
        {
            var fixture = new ReactiveList<string>() as IList;
            Assert.NotNull(fixture);

            var pos = fixture.Add("foo");
            Assert.Equal(0, pos);
            Assert.Equal(1, fixture.Count);
            Assert.True(fixture.Contains("foo"));

            fixture.Insert(0, "bar");
            Assert.Equal(0, fixture.IndexOf("bar"));
            Assert.Equal(1, fixture.IndexOf("foo"));
            Assert.Equal("bar", fixture[0] as string);
            Assert.Equal("foo", fixture[1] as string);

            var arr = new string[2];
            fixture.CopyTo(arr, 0);
            Assert.Equal(2, arr.Length);
            Assert.Equal("bar", arr[0]);
            Assert.Equal("foo", arr[1]);

            fixture[1] = "baz";
            Assert.Equal(1, fixture.IndexOf("baz"));
            Assert.Equal(-1, fixture.IndexOf("foo"));
            Assert.Equal("baz", fixture[1] as string);
            Assert.False(fixture.Contains("foo"));
            Assert.True(fixture.Contains("baz"));

            fixture.Remove("bar");
            Assert.Equal(1, fixture.Count);
            Assert.False(fixture.Contains("bar"));
        }

        [Fact]
        public void IListTSmokeTest()
        {
            var fixture = new ReactiveList<string>() as IList<string>;
            Assert.NotNull(fixture);

            fixture.Add("foo");
            Assert.Equal(1, fixture.Count);
            Assert.True(fixture.Contains("foo"));

            fixture.Insert(0, "bar");
            Assert.Equal(0, fixture.IndexOf("bar"));
            Assert.Equal(1, fixture.IndexOf("foo"));
            Assert.Equal("bar", fixture[0]);
            Assert.Equal("foo", fixture[1]);

            var genericEnum = fixture.GetEnumerator();
            Assert.NotNull(genericEnum);
            var result = genericEnum.MoveNext();
            Assert.True(result);
            Assert.Equal("bar", genericEnum.Current);
            result = genericEnum.MoveNext();
            Assert.True(result);
            Assert.Equal("foo", genericEnum.Current);
            result = genericEnum.MoveNext();
            Assert.False(result);

            var plainEnum = ((IEnumerable)fixture).GetEnumerator();
            Assert.NotNull(plainEnum);
            result = plainEnum.MoveNext();
            Assert.True(result);
            Assert.Equal("bar", plainEnum.Current as string);
            result = plainEnum.MoveNext();
            Assert.True(result);
            Assert.Equal("foo", plainEnum.Current as string);
            result = plainEnum.MoveNext();
            Assert.False(result);

            var arr = new string[2];
            fixture.CopyTo(arr, 0);
            Assert.Equal(2, arr.Length);
            Assert.Equal("bar", arr[0]);
            Assert.Equal("foo", arr[1]);

            fixture[1] = "baz";
            Assert.Equal(1, fixture.IndexOf("baz"));
            Assert.Equal(-1, fixture.IndexOf("foo"));
            Assert.Equal("baz", fixture[1]);
            Assert.False(fixture.Contains("foo"));
            Assert.True(fixture.Contains("baz"));

            fixture.RemoveAt(1);
            Assert.False(fixture.Contains("baz"));

            fixture.Remove("bar");
            Assert.Equal(0, fixture.Count);
            Assert.False(fixture.Contains("bar"));
        }

        [Fact]
        public void IndexerIsNotAmbiguous()
        {
            IReactiveList<int> reactiveList = new ReactiveList<int>
            {
                0,
                1
            };
            Assert.Equal(0, reactiveList[0]);
        }

        [Fact]
        public void InsertRangeSmokeTest()
        {
            var fixture = new ReactiveList<string>();
            var output = fixture.CreateDerivedCollection(x => "Prefix" + x);

            fixture.Add("Bamf");
            Assert.Equal(1, fixture.Count);
            Assert.Equal(1, output.Count);
            Assert.Equal("Bamf", fixture[0]);
            Assert.Equal("PrefixBamf", output[0]);

            fixture.InsertRange(0, Enumerable.Repeat("Bar", 4));
            Assert.Equal(5, fixture.Count);
            Assert.Equal(5, output.Count);
            Assert.Equal("Bamf", fixture[4]);
            Assert.Equal("PrefixBamf", output[4]);

            Assert.True(fixture.Take(4).All(x => x == "Bar"));
            Assert.True(output.Take(4).All(x => x == "PrefixBar"));

            // Trigger the Reset by adding a ton of items
            fixture.InsertRange(0, Enumerable.Repeat("Bar", 35));
            Assert.Equal(40, fixture.Count);
            Assert.Equal(40, output.Count);
            Assert.Equal("Bamf", fixture[39]);
            Assert.Equal("PrefixBamf", output[39]);
        }

        [Fact]
        public void ItemsAddedAndRemovedTest()
        {
            var fixture = new ReactiveList<int>();
            var before_added = new List<int>();
            var before_removed = new List<int>();
            var added = new List<int>();
            var removed = new List<int>();

            fixture.BeforeItemsAdded.Subscribe(before_added.Add);
            fixture.BeforeItemsRemoved.Subscribe(before_removed.Add);
            fixture.ItemsAdded.Subscribe(added.Add);
            fixture.ItemsRemoved.Subscribe(removed.Add);

            fixture.Add(10);
            fixture.Add(20);
            fixture.Add(30);
            fixture.RemoveAt(1);
            fixture.Clear();

            var added_results = new[] { 10, 20, 30 };
            Assert.Equal(added_results.Length, added.Count);
            added_results.AssertAreEqual(added);

            var removed_results = new[] { 20 };
            Assert.Equal(removed_results.Length, removed.Count);
            removed_results.AssertAreEqual(removed);

            Assert.Equal(before_added.Count, added.Count);
            added.AssertAreEqual(before_added);

            Assert.Equal(before_removed.Count, removed.Count);
            removed.AssertAreEqual(before_removed);
        }

        [Fact]
        public void MoveShouldBehaveAsObservableCollectionMove()
        {
            var items = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            var fixture = new ReactiveList<int>(items);
            var reference = new System.Collections.ObjectModel.ObservableCollection<int>(items);

            Assert.True(fixture.SequenceEqual(reference));

            var fixtureNotifications = new List<NotifyCollectionChangedEventArgs>();
            var referenceNotifications = new List<NotifyCollectionChangedEventArgs>();

            fixture.Changed.Subscribe(fixtureNotifications.Add);

            Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(x => reference.CollectionChanged += x, x => reference.CollectionChanged -= x).Select(x => x.EventArgs).Subscribe(referenceNotifications.Add);

            for (var i = 0; i < items.Length; i++)
            {
                for (var j = 0; j < items.Length; j++)
                {
                    reference.Move(i, j);
                    fixture.Move(i, j);

                    Assert.True(fixture.SequenceEqual(reference));
                    Assert.Equal(fixtureNotifications.Count, referenceNotifications.Count);

                    var lastFixtureNotification = fixtureNotifications.Last();
                    var lastReferenceNotification = referenceNotifications.Last();

                    Assert.Equal(NotifyCollectionChangedAction.Move, lastFixtureNotification.Action);
                    Assert.Equal(NotifyCollectionChangedAction.Move, lastReferenceNotification.Action);

                    Assert.Equal(lastFixtureNotification.OldStartingIndex, lastReferenceNotification.OldStartingIndex);
                    Assert.Equal(lastFixtureNotification.NewStartingIndex, lastReferenceNotification.NewStartingIndex);

                    Assert.Equal(lastReferenceNotification.OldItems[0], lastReferenceNotification.OldItems[0]);
                    Assert.Equal(lastReferenceNotification.NewItems[0], lastReferenceNotification.NewItems[0]);
                }
            }
        }

        [Fact]
        public void ReactiveCollectionIsRoundTrippable()
        {
            var output = new[] { "Foo", "Bar", "Baz", "Bamf" };
            var fixture = new ReactiveList<string>(output);

            var json = JSONHelper.Serialize(fixture);
            var results = JSONHelper.Deserialize<ReactiveList<string>>(json);

            output.AssertAreEqual(results);

            var should_die = true;
            results.ItemsAdded.Subscribe(_ => should_die = false);
            results.Add("Foobar");
            Assert.False(should_die);
        }

        [Fact]
        public void SortShouldActuallySort()
        {
            var fixture = new ReactiveList<int>(new[] { 5, 1, 3, 2, 4, });
            fixture.Sort();

            Assert.True(new[] { 1, 2, 3, 4, 5, }.Zip(fixture, (expected, actual) => expected == actual).All(x => x));
        }

        [Fact]
        public void TestDelayNotifications()
        {
            var maxSize = 10;
            var data = MakeAsyncCollection(maxSize);

            var list = new ReactiveList<TextModel>(data)
            {
                ChangeTrackingEnabled = true
            };

            var derivedList = list.CreateDerivedCollection(m => m.Value, m => m.HasData, (a, b) => a.Text.CompareTo(b.Text), Observable.Never(4), RxApp.MainThreadScheduler);

            derivedList.CountChanged.StartWith(derivedList.Count).Subscribe(
                                                                            count =>
                                                                            {
                                                                                Debug.WriteLine(count);
                                                                                Assert.True(count <= maxSize);
                                                                            });

            data = MakeAsyncCollection(maxSize);

            Observable.Delay(Observables.Unit, TimeSpan.FromMilliseconds(100)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(
                                                                                                                              _ =>
                                                                                                                              {
                                                                                                                                  using (list.SuppressChangeNotifications())
                                                                                                                                  {
                                                                                                                                      list.Clear();
                                                                                                                                      list.AddRange(data);
                                                                                                                                  }
                                                                                                                              });

            var done = new EventWaitHandle(false, EventResetMode.ManualReset);
            done.WaitOne(5000);
        }

        [Fact]
        public void WhenAddingRangeOfNullArgumentNullExceptionIsThrown()
        {
            var fixture = new ReactiveList<int>();

            Assert.Throws<ArgumentNullException>(() => fixture.AddRange(null));
        }

        [Fact]
        public void WhenInsertingRangeOfNullArgumentNullExceptionIsThrown()
        {
            var fixture = new ReactiveList<int>();

            Assert.Throws<ArgumentNullException>(() => fixture.InsertRange(1, null));
        }

        [Fact]
        public void WhenInsertingRangeOutOfRangeExceptionIsThrown()
        {
            var fixture = new ReactiveList<int>();

            Assert.Throws<ArgumentOutOfRangeException>(
                                                       () => fixture.InsertRange(
                                                                                 1,
                                                                                 new List<int>
                                                                                 {
                                                                                     1
                                                                                 }));
        }

        [Fact]
        public void WhenRemovingAllOfNullArgumentNullExceptionIsThrown()
        {
            var fixture = new ReactiveList<int>();

            Assert.Throws<ArgumentNullException>(() => fixture.RemoveAll(null));
        }

        private ReactiveList<TextModel> MakeAsyncCollection(int maxSize)
        {
            return new ReactiveList<TextModel>(Enumerable.Repeat(Unit.Default, maxSize).Select(_ => new TextModel()));
        }

        public static class TheDerivedSortMethods
        {
            public class TheNewPositionForExistingItemMethod
            {
                [Fact]
                public void TheNewPositionForExistingItemMethodSmokeTest()
                {
                    AssertNewIndex(new[] { 10, 20 }, newValue: 15, currentIndex: 0, expectedNewIndex: 0);
                    AssertNewIndex(new[] { 10, 20 }, newValue: 25, currentIndex: 0, expectedNewIndex: 1);

                    AssertNewIndex(new[] { 10, 20 }, newValue: 15, currentIndex: 1, expectedNewIndex: 1);
                    AssertNewIndex(new[] { 10, 20 }, newValue: 5, currentIndex: 1, expectedNewIndex: 0);

                    AssertNewIndex(new[] { 10, 20, 30 }, newValue: 15, currentIndex: 2, expectedNewIndex: 1);
                    AssertNewIndex(new[] { 10, 20, 30 }, newValue: 5, currentIndex: 2, expectedNewIndex: 0);

                    var items = new[] { 10, 20, 30, 40, 50 };

                    AssertNewIndex(items, newValue: 11, currentIndex: 0, expectedNewIndex: 0);
                    AssertNewIndex(items, newValue: 10, currentIndex: 0, expectedNewIndex: 0);
                    AssertNewIndex(items, newValue: 15, currentIndex: 0, expectedNewIndex: 0);
                    AssertNewIndex(items, newValue: 19, currentIndex: 0, expectedNewIndex: 0);
                    AssertNewIndex(items, newValue: 21, currentIndex: 0, expectedNewIndex: 1);
                    AssertNewIndex(items, newValue: 60, currentIndex: 0, expectedNewIndex: 4);

                    AssertNewIndex(items, newValue: 50, currentIndex: 3, expectedNewIndex: 3);

                    AssertNewIndex(items, newValue: 1, currentIndex: 4, expectedNewIndex: 0);
                    AssertNewIndex(items, newValue: 51, currentIndex: 4, expectedNewIndex: 4);
                    AssertNewIndex(items, newValue: 39, currentIndex: 4, expectedNewIndex: 3);

                    AssertNewIndex(items, newValue: 10, currentIndex: 1, expectedNewIndex: 1);
                }

                private static void AssertNewIndex<T>(IList<T> items, T newValue, int currentIndex, int expectedNewIndex)
                {
                    var newIndex = ReactiveDerivedCollection<T, T>.NewPositionForExistingItem(items, newValue, currentIndex, Comparer<T>.Default.Compare);

                    Assert.Equal(expectedNewIndex, newIndex);

                    var test = new List<T>(items);
                    test.RemoveAt(currentIndex);
                    test.Insert(newIndex, newValue);

                    Assert.True(test.SequenceEqual(test.OrderBy(x => x, Comparer<T>.Default)));
                }
            }

            public class ThePositionForNewItemMethod
            {
                [Fact]
                public void ThePositionForNewItemMethodSmokeTest()
                {
                    AssertNewIndex(Array.Empty<int>(), newValue: 1, expectedIndex: 0);

                    AssertNewIndex(new[] { 10 }, newValue: 9, expectedIndex: 0);
                    AssertNewIndex(new[] { 10 }, newValue: 10, expectedIndex: 0);
                    AssertNewIndex(new[] { 10 }, newValue: 11, expectedIndex: 1);

                    AssertNewIndex(new[] { 10, 20 }, newValue: 9, expectedIndex: 0);
                    AssertNewIndex(new[] { 10, 20 }, newValue: 15, expectedIndex: 1);
                    AssertNewIndex(new[] { 10, 20 }, newValue: 20, expectedIndex: 1);
                    AssertNewIndex(new[] { 10, 20 }, newValue: 21, expectedIndex: 2);

                    var items = new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90 };

                    AssertNewIndex(items, newValue: 0, expectedIndex: 0);
                    AssertNewIndex(items, newValue: 15, expectedIndex: 1);
                    AssertNewIndex(items, newValue: 25, expectedIndex: 2);
                    AssertNewIndex(items, newValue: 35, expectedIndex: 3);
                    AssertNewIndex(items, newValue: 45, expectedIndex: 4);
                    AssertNewIndex(items, newValue: 55, expectedIndex: 5);
                    AssertNewIndex(items, newValue: 65, expectedIndex: 6);
                    AssertNewIndex(items, newValue: 75, expectedIndex: 7);
                    AssertNewIndex(items, newValue: 85, expectedIndex: 8);
                    AssertNewIndex(items, newValue: 95, expectedIndex: 9);
                }

                private static void AssertNewIndex<T>(IList<T> items, T newValue, int expectedIndex, Func<T, T, int> orderer = null)
                {
                    if (orderer == null)
                    {
                        orderer = Comparer<T>.Default.Compare;
                    }

                    var newIndex = ReactiveDerivedCollection<T, T>.PositionForNewItem(items, newValue, orderer);

                    Assert.Equal(expectedIndex, newIndex);
                }
            }
        }

        public class DerivedCollectionLogging
        {
            [Fact]
            public void DerivedCollectionsShouldNotTriggerSupressNotificationWarning()
            {
                var resolver = new ModernDependencyResolver();
                var logger = new TestLogger();

                using (resolver.WithResolver())
                {
                    resolver.RegisterConstant(new FuncLogManager(t => new WrappingFullLogger(new WrappingPrefixLogger(logger, t))), typeof(ILogManager));

                    var incc = new ReactiveList<NoOneHasEverSeenThisClassBeforeEither>(scheduler: CurrentThreadScheduler.Instance);
                    var inccDerived = incc.CreateDerivedCollection(x => x);

                    Assert.False(logger.Messages.Any(x => x.Item1.Contains("SuppressChangeNotifications")));

                    // Derived collections should only suppress warnings for internal behavior.
                    inccDerived.ItemsAdded.Subscribe();
                    incc.Reset();
                    Assert.True(logger.Messages.Any(x => x.Item1.Contains("SuppressChangeNotifications")));
                }
            }

            [Fact]
            public void DerivedCollectionsShouldWarnWhenSourceIsNotINotifyCollectionChanged()
            {
                var resolver = new ModernDependencyResolver();
                var logger = new TestLogger();

                using (resolver.WithResolver())
                {
                    resolver.RegisterConstant(new FuncLogManager(t => new WrappingFullLogger(new WrappingPrefixLogger(logger, t))), typeof(ILogManager));

                    var incc = new ReactiveList<NoOneHasEverSeenThisClassBefore>(scheduler: CurrentThreadScheduler.Instance);
                    Assert.True(incc is INotifyCollectionChanged);
                    var inccDerived = incc.CreateDerivedCollection(x => x);

                    Assert.False(logger.Messages.Any(x => x.Item1.Contains("INotifyCollectionChanged")));

                    // Reset
                    logger.Messages.Clear();

                    var nonIncc = new List<NoOneHasEverSeenThisClassBefore>();

                    Assert.False(nonIncc is INotifyCollectionChanged);
                    var nonInccderived = nonIncc.CreateDerivedCollection(x => x);

                    Assert.Equal(1, logger.Messages.Count);

                    var m = logger.Messages.Last();
                    var message = m.Item1;
                    var level = m.Item3;

                    Assert.Contains("INotifyCollectionChanged", message);
                    Assert.Equal(LogLevel.Warn, level);
                }
            }

            // We need a sentinel class to make sure no test has triggered the warnings before
            #pragma warning disable CA1812 // Never instantiated classes.
            private class NoOneHasEverSeenThisClassBefore
            {
            }

            private class NoOneHasEverSeenThisClassBeforeEither
            {
            }
            #pragma warning restore CA1812 // Never instantiated class.
        }

        public class DerivedPropertyChanges
        {
            [Fact]
            public void DerivedCollectionShouldHandleRemovesOfFilteredItems()
            {
                var a = new ReactiveVisibilityItem<string>("A", true);
                var b = new ReactiveVisibilityItem<string>("B", true);
                var c = new ReactiveVisibilityItem<string>("C", true);
                var d = new ReactiveVisibilityItem<string>("D", false);
                var e = new ReactiveVisibilityItem<string>("E", true);

                var items = new ReactiveList<ReactiveVisibilityItem<string>>(new[] { a, b, c, d, e })
                {
                    ChangeTrackingEnabled = true
                };

                var onlyVisible = items.CreateDerivedCollection(x => x.Value, x => x.IsVisible, OrderedComparer<string>.OrderByDescending(x => x).Compare);

                Assert.True(onlyVisible.SequenceEqual(new[] { "E", "C", "B", "A" }, StringComparer.Ordinal));
                Assert.Equal(4, onlyVisible.Count);

                // Removal of an item from the source collection that's filtered in the derived collection should
                // have no effect on the derived.
                items.Remove(d);

                Assert.True(onlyVisible.SequenceEqual(new[] { "E", "C", "B", "A" }, StringComparer.Ordinal));
                Assert.Equal(4, onlyVisible.Count);

                c.IsVisible = false;
                Assert.Equal(3, onlyVisible.Count);
                Assert.True(onlyVisible.SequenceEqual(new[] { "E", "B", "A" }, StringComparer.Ordinal));

                items.Remove(c);
                Assert.Equal(3, onlyVisible.Count);
                Assert.True(onlyVisible.SequenceEqual(new[] { "E", "B", "A" }, StringComparer.Ordinal));

                items.Remove(b);
                Assert.Equal(2, onlyVisible.Count);
                Assert.True(onlyVisible.SequenceEqual(new[] { "E", "A" }, StringComparer.Ordinal));
            }

            [Fact]
            public void DerivedCollectionsShouldReactToPropertyChanges()
            {
                // This differs from the FilteredDerivedCollectionsShouldReactToPropertyChanges as it tests providing a
                // non-identity selector (ie x=>x.Value).
                var foo = new ReactiveVisibilityItem<string>("Foo", true);
                var bar = new ReactiveVisibilityItem<string>("Bar", true);
                var baz = new ReactiveVisibilityItem<string>("Baz", true);

                var items = new ReactiveList<ReactiveVisibilityItem<string>>(new[] { foo, bar, baz })
                {
                    ChangeTrackingEnabled = true
                };

                var onlyVisible = items.CreateDerivedCollection(
                                                                x => new string('*', x.Value.Length), // Note, not an identity function.
                                                                x => x.IsVisible,
                                                                StringComparer.Ordinal.Compare);

                Assert.Equal(3, onlyVisible.Count);
                Assert.True(onlyVisible.SequenceEqual(new[] { "***", "***", "***" }));

                foo.IsVisible = false;

                Assert.Equal(2, onlyVisible.Count);
                Assert.True(onlyVisible.SequenceEqual(new[] { "***", "***" }));
            }

            [Fact]
            public void DerivedCollectionsSmokeTest()
            {
                var adam = new ReactiveEmployee
                {
                    Name = "Adam",
                    Age = 20,
                    Salary = 100
                };
                var bob = new ReactiveEmployee
                {
                    Name = "Bob",
                    Age = 30,
                    Salary = 150
                };
                var carol = new ReactiveEmployee
                {
                    Name = "Carol",
                    Age = 40,
                    Salary = 200
                };
                var dan = new ReactiveEmployee
                {
                    Name = "Dan",
                    Age = 50,
                    Salary = 250
                };
                var eve = new ReactiveEmployee
                {
                    Name = "Eve",
                    Age = 60,
                    Salary = 300
                };

                var start = new[] { adam, bob, carol, dan, eve };

                var employees = new ReactiveList<ReactiveEmployee>(start)
                {
                    ChangeTrackingEnabled = true
                };

                var employeesByName = DerivedCollectionTestContainer.Create(employees, selector: x => x, orderer: OrderedComparer<ReactiveEmployee>.OrderBy(x => x.Name));

                var employeesByAge = DerivedCollectionTestContainer.Create(employees, selector: x => x, orderer: OrderedComparer<ReactiveEmployee>.OrderBy(x => x.Age));

                var employeesBySalary = DerivedCollectionTestContainer.Create(employees, selector: x => x, orderer: OrderedComparer<ReactiveEmployee>.OrderBy(x => x.Salary));

                // special

                // filtered, ordered, reference
                var oldEmployeesByAge = DerivedCollectionTestContainer.Create(employees, selector: x => x, filter: x => x.Age >= 50, orderer: OrderedComparer<ReactiveEmployee>.OrderBy(x => x.Age));

                // ordered, not reference
                var employeeSalaries = DerivedCollectionTestContainer.Create(employees, selector: x => x.Salary, orderer: Comparer<int>.Default);

                // not filtered (derived filter), not reference, not ordered (derived order)
                oldEmployeesByAge.Derived.ChangeTrackingEnabled = true;
                var oldEmployeesSalariesByAge = DerivedCollectionTestContainer.Create(oldEmployeesByAge.Derived, selector: x => x.Salary);

                var containers = new List<DerivedCollectionTestContainer>
                {
                    employeesByName,
                    employeesByAge,
                    employeesBySalary,
                    oldEmployeesByAge,
                    employeeSalaries,
                    oldEmployeesSalariesByAge
                };

                Action<Action> testAll = a =>
                {
                    a();
                    containers.ForEach(x => x.Test());
                };

                containers.ForEach(x => x.Test());

                // if (isIncluded && !shouldBeIncluded)
                testAll(() => { dan.Age = 49; });

                // else if (!isIncluded && shouldBeIncluded)
                testAll(() => { dan.Age = eve.Age + 1; });

                // else if (isIncluded && shouldBeIncluded)
                testAll(() => { adam.Salary = 350; });
                testAll(() => { dan.Age = 50; });
                testAll(() => { dan.Age = 51; });
            }

            [Fact]
            public void FilteredDerivedCollectionsShouldReactToPropertyChanges()
            {
                // Naturally this isn't done by magic, it only works if the source implements IReactiveCollection.
                var a = new ReactiveVisibilityItem<string>("a", true);
                var b = new ReactiveVisibilityItem<string>("b", true);
                var c = new ReactiveVisibilityItem<string>("c", true);

                var items = new ReactiveList<ReactiveVisibilityItem<string>>(new[] { a, b, c })
                {
                    ChangeTrackingEnabled = true
                };

                var onlyVisible = items.CreateDerivedCollection(x => x.Value, x => x.IsVisible, StringComparer.Ordinal.Compare);

                var onlyNonVisible = items.CreateDerivedCollection(x => x.Value, x => !x.IsVisible, StringComparer.Ordinal.Compare);

                var onlVisibleStartingWithB = items.CreateDerivedCollection(x => x.Value, x => x.IsVisible && x.Value.StartsWith("b"), StringComparer.Ordinal.Compare);

                Assert.Equal(3, onlyVisible.Count);
                Assert.Equal(0, onlyNonVisible.Count);
                Assert.Equal(1, onlVisibleStartingWithB.Count);

                a.IsVisible = false;

                Assert.Equal(2, onlyVisible.Count);
                Assert.Equal(1, onlyNonVisible.Count);
                Assert.Equal(1, onlVisibleStartingWithB.Count);

                b.Value = "D";

                Assert.Equal(0, onlVisibleStartingWithB.Count);
            }

            [Fact]
            public void FilteredProjectedDerivedCollectionsShouldReactToPropertyChanges()
            {
                // This differs from the FilteredDerivedCollectionsShouldReactToPropertyChanges as it tests providing a
                // non-identity selector (ie x=>x.Value).
                var a = new ReactiveVisibilityItem<string>("a", true);
                var b = new ReactiveVisibilityItem<string>("b", true);
                var c = new ReactiveVisibilityItem<string>("c", true);

                var items = new ReactiveList<ReactiveVisibilityItem<string>>(new[] { a, b, c })
                {
                    ChangeTrackingEnabled = true
                };

                var onlyVisible = items.CreateDerivedCollection(
                                                                x => x.Value.ToUpper(CultureInfo.InvariantCulture), // Note, not an identity function.
                                                                x => x.IsVisible,
                                                                StringComparer.Ordinal.Compare);

                Assert.Equal(3, onlyVisible.Count);
                Assert.True(onlyVisible.SequenceEqual(new[] { "A", "B", "C" }));

                a.IsVisible = false;

                Assert.Equal(2, onlyVisible.Count);
                Assert.True(onlyVisible.SequenceEqual(new[] { "B", "C" }));
            }

            [Fact]
            public void PropertyChangesShouldWorkWithChainedCollections()
            {
                // This is a highly contrived test and I apologize for it not making much sense. I added it
                // specifically track down an bug I was hitting when derived collection notification triggered
                // re-entrant notifications.
                var a = new ReactiveVisibilityItem<string>("A", true);
                var b = new ReactiveVisibilityItem<string>("B", true);
                var c = new ReactiveVisibilityItem<string>("C", true);
                var d = new ReactiveVisibilityItem<string>("D", true);
                var e = new ReactiveVisibilityItem<string>("E", true);
                var f = new ReactiveVisibilityItem<string>("F", true);

                var items = new ReactiveList<ReactiveVisibilityItem<string>>(new[] { a, b, c, d, e, f })
                {
                    ChangeTrackingEnabled = true
                };

                var itemsByVisibility = items.CreateDerivedCollection(x => x, orderer: OrderedComparer<ReactiveVisibilityItem<string>>.OrderByDescending(x => x.IsVisible).ThenBy(x => x.Value).Compare);

                itemsByVisibility.ChangeTrackingEnabled = true;

                var onlyVisibleReversed = itemsByVisibility.CreateDerivedCollection(x => x, x => x.IsVisible, OrderedComparer<ReactiveVisibilityItem<string>>.OrderByDescending(x => x.Value).Compare);

                onlyVisibleReversed.ChangeTrackingEnabled = true;

                var onlyVisibleAndGreaterThanC = onlyVisibleReversed.CreateDerivedCollection(x => x, x => x.Value[0] > 'C', OrderedComparer<ReactiveVisibilityItem<string>>.OrderBy(x => x.Value).Compare);

                onlyVisibleAndGreaterThanC.ChangeTrackingEnabled = true;

                Assert.True(items.SequenceEqual(new[] { a, b, c, d, e, f }));
                Assert.True(itemsByVisibility.SequenceEqual(new[] { a, b, c, d, e, f }));
                Assert.True(onlyVisibleReversed.SequenceEqual(new[] { f, e, d, c, b, a }));
                Assert.True(onlyVisibleAndGreaterThanC.SequenceEqual(new[] { d, e, f }));

                // When the value of d changes, update a to Y
                d.WhenAnyValue(x => x.Value).Where(x => x == "Y").Subscribe(x => a.Value = "Z");

                // When the visibility of e changes, update d to Z
                e.WhenAnyValue(x => x.IsVisible).Where(x => x == false).Subscribe(x => d.Value = "Y");

                // As soon as the "last" collection changes, remove b
                onlyVisibleAndGreaterThanC.Changed.Subscribe(x => items.Remove(b));

                e.IsVisible = false;

                Assert.True(items.SequenceEqual(new[] { a, c, d, e, f }));
                Assert.True(
                            itemsByVisibility.SequenceEqual(
                                                            new[]
                                                            {
                                                                c, f, d, // d is now y
                                                                a, // a is now z
                                                                e // e is now hidden
                                                            }));

                Assert.True(
                            onlyVisibleReversed.SequenceEqual(
                                                              new[]
                                                              {
                                                                  a, // a is now z
                                                                  d, // d is now y
                                                                  f, c
                                                              }));

                Assert.True(
                            onlyVisibleAndGreaterThanC.SequenceEqual(
                                                                     new[]
                                                                     {
                                                                         f, d, // d is now y
                                                                         a, // a is now z
                                                                     }));
            }

            public class DerivedCollectionTestContainer
            {
                public static DerivedCollectionTestContainer<TSource, TValue> Create<TSource, TValue>(IEnumerable<TSource> source, Func<TSource, TValue> selector, Func<TSource, bool> filter = null, IComparer<TValue> orderer = null)
                {
                    var comparison = orderer == null ? (Func<TValue, TValue, int>)null : orderer.Compare;
                    var derived = source.CreateDerivedCollection(selector, filter, comparison);

                    return new DerivedCollectionTestContainer<TSource, TValue>
                    {
                        Source = source,
                        Selector = selector,
                        Derived = derived,
                        Filter = filter,
                        Orderer = orderer
                    };
                }

                public virtual void Test()
                {
                }
            }

            public class DerivedCollectionTestContainer<TSource, TValue> : DerivedCollectionTestContainer
            {
                public IReactiveDerivedList<TValue> Derived { get; set; }

                public Func<TSource, bool> Filter { get; set; }

                public IComparer<TValue> Orderer { get; set; }

                public Func<TSource, TValue> Selector { get; set; }

                public IEnumerable<TSource> Source { get; set; }

                public override void Test()
                {
                    var filtered = Source;

                    if (Filter != null)
                    {
                        filtered = filtered.Where(Filter);
                    }

                    var projected = filtered.Select(Selector);

                    var ordered = projected;

                    if (Orderer != null)
                    {
                        ordered = ordered.OrderBy(x => x, Orderer);
                    }

                    var shouldBe = ordered;
                    var isEqual = Derived.SequenceEqual(shouldBe);

                    Assert.True(isEqual);
                }
            }

            [DebuggerDisplay("{Name} is {Age} years old and makes ${Salary}")]
            private class ReactiveEmployee : ReactiveObject
            {
                private int _age;

                private string _name;

                private int _salary;

                public int Age
                {
                    get => _age;
                    set => this.RaiseAndSetIfChanged(ref _age, value);
                }

                public string Name
                {
                    get => _name;
                    set => this.RaiseAndSetIfChanged(ref _name, value);
                }

                public int Salary
                {
                    get => _salary;
                    set => this.RaiseAndSetIfChanged(ref _salary, value);
                }
            }

            private class ReactiveVisibilityItem<T> : ReactiveObject
            {
                private bool _isVisible;

                private T _value;

                public ReactiveVisibilityItem(T item1, bool isVisible)
                {
                    _value = item1;
                    _isVisible = isVisible;
                }

                public bool IsVisible
                {
                    get => _isVisible;
                    set => this.RaiseAndSetIfChanged(ref _isVisible, value);
                }

                public T Value
                {
                    get => _value;
                    set => this.RaiseAndSetIfChanged(ref _value, value);
                }
            }
        }

        public class PropertyBindView : Control, IViewFor<PropertyBindViewModel>
        {
            public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(PropertyBindViewModel), typeof(PropertyBindView), new PropertyMetadata(null));

            public PropertyBindFakeControl FakeControl;

            public ListBox FakeItemsControl;

            public TextBox Property2;

            public TextBox SomeTextBox;

            public PropertyBindView()
            {
                SomeTextBox = new TextBox();
                Property2 = new TextBox();
                FakeControl = new PropertyBindFakeControl();
                FakeItemsControl = new ListBox();
            }

            public PropertyBindViewModel ViewModel
            {
                get => (PropertyBindViewModel)GetValue(ViewModelProperty);
                set => SetValue(ViewModelProperty, value);
            }

            object IViewFor.ViewModel
            {
                get => ViewModel;
                set => ViewModel = (PropertyBindViewModel)value;
            }
        }

        public class PropertyBindViewModel : ReactiveObject
        {
            public decimal _JustADecimal;

            public double _JustADouble;

            public int _JustAInt32;

            public PropertyBindModel _Model;

            public double? _NullableDouble;

            public string _Property1;

            public int _Property2;

            public PropertyBindViewModel(PropertyBindModel model = null)
            {
                Model = model ?? new PropertyBindModel
                {
                    AThing = 42,
                    AnotherThing = "Baz"
                };
                SomeCollectionOfStrings = new ReactiveList<string>(new[] { "Foo", "Bar" });
            }

            public decimal JustADecimal
            {
                get => _JustADecimal;
                set => this.RaiseAndSetIfChanged(ref _JustADecimal, value);
            }

            public double JustADouble
            {
                get => _JustADouble;
                set => this.RaiseAndSetIfChanged(ref _JustADouble, value);
            }

            public int JustAInt32
            {
                get => _JustAInt32;
                set => this.RaiseAndSetIfChanged(ref _JustAInt32, value);
            }

            public PropertyBindModel Model
            {
                get => _Model;
                set => this.RaiseAndSetIfChanged(ref _Model, value);
            }

            public double? NullableDouble
            {
                get => _NullableDouble;
                set => this.RaiseAndSetIfChanged(ref _NullableDouble, value);
            }

            public string Property1
            {
                get => _Property1;
                set => this.RaiseAndSetIfChanged(ref _Property1, value);
            }

            public int Property2
            {
                get => _Property2;
                set => this.RaiseAndSetIfChanged(ref _Property2, value);
            }

            public ReactiveList<string> SomeCollectionOfStrings { get; }
        }
    }

    internal class NestedTextModel : ReactiveObject
    {
        private bool _hasData;

        private string _text;

        public bool HasData
        {
            get => _hasData;
            set => this.RaiseAndSetIfChanged(ref _hasData, value);
        }

        public string Text
        {
            get => _text;
            set => this.RaiseAndSetIfChanged(ref _text, value);
        }
    }

    internal class TextModel : ReactiveObject
    {
        private NestedTextModel _value;

        public TextModel()
        {
            this.WhenAnyValue(x => x.Value.HasData).Subscribe(_ => this.RaisePropertyChanged(nameof(HasData)));
        }

        public bool HasData => Value.HasData;

        public NestedTextModel Value
        {
            get
            {
                if (_value != null)
                {
                    return _value;
                }

                var newValue = _value = new NestedTextModel
                {
                    Text = "text",
                    HasData = true
                };

                this.RaiseAndSetIfChanged(ref _value, newValue);
                return _value;
            }
        }
    }
}
