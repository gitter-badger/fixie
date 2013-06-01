﻿using System;
using System.Reflection;
using Fixie.Behaviors;
using Fixie.Conventions;

namespace Fixie.Samples
{
    //The types in this file serve the NUnit and xUnit samples.
    //Once refined, they'll be promoted to members of the Fixie
    //namespace.

    public delegate ExceptionList ClassAction(Type testClass);
    public delegate ExceptionList InstanceAction(Type testClass, object instance);

    public static class MethodFilterExtensions
    {
        public static ExceptionList InvokeAll(this MethodFilter methodFilter, Type testClass, object instance)
        {
            var invoke = new Invoke();
            var exceptions = new ExceptionList();
            foreach (var method in methodFilter.Filter(testClass))
                invoke.Execute(method, instance, exceptions);
            return exceptions;
        }
    }

    public class MethodBehaviorBuilder
    {
        public MethodBehaviorBuilder()
        {
            Behavior = new Invoke();
        }

        public MethodBehavior Behavior { get; private set; }

        public MethodBehaviorBuilder SetUpTearDown<TSetUpAttribute, TTearDownAttribute>()
            where TSetUpAttribute : Attribute
            where TTearDownAttribute : Attribute
        {
            Behavior = new MethodSetUpTearDown(
                new MethodFilter().HasOrInherits<TSetUpAttribute>().InvokeAll,
                Behavior,
                new MethodFilter().HasOrInherits<TTearDownAttribute>().InvokeAll);
            return this;
        }

        class MethodSetUpTearDown : MethodBehavior
        {
            readonly InstanceAction setUp;
            readonly MethodBehavior inner;
            readonly InstanceAction tearDown;

            public MethodSetUpTearDown(InstanceAction setUp, MethodBehavior inner, InstanceAction tearDown)
            {
                this.setUp = setUp;
                this.inner = inner;
                this.tearDown = tearDown;
            }

            public void Execute(MethodInfo method, object instance, ExceptionList exceptions)
            {
                var setUpExceptions = setUp(method.ReflectedType, instance);
                if (setUpExceptions.Any())
                {
                    exceptions.Add(setUpExceptions);
                    return;
                }

                inner.Execute(method, instance, exceptions);

                exceptions.Add(tearDown(method.ReflectedType, instance));
            }
        }
    }

    public class InstanceBehaviorBuilder
    {
        public InstanceBehaviorBuilder()
        {
            Behavior = new ExecuteCases();
        }

        public InstanceBehavior Behavior { get; private set; }

        public InstanceBehaviorBuilder SetUpTearDown<TSetUpAttribute, TTearDownAttribute>()
            where TSetUpAttribute : Attribute
            where TTearDownAttribute : Attribute
        {
            Behavior = new InstanceSetUpTearDown(
                new MethodFilter().HasOrInherits<TSetUpAttribute>().InvokeAll,
                Behavior,
                new MethodFilter().HasOrInherits<TTearDownAttribute>().InvokeAll);
            return this;
        }

        public InstanceBehaviorBuilder SetUp(InstanceAction setUp)
        {
            Behavior = new InstanceSetUpTearDown(setUp, Behavior, (testClass, instance) => new ExceptionList());
            return this;
        }

        class InstanceSetUpTearDown : InstanceBehavior
        {
            readonly InstanceAction setUp;
            readonly InstanceBehavior inner;
            readonly InstanceAction tearDown;

            public InstanceSetUpTearDown(InstanceAction setUp, InstanceBehavior inner, InstanceAction tearDown)
            {
                this.setUp = setUp;
                this.inner = inner;
                this.tearDown = tearDown;
            }

            public void Execute(Type testClass, object instance, Case[] cases, Convention convention)
            {
                var instanceSetUpExceptions = setUp(testClass, instance);
                if (instanceSetUpExceptions.Any())
                {
                    foreach (var @case in cases)
                        @case.Exceptions.Add(instanceSetUpExceptions);
                    return;
                }

                inner.Execute(testClass, instance, cases, convention);

                var instanceTearDownExceptions = tearDown(testClass, instance);
                if (instanceTearDownExceptions.Any())
                    foreach (var @case in cases)
                        @case.Exceptions.Add(instanceTearDownExceptions);
            }
        }
    }

    public class TypeBehaviorBuilder
    {
        public TypeBehavior Behavior { get; private set; }

        public TypeBehaviorBuilder CreateInstancePerCase()
        {
            Behavior = new CreateInstancePerCase();
            return this;
        }

        public TypeBehaviorBuilder CreateInstancePerFixture()
        {
            Behavior = new CreateInstancePerFixture();
            return this;
        }

        public TypeBehaviorBuilder SetUpTearDown(ClassAction setUp, ClassAction tearDown)
        {
            Behavior = new ClassSetUpTearDown(setUp, Behavior, tearDown);
            return this;
        }

        class ClassSetUpTearDown : TypeBehavior
        {
            readonly ClassAction setUp;
            readonly TypeBehavior inner;
            readonly ClassAction tearDown;

            public ClassSetUpTearDown(ClassAction setUp, TypeBehavior inner, ClassAction tearDown)
            {
                this.setUp = setUp;
                this.inner = inner;
                this.tearDown = tearDown;
            }

            public void Execute(Type fixtureClass, Convention convention, Case[] cases)
            {
                var classSetUpExceptions = setUp(fixtureClass);
                if (classSetUpExceptions.Any())
                {
                    foreach (var @case in cases)
                        @case.Exceptions.Add(classSetUpExceptions);
                    return;
                }

                inner.Execute(fixtureClass, convention, cases);

                var classTearDownExceptions = tearDown(fixtureClass);
                foreach (var @case in cases)
                    @case.Exceptions.Add(classTearDownExceptions);
            }
        }
    }
}