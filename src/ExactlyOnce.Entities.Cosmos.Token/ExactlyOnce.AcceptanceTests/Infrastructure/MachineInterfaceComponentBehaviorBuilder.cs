using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.AcceptanceTesting;
using NServiceBus.AcceptanceTesting.Support;

namespace ExactlyOnce.AcceptanceTests.Infrastructure;

public class MachineInterfaceComponentBehaviorBuilder<TContext> where TContext : ScenarioContext
{
    public MachineInterfaceComponentBehaviorBuilder(IEndpointConfigurationFactory endpointConfigurationFactory)
    {
        behavior = new MachineInterfaceComponentBehavior(endpointConfigurationFactory)
        {
            Whens = new List<IWhenDefinition>()
        };
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> When(Func<IMessageSession, TContext, Task> action)
    {
        return When(c => true, action);
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> When(Func<IMessageSession, Task> action)
    {
        return When(c => true, action);
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> When(Func<TContext, Task<bool>> condition, Func<IMessageSession, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

        return this;
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IMessageSession, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(ctx => Task.FromResult(condition(ctx)), action));

        return this;
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> When(Func<TContext, Task<bool>> condition, Func<IMessageSession, TContext, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(condition, action));

        return this;
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> When(Predicate<TContext> condition, Func<IMessageSession, TContext, Task> action)
    {
        behavior.Whens.Add(new WhenDefinition<TContext>(ctx => Task.FromResult(condition(ctx)), action));

        return this;
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> CustomConfig(Action<EndpointConfiguration> action)
    {
        behavior.CustomConfig.Add((busConfig, context) => action(busConfig));

        return this;
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> CustomConfig(Action<EndpointConfiguration, TContext> action)
    {
        behavior.CustomConfig.Add((configuration, context) => action(configuration, (TContext)context));

        return this;
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> ToCreateInstance<T>(Func<EndpointConfiguration, Task<T>> createCallback, Func<T, Task<IEndpointInstance>> startCallback)
    {
        behavior.ConfigureHowToCreateInstance(createCallback, startCallback);

        return this;
    }

    public MachineInterfaceComponentBehaviorBuilder<TContext> DoNotFailOnErrorMessages()
    {
        behavior.DoNotFailOnErrorMessages = true;

        return this;
    }

    public MachineInterfaceComponentBehavior Build()
    {
        return behavior;
    }

    MachineInterfaceComponentBehavior behavior;
}